using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Quantizers
{
    public class Color
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Color(int x, int y, int z)
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
            int x = 0, y = 0, z = 0;
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
            return (X << 16) + (Y << 8) + Z;
        }
    }
}
