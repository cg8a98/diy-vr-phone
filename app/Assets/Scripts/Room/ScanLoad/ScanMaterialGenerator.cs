using System;
using System.Collections.Generic;
using GLTFast;
using GLTFast.Logging;
using GLTFast.Materials;
using GLTFast.Schema;
using UnityEngine;
using Material = UnityEngine.Material;

namespace DiyVr.Room
{
    /// <summary>
    /// Tells glTFast to use our own unlit shader (DiyVr/StereoUnlit) with the scan's base colour texture, instead
    /// of glTFast's shader graphs. A photo scan has its lighting baked into the texture, so unlit is right, and
    /// glTFast's own shaders would have to be forced into the Android build.
    /// </summary>
    public sealed class ScanMaterialGenerator : IMaterialGenerator
    {
        public const string ShaderName = "DiyVr/StereoUnlit";
        private static readonly string[] FallbackShaders = { "Universal Render Pipeline/Unlit", "Unlit/Texture", "Sprites/Default" };

        private readonly List<Material> _materials = new List<Material>();
        private Material _default;

        /// <summary>Every material made for this scan (the default one included).</summary>
        public IReadOnlyList<Material> Materials { get { return _materials; } }

        public Material GetDefaultMaterial(bool pointsSupport = false)
        {
            if (_default == null) _default = NewMaterial("ScanDefault");
            return _default;
        }

        public Material GenerateMaterial(MaterialBase gltfMaterial, IGltfReadable gltf, bool pointsSupport = false)
        {
            string name = gltfMaterial != null && !string.IsNullOrEmpty(gltfMaterial.name) ? gltfMaterial.name : "ScanMaterial";
            Material material = NewMaterial(name);
            PbrMetallicRoughnessBase pbr = gltfMaterial != null ? gltfMaterial.PbrMetallicRoughness : null;
            if (pbr == null) return material;

            Texture2D texture = null;
            TextureInfoBase info = pbr.BaseColorTexture;
            if (info != null && info.index >= 0 && gltf != null) texture = gltf.GetTexture(info.index);
            if (texture != null) material.mainTexture = texture;
            else material.color = pbr.BaseColor; // untextured part: show its plain colour
            return material;
        }

        public void SetLogger(ICodeLogger logger) { }

        private Material NewMaterial(string name)
        {
            Shader shader = Shader.Find(ShaderName);
            for (int i = 0; shader == null && i < FallbackShaders.Length; i++) shader = Shader.Find(FallbackShaders[i]);
            if (shader == null) throw new InvalidOperationException("shader " + ShaderName + " is missing from the app");
            var material = new Material(shader) { name = name, color = Color.white };
            _materials.Add(material);
            return material;
        }
    }

    /// <summary>Keeps glTFast's messages out of the Unity log and remembers the first error for the status label.</summary>
    public sealed class ScanLog : ICodeLogger
    {
        public string FirstError { get; private set; }

        public void Error(LogCode code, params string[] messages) { Keep(code + Join(messages)); }
        public void Warning(LogCode code, params string[] messages) { }
        public void Info(LogCode code, params string[] messages) { }
        public void Error(string message) { Keep(message); }
        public void Warning(string message) { }
        public void Info(string message) { }

        private static string Join(string[] messages)
        {
            return messages == null || messages.Length == 0 ? string.Empty : ": " + string.Join(", ", messages);
        }

        private void Keep(string message)
        {
            if (FirstError == null && !string.IsNullOrEmpty(message)) FirstError = message;
        }
    }
}
