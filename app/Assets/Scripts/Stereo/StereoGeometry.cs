using System;
using UnityEngine;

namespace DiyVr.Stereo
{
    public enum EyeSide { Left = 0, Right = 1 }

    /// <summary>
    /// Viewport / field-of-view maths for one eye. All "Rel" values are metres on the phone screen
    /// measured from that eye's lens centre (x right, y up). "Tan" values are tangents of view angles.
    /// </summary>
    public struct EyeGeometry
    {
        public EyeSide Side;
        public StereoParams P;
        /// <summary>Screen position of the lens centre relative to the screen centre (metres).</summary>
        public Vector2 LensCenter;
        /// <summary>Screen area of this eye relative to the lens centre.</summary>
        public double RelLeft, RelRight, RelBottom, RelTop;
        /// <summary>Rendering frustum, tangents of the angles (left/bottom are negative).</summary>
        public double TanLeft, TanRight, TanBottom, TanTop;

        public double FovLeftDeg { get { return Math.Atan(-TanLeft) * 180.0 / Math.PI; } }
        public double FovRightDeg { get { return Math.Atan(TanRight) * 180.0 / Math.PI; } }
        public double FovDownDeg { get { return Math.Atan(-TanBottom) * 180.0 / Math.PI; } }
        public double FovUpDeg { get { return Math.Atan(TanTop) * 180.0 / Math.PI; } }
    }

    public static class StereoGeometry
    {
        /// <summary>Tangent of the view angle seen at one screen edge: undistort the edge, then clamp.</summary>
        private static double EdgeTan(double relMetres, StereoParams p)
        {
            double s = Math.Abs(relMetres) / p.ScreenToLens;
            double t = CardboardDistortion.DistortInverse(s, p.K1, p.K2);
            t = Math.Min(t, Math.Tan(p.MaxHalfFovRadians));
            return relMetres < 0 ? -t : t;
        }

        public static EyeGeometry ComputeEye(StereoParams p, EyeSide side)
        {
            double cx = side == EyeSide.Left ? -p.LensSpacing / 2.0 : p.LensSpacing / 2.0;
            double xMin = side == EyeSide.Left ? -p.ScreenWidth / 2.0 : 0.0;
            double xMax = side == EyeSide.Left ? 0.0 : p.ScreenWidth / 2.0;
            var g = new EyeGeometry
            {
                Side = side,
                P = p,
                LensCenter = new Vector2((float)cx, (float)p.LensCenterYOffset),
                RelLeft = xMin - cx,
                RelRight = xMax - cx,
                RelBottom = -p.ScreenHeight / 2.0 - p.LensCenterYOffset,
                RelTop = p.ScreenHeight / 2.0 - p.LensCenterYOffset
            };
            g.TanLeft = EdgeTan(g.RelLeft, p);
            g.TanRight = EdgeTan(g.RelRight, p);
            g.TanBottom = EdgeTan(g.RelBottom, p);
            g.TanTop = EdgeTan(g.RelTop, p);
            return g;
        }

        /// <summary>
        /// Where in the eye render texture (0..1) the pixel for a given screen point comes from.
        /// The screen point is relative to the lens centre, in metres.
        /// </summary>
        public static Vector2 ScreenToTextureUV(EyeGeometry g, double relX, double relY)
        {
            double sx = relX / g.P.ScreenToLens;
            double sy = relY / g.P.ScreenToLens;
            double r = Math.Sqrt(sx * sx + sy * sy);
            // Near the centre the ratio tends to 1 / DistortionFactor(0) = 1.
            double scale = r > 1e-12 ? CardboardDistortion.DistortInverse(r, g.P.K1, g.P.K2) / r : 1.0;
            double tx = sx * scale;
            double ty = sy * scale;
            double u = (tx - g.TanLeft) / (g.TanRight - g.TanLeft);
            double v = (ty - g.TanBottom) / (g.TanTop - g.TanBottom);
            return new Vector2((float)u, (float)v);
        }

        /// <summary>Asymmetric projection matrix for the eye frustum.</summary>
        public static Matrix4x4 Projection(EyeGeometry g, float near, float far)
        {
            return Matrix4x4.Frustum((float)(g.TanLeft * near), (float)(g.TanRight * near),
                                     (float)(g.TanBottom * near), (float)(g.TanTop * near), near, far);
        }

        /// <summary>
        /// Builds the distortion mesh for one eye in "composite space": x from -aspect..0 (left eye) or 0..aspect
        /// (right eye), y from -1..1, z = 0. UV0 says where to read the eye render texture.
        /// </summary>
        public static Mesh BuildDistortionMesh(EyeGeometry g, float aspect, int gridSize)
        {
            int n = Mathf.Max(2, gridSize);
            var verts = new Vector3[n * n];
            var uvs = new Vector2[n * n];
            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    double fx = i / (double)(n - 1), fy = j / (double)(n - 1);
                    double relX = g.RelLeft + fx * (g.RelRight - g.RelLeft);
                    double relY = g.RelBottom + fy * (g.RelTop - g.RelBottom);
                    double scrX = g.LensCenter.x + relX;
                    double scrY = g.LensCenter.y + relY;
                    int k = j * n + i;
                    verts[k] = new Vector3((float)(scrX / (g.P.ScreenWidth / 2.0) * aspect),
                                           (float)(scrY / (g.P.ScreenHeight / 2.0)), 0f);
                    uvs[k] = ScreenToTextureUV(g, relX, relY);
                }
            }
            var tris = new int[(n - 1) * (n - 1) * 6];
            int t = 0;
            for (int j = 0; j < n - 1; j++)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            }
            var mesh = new Mesh { name = "DistortionMesh" + g.Side };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            return mesh;
        }
    }
}
