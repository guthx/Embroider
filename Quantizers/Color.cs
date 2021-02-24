using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Quantizers
{
    public class Color
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Color(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Add(Color color)
        {
            X += color.X;
            Y += color.Y;
            Z += color.Z;
        }

        public Color Normalized(int pixelCount)
        {
            return new Color(X / pixelCount, Y / pixelCount, Z / pixelCount);
        }

        public static Color Average(List<Color> colors)
        {
            double x = 0, y = 0, z = 0;
            foreach(var color in colors)
            {
                x += color.X;
                y += color.Y;
                z += color.Z;
            }
            x /= colors.Count;
            y /= colors.Count;
            z /= colors.Count;
            return new Color(x, y, z);
        }

        public Color RgbToLab()
        {
            var R = X / 255.0;
            var G = Y / 255.0;
            var B = Z / 255.0;

            R = (R > 0.04045) ? Math.Pow((R + 0.055) / 1.055, 2.4) : R / 12.92;
            G = (G > 0.04045) ? Math.Pow((G + 0.055) / 1.055, 2.4) : G / 12.92;
            B = (B > 0.04045) ? Math.Pow((B + 0.055) / 1.055, 2.4) : B / 12.92;

            var x = (R * 0.4124 + G * 0.3576 + B * 0.1805) / 0.95047;
            var y = (R * 0.2126 + G * 0.7152 + B * 0.0722) / 1.00000;
            var z = (R * 0.0193 + G * 0.1192 + B * 0.9505) / 1.08883;
            // 7.787
            // 903.3
            x = (x > 0.008856) ? Math.Pow(x, 1.0 / 3) : (7.787 * x) + 16.0 / 116;
            y = (y > 0.008856) ? Math.Pow(y, 1.0 / 3) : (7.787 * y) + 16.0 / 116;
            z = (z > 0.008856) ? Math.Pow(z, 1.0 / 3) : (7.787 * z) + 16.0 / 116;

            return new Color(
                (116 * y) - 16,
                500 * (x - y),
                200 * (y - z)
                );
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj.GetType() == typeof(Color))
            {
                var color2 = (Color)obj;
                return this.X == color2.X && this.Y == color2.Y && this.Z == color2.Z;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ((int)X << 16) + ((int)Y << 8) + (int)Z;
        }
    }
}
