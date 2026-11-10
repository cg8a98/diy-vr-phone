using System.IO;
using System.Text;
using UnityEngine;

namespace DiyVr.Tests
{
    /// <summary>
    /// Makes a tiny binary glTF in memory for tests: one triangle, 1 m wide and 1 m tall, optionally with a small
    /// generated texture. It contains no real room data (real scans never go into git).
    /// </summary>
    internal static class SyntheticGlb
    {
        public static byte[] Build(bool withTexture, int texturePx = 4, bool mainScene = true)
        {
            var bin = new MemoryStream();
            var w = new BinaryWriter(bin);
            float[] positions = { 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f };
            float[] uvs = { 0f, 0f, 1f, 0f, 0f, 1f };
            for (int i = 0; i < positions.Length; i++) w.Write(positions[i]); // bytes 0..35
            for (int i = 0; i < uvs.Length; i++) w.Write(uvs[i]);             // bytes 36..59
            w.Write((ushort)0);
            w.Write((ushort)1);
            w.Write((ushort)2);                                               // bytes 60..65
            w.Write((ushort)0);                                               // pad to 68
            int imageOffset = 68;
            int imageLength = 0;
            if (withTexture)
            {
                byte[] png = MakePng(texturePx);
                imageLength = png.Length;
                w.Write(png);
                while (bin.Length % 4 != 0) w.Write((byte)0);
            }
            w.Flush();
            byte[] binChunk = bin.ToArray();

            var json = new StringBuilder();
            json.Append("{'asset':{'version':'2.0'},");
            if (mainScene) json.Append("'scene':0,");
            json.Append("'scenes':[{'nodes':[0]}],");
            json.Append("'nodes':[{'mesh':0,'name':'room'}],");
            json.Append("'meshes':[{'primitives':[{'attributes':{'POSITION':0,'TEXCOORD_0':1},'indices':2,'material':0}]}],");
            if (withTexture)
            {
                json.Append("'materials':[{'name':'baked','pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicFactor':0,'roughnessFactor':1}}],");
                json.Append("'textures':[{'source':0}],");
                json.Append("'images':[{'bufferView':3,'mimeType':'image/png'}],");
            }
            else
            {
                json.Append("'materials':[{'name':'plain','pbrMetallicRoughness':{'baseColorFactor':[1,0,0,1],'metallicFactor':0,'roughnessFactor':1}}],");
            }
            json.Append("'buffers':[{'byteLength':" + binChunk.Length + "}],");
            json.Append("'bufferViews':[");
            json.Append("{'buffer':0,'byteOffset':0,'byteLength':36,'target':34962},");
            json.Append("{'buffer':0,'byteOffset':36,'byteLength':24,'target':34962},");
            json.Append("{'buffer':0,'byteOffset':60,'byteLength':6,'target':34963}");
            if (withTexture) json.Append(",{'buffer':0,'byteOffset':" + imageOffset + ",'byteLength':" + imageLength + "}");
            json.Append("],");
            json.Append("'accessors':[");
            json.Append("{'bufferView':0,'componentType':5126,'count':3,'type':'VEC3','min':[0,0,0],'max':[1,1,0]},");
            json.Append("{'bufferView':1,'componentType':5126,'count':3,'type':'VEC2'},");
            json.Append("{'bufferView':2,'componentType':5123,'count':3,'type':'SCALAR'}");
            json.Append("]}");

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json.ToString().Replace('\'', '"'));
            int jsonPadded = (jsonBytes.Length + 3) / 4 * 4;

            var glb = new MemoryStream();
            var g = new BinaryWriter(glb);
            g.Write(0x46546C67u); // "glTF"
            g.Write(2u);
            g.Write((uint)(12 + 8 + jsonPadded + 8 + binChunk.Length));
            g.Write((uint)jsonPadded);
            g.Write(0x4E4F534Au); // "JSON"
            g.Write(jsonBytes);
            for (int i = jsonBytes.Length; i < jsonPadded; i++) g.Write((byte)0x20);
            g.Write((uint)binChunk.Length);
            g.Write(0x004E4942u); // "BIN"
            g.Write(binChunk);
            g.Flush();
            return glb.ToArray();
        }

        private static byte[] MakePng(int px)
        {
            var texture = new Texture2D(px, px, TextureFormat.RGBA32, false);
            var pixels = new Color32[px * px];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = (i + i / px) % 2 == 0 ? new Color32(200, 120, 40, 255) : new Color32(40, 120, 200, 255);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }
    }
}
