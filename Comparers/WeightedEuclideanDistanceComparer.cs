using Embroider.Quantizers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Comparers
{
    public class WeightedEuclideanDistanceComparer : ColorComparer
    {
        public override double Compare(Color color1, Color color2)
        {
            return Math.Sqrt(
                Math.Pow((color1.X - color2.X) * 30, 2) +
                Math.Pow((color1.Y - color2.Y) * 59, 2) +
                Math.Pow((color1.Z - color2.Z) * 11, 2)
                );
        }
    }
}
