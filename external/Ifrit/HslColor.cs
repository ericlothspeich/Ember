using System;
using Microsoft.Xna.Framework;

namespace Ifrit
{
    /// <summary>HSL <=> RGB conversion. Ember stores colors as HSL Vector3(H 0-360, S 0-1, L 0-1).</summary>
    public static class HslColor
    {
        /// <summary>Converts HSL (H 0-360, S 0-1, L 0-1) to an RGBA Color (A=255).</summary>
        public static Color ToRgb(Vector3 hsl)
        {
            float h = hsl.X;
            float s = hsl.Y;
            float l = hsl.Z;

            if (s == 0f)
            {
                byte v = (byte)(l * 255f + 0.5f);
                return new Color(v, v, v);
            }

            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;
            float hNorm = h / 360f;

            byte r = (byte)(HueToRgb(p, q, hNorm + 1f / 3f) * 255f + 0.5f);
            byte g = (byte)(HueToRgb(p, q, hNorm) * 255f + 0.5f);
            byte b = (byte)(HueToRgb(p, q, hNorm - 1f / 3f) * 255f + 0.5f);

            return new Color(r, g, b);
        }

        /// <summary>Converts an RGB Color to HSL Vector3(H 0-360, S 0-1, L 0-1).</summary>
        public static Vector3 FromRgb(Color color)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float l = (max + min) / 2f;
            float h = 0f, s = 0f;

            if (max != min)
            {
                float d = max - min;
                s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

                if (max == r)
                    h = (g - b) / d + (g < b ? 6f : 0f);
                else if (max == g)
                    h = (b - r) / d + 2f;
                else
                    h = (r - g) / d + 4f;

                h *= 60f;
            }

            return new Vector3(h, s, l);
        }

        /// <summary>Linearly interpolates between two HSL colors.</summary>
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }
    }
}
