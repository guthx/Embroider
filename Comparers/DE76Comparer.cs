using Embroider.Quantizers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Comparers
{
    public class DE76Comparer : ColorComparer
    {
        public override double Compare(Color color1, Color color2)
        {
            var lab1 = color1.RgbToLab();
            var lab2 = color2.RgbToLab();

            return Math.Sqrt(
                Math.Pow(lab1.X - lab2.X, 2) +
                Math.Pow(lab1.Y - lab2.Y, 2) +
                Math.Pow(lab1.Z - lab2.Z, 2));
        }
    }
}
