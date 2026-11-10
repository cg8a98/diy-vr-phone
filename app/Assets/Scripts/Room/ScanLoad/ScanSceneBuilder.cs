using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DiyVr.Room
{
    /// <summary>A scan that is in the scene (or the reason it is not).</summary>
    public sealed class LoadedScan
    {
        /// <summary>Parent of everything in the scan. Sits at the world origin with no rotation and scale 1.</summary>
        public GameObject Root;
        public string Name;
        public int Triangles;
        /// <summary>Longest side of the biggest texture as it is now shown.</summary>
        public int TexturePx;
        /// <summary>Longest side of the biggest texture as it was in the file.</summary>
        public int SourceTexturePx;
        /// <summary>Null when loading worked.</summary>
        public string Error;

        internal GltfImport Import;
        internal ScanMaterialGenerator Generator;
        internal readonly List<Object> Owned = new List<Object>();

        public bool Ok { get { return Error == null && Root != null; } }

        /// <summary>Removes the scan from the scene and frees its meshes, textures and materials.</summary>
        public void Unload()
        {
            if (Root != null) ScanSceneBuilder.DestroyObject(Root);
            Root = null;
            if (Import != null)
            {
                try { Import.Dispose(); }
                catch (Exception) { /* freeing is best effort */ }
                Import = null;
            }
            for (int i = 0; i < Owned.Count; i++) ScanSceneBuilder.DestroyObject(Owned[i]);
            Owned.Clear();
            if (Generator != null)
            {
                for (int i = 0; i < Generator.Materials.Count; i++) ScanSceneBuilder.DestroyObject(Generator.Materials[i]);
                Generator = null;
            }
        }
    }

    /// <summary>
    /// Turns the bytes of a .glb into scene objects: glTFast reads the file, textures above the limit are made
    /// smaller, every mesh gets our unlit material. glTF is Y-up, right-handed, in metres; glTFast converts that
    /// to Unity's axes, and the root stays at the origin with scale 1, so 1 unit is 1 m.
    /// </summary>
    public static class ScanSceneBuilder
    {
        public const string RootName = "Scan";

        public static async Task<LoadedScan> BuildAsync(byte[] glb, string name, int maxTextureSize, IDeferAgent deferAgent = null)
        {
            var scan = new LoadedScan { Name = name };
            var log = new ScanLog();
            try
            {
                if (!ScanLoadLogic.LooksLikeGlb(glb, glb != null ? glb.Length : 0))
                {
                    scan.Error = "not a .glb file";
                    return scan;
                }

                scan.Generator = new ScanMaterialGenerator();
                scan.Import = new GltfImport(null, deferAgent, scan.Generator, log);
                var settings = new ImportSettings { GenerateMipMaps = true, AnisotropicFilterLevel = 2 };

                bool loaded = await scan.Import.Load(glb, null, settings);
                if (!loaded)
                {
                    scan.Error = "could not read the file" + Detail(log);
                    scan.Unload();
                    return scan;
                }

                ShrinkTextures(scan, maxTextureSize);

                scan.Root = new GameObject(RootName);
                // A file without a "main scene" entry: show its first scene instead (asking glTFast for the main
                // scene of such a file only logs a warning and builds nothing).
                bool placed;
                if (scan.Import.DefaultSceneIndex.HasValue)
                    placed = await scan.Import.InstantiateMainSceneAsync(scan.Root.transform);
                else if (scan.Import.SceneCount > 0)
                    placed = await scan.Import.InstantiateSceneAsync(scan.Root.transform, 0);
                else
                    placed = false;
                if (!placed)
                {
                    scan.Error = "could not build the scan" + Detail(log);
                    scan.Unload();
                    return scan;
                }

                scan.Triangles = SetUpRenderers(scan.Root);
                if (scan.Triangles == 0)
                {
                    scan.Error = "the file has no triangles";
                    scan.Unload();
                    return scan;
                }
            }
            catch (Exception e)
            {
                scan.Error = e.Message;
                scan.Unload();
            }
            return scan;
        }

        /// <summary>Swaps every too-big texture for a smaller copy and frees the big one straight away.</summary>
        private static void ShrinkTextures(LoadedScan scan, int maxTextureSize)
        {
            var replaced = new Dictionary<Texture2D, Texture2D>();
            IReadOnlyList<Material> materials = scan.Generator.Materials;
            for (int i = 0; i < materials.Count; i++)
            {
                var texture = materials[i].mainTexture as Texture2D;
                if (texture == null) continue;

                Texture2D small;
                if (replaced.TryGetValue(texture, out small))
                {
                    materials[i].mainTexture = small;
                    continue;
                }
                scan.SourceTexturePx = Mathf.Max(scan.SourceTexturePx, Mathf.Max(texture.width, texture.height));
                if (ScanLoadLogic.NeedsDownscale(texture.width, texture.height, maxTextureSize))
                {
                    small = ScanTextureScaler.Downscale(texture, maxTextureSize);
                    scan.Owned.Add(small);
                    replaced[texture] = small;
                    materials[i].mainTexture = small;
                    texture = small;
                }
                scan.TexturePx = Mathf.Max(scan.TexturePx, Mathf.Max(texture.width, texture.height));
            }
            foreach (KeyValuePair<Texture2D, Texture2D> pair in replaced) DestroyObject(pair.Key);
        }

        /// <summary>No shadows or light probes (the scan is unlit). Returns the triangle count.</summary>
        private static int SetUpRenderers(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            long triangles = 0;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null) continue;
                for (int s = 0; s < mesh.subMeshCount; s++)
                    if (mesh.GetTopology(s) == MeshTopology.Triangles) triangles += mesh.GetIndexCount(s) / 3;
            }
            return triangles > int.MaxValue ? int.MaxValue : (int)triangles;
        }

        private static string Detail(ScanLog log)
        {
            return string.IsNullOrEmpty(log.FirstError) ? string.Empty : " (" + log.FirstError + ")";
        }

        internal static void DestroyObject(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}
