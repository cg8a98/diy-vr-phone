using System.Collections.Generic;
using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>
    /// Draws the scan see-through while aligning, so the real walls (camera view) show through it, and puts the
    /// normal materials back afterwards. Uses Unity's built-in "Sprites/Default" shader: unlit, alpha blended,
    /// and always in the Android build (the plane view uses it too), so no new shader is needed.
    /// </summary>
    public sealed class ScanGhost
    {
        public const string ShaderName = "Sprites/Default";
        /// <summary>How solid the scan is while aligning: 0 = invisible, 1 = solid.</summary>
        public const float Alpha = 0.35f;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Material[]> _originals = new List<Material[]>();
        private readonly Dictionary<Material, Material> _bySource = new Dictionary<Material, Material>();
        private readonly List<Material> _ghosts = new List<Material>();

        public bool IsOn { get; private set; }
        /// <summary>The see-through materials now in use (empty when off).</summary>
        public IReadOnlyList<Material> Materials { get { return _ghosts; } }

        /// <summary>Swaps every material under <paramref name="root"/> for a see-through copy. False if that is not possible.</summary>
        public bool Show(GameObject root)
        {
            if (IsOn) Restore();
            if (root == null) return false;
            Shader shader = Shader.Find(ShaderName);
            if (shader == null) return false;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] originals = renderer.sharedMaterials;
                var ghosts = new Material[originals.Length];
                for (int i = 0; i < originals.Length; i++) ghosts[i] = GhostOf(originals[i], shader);
                _renderers.Add(renderer);
                _originals.Add(originals);
                renderer.sharedMaterials = ghosts;
            }
            IsOn = true;
            return true;
        }

        /// <summary>Puts the scan's own materials back and frees the see-through ones.</summary>
        public void Restore()
        {
            for (int i = 0; i < _renderers.Count; i++)
                if (_renderers[i] != null) _renderers[i].sharedMaterials = _originals[i];
            _renderers.Clear();
            _originals.Clear();
            for (int i = 0; i < _ghosts.Count; i++) ScanAlignVisuals.DestroyObject(_ghosts[i]);
            _ghosts.Clear();
            _bySource.Clear();
            IsOn = false;
        }

        private Material GhostOf(Material source, Shader shader)
        {
            Material ghost;
            if (source != null && _bySource.TryGetValue(source, out ghost)) return ghost;

            Color color = Color.white;
            Texture texture = null;
            if (source != null)
            {
                if (source.HasProperty("_Color")) color = source.GetColor("_Color");
                if (source.HasProperty("_MainTex")) texture = source.GetTexture("_MainTex");
            }
            color.a = Alpha;
            ghost = new Material(shader) { name = "ScanGhost" };
            ghost.SetColor("_Color", color);
            if (texture != null) ghost.SetTexture("_MainTex", texture);
            _ghosts.Add(ghost);
            if (source != null) _bySource[source] = ghost;
            return ghost;
        }
    }

    /// <summary>Scene helpers for scan alignment.</summary>
    public static class ScanAlignVisuals
    {
        public const string LabelName = "AlignStatusLabel";
        /// <summary>Label position in front of the head: 1.2 m away, above the centre (the scan and room-frame texts sit below it).</summary>
        public static readonly Vector3 LabelOffset = new Vector3(0f, 0.28f, 1.2f);

        /// <summary>The align status label, as a child of <paramref name="head"/>. Same look as the scan loader's label.</summary>
        public static TextMesh BuildLabel(Transform head)
        {
            TextMesh label = ScanLoadController.BuildLabel(head);
            label.gameObject.name = LabelName;
            label.transform.localPosition = LabelOffset;
            return label;
        }

        /// <summary>
        /// The box around every mesh under <paramref name="root"/>, in the root's own coordinates (so it does not
        /// matter where the scan is at the moment).
        /// </summary>
        public static Bounds LocalBounds(GameObject root)
        {
            bool any = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            if (root == null) return bounds;
            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null) continue;
                Matrix4x4 m = toRoot * filter.transform.localToWorldMatrix;
                Vector3 c = mesh.bounds.center;
                Vector3 e = mesh.bounds.extents;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        c.x + ((i & 1) == 0 ? -e.x : e.x),
                        c.y + ((i & 2) == 0 ? -e.y : e.y),
                        c.z + ((i & 4) == 0 ? -e.z : e.z));
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (!any)
                    {
                        bounds = new Bounds(p, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(p);
                    }
                }
            }
            return bounds;
        }

        internal static void DestroyObject(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}
