using System;
using System.Collections.Generic;
using System.Text;
using static Embroider.Enums;

namespace Embroider.Quantizers
{
    public class Color
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Color(double x, double y, double z, ColorSpace colorSpace, ColorSpace from = ColorSpace.Rgb, bool normalized = false)
        {
            Color rgbColor;
            switch (from)
            {
                case ColorSpace.Rgb:
                    rgbColor = new Color(x, y, z);
                    break;
                case ColorSpace.Lab:
                    rgbColor = new Color(x, y, z).LabToRgb(normalized);
                    break;
                default:
                    rgbColor = new Color(x, y, z);
                    break;
            }
            switch (colorSpace)
            {
                case ColorSpace.Rgb:
                    X = rgbColor.X;
                    Y = rgbColor.Y;
                    Z = rgbColor.Z;
                    break;
                case ColorSpace.Lab:
                    var lab = rgbColor.RgbToLab(normalized);
                    X = lab.X;
                    Y = lab.Y;
                    Z = lab.Z;
                    break;
                default:
                    X = rgbColor.X;
                    Y = rgbColor.Y;
                    Z = rgbColor.Z;
                    break;
            }
        }

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

        public Color RgbToLab(bool normalized = false)
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
            if (!normalized)
                return new Color(
                    (116 * y) - 16,
                    500 * (x - y),
                    200 * (y - z)
                    );
            else
                return new Color(
                    ((116 * y) - 16) * (100/255),
                    500 * (x - y) - 128,
                    200 * (y - z) - 128
                    );
        }

        public Color LabToRgb(bool normalized = false)
        {
            double L = X, a = Y, b = Z;
            if (normalized)
            {
                L = L * (255 / 100);
                a = a + 128;
                b = b + 128;
            }
            var fy = (L + 16) / 116;
            var fx = a / 500 + fy;
            var fz = fy - b / 200;
            var k = 903.3;
            var e = 0.008856;
            var x = Math.Pow(fx, 3) > e ? Math.Pow(fx, 3) : (116 * fx - 16) / k;
            var y = L > k * e ? Math.Pow(fy, 3) : L / k;
            var z = Math.Pow(fz, 3) > e ? Math.Pow(fz, 3) : (116 * fz - 16) / k;

            x = x * 0.95047;
            y = y * 1.00000;
            z = z * 1.08883;

            var red = x * 3.2404542 + y * -1.5371853 + z * -0.4985314;
            var green = x * -0.9692660 + y * 1.8760108 + z * 0.0415560;
            var blue = x * 0.0556434 + y * -0.2040259 + z * 1.0572252;

            red = red <= 0.0031308 ? red * 12.92 : Math.Pow(red * 1.055, 1 / 2.4) - 0.055;
            green = green <= 0.0031308 ? green * 12.92 : Math.Pow(green * 1.055, 1 / 2.4) - 0.055;
            blue = blue <= 0.0031308 ? blue * 12.92 : Math.Pow(blue * 1.055, 1 / 2.4) - 0.055;

            return new Color(
                red * 255,
                green * 255,
                blue * 255
                );
        }

        public void Clamp()
        {
            X = Math.Clamp(X, 0, 255);
            Y = Math.Clamp(Y, 0, 255);
            Z = Math.Clamp(Z, 0, 255);
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
