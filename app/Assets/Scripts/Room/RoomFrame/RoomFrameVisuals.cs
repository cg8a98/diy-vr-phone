using System.Collections.Generic;
using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>
    /// Builds the things you can see: the axes gizmo at the room origin, small labelled cubes (marker B, the test
    /// object) and the status text in front of the head. Only building, no logic, so an EditMode test can run it.
    /// </summary>
    public static class RoomFrameVisuals
    {
        public const float AxisLength = 0.3f;
        public const float AxisThickness = 0.012f;
        public const float HudDistance = 1.5f;
        public const float HudDrop = 0.3f;
        // TextMesh: characterSize * fontSize is roughly the line height in tenths of a millimetre at this font size.
        private const float CharacterSize = 0.004f;
        private const int FontSize = 64;

        /// <summary>Red X, green Y (up), blue Z (towards marker B) bars from the room origin, plus a label.</summary>
        public static GameObject BuildGizmo(Transform parent, List<Material> owned)
        {
            var root = new GameObject("Room Frame Gizmo");
            root.transform.SetParent(parent, false);
            Bar(root.transform, "Axis X", Vector3.right, new Color(1f, 0.2f, 0.2f), owned);
            Bar(root.transform, "Axis Y", Vector3.up, new Color(0.2f, 1f, 0.2f), owned);
            Bar(root.transform, "Axis Z", Vector3.forward, new Color(0.3f, 0.5f, 1f), owned);
            Cube(root.transform, "Origin", Vector3.zero, Vector3.one * (AxisThickness * 2.5f), Color.white, owned);
            var label = Text(root.transform, "Label", 1f);
            label.text = "ROOM 0 (marker A)";
            label.transform.localPosition = new Vector3(0f, AxisLength + 0.05f, 0f);
            return root;
        }

        /// <summary>A small coloured cube with a label above it.</summary>
        public static GameObject BuildPoint(Transform parent, string name, string labelText, Color color, float size, List<Material> owned)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Cube(root.transform, "Cube", Vector3.zero, Vector3.one * size, color, owned);
            var label = Text(root.transform, "Label", 1f);
            label.text = labelText;
            label.transform.localPosition = new Vector3(0f, size * 0.5f + 0.04f, 0f);
            return root;
        }

        /// <summary>The status text. Starts as a root object; <see cref="AttachHud"/> hangs it in front of the head.</summary>
        public static TextMesh BuildHud()
        {
            var tm = Text(null, "Room Frame HUD", 1f);
            tm.anchor = TextAnchor.UpperCenter;
            return tm;
        }

        /// <summary>Puts the status text below the centre of view, fixed to the head.</summary>
        public static void AttachHud(TextMesh hud, Transform head)
        {
            hud.transform.SetParent(head, false);
            hud.transform.localPosition = new Vector3(0f, -HudDrop, HudDistance);
            hud.transform.localRotation = Quaternion.identity;
        }

        private static void Bar(Transform parent, string name, Vector3 axis, Color color, List<Material> owned)
        {
            Vector3 scale = Vector3.one * AxisThickness + axis * (AxisLength - AxisThickness);
            Cube(parent, name, axis * (AxisLength * 0.5f), scale, color, owned);
        }

        private static void Cube(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color, List<Material> owned)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = 0;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            // Display only: no collider, so gaze and physics ignore it.
            RoomMarkers.DestroyNow(go.GetComponent<Collider>());

            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(UnlitShader()) { name = "RoomFrame " + name, color = color };
            owned.Add(mat);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static TextMesh Text(Transform parent, string name, float size)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMesh>();
            // TextMesh requires a MeshRenderer and adds one itself; AddComponent<MeshRenderer>() would return null.
            var mr = go.GetComponent<MeshRenderer>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.fontSize = FontSize;
            tm.characterSize = CharacterSize * size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            mr.sharedMaterial = font.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return tm;
        }

        private static Shader UnlitShader()
        {
            Shader s = Shader.Find("DiyVr/StereoUnlit");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Unlit/Color");
            return s;
        }
    }
}
