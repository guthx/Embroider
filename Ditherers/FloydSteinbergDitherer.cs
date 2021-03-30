using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Ditherers
{
    public class FloydSteinbergDitherer : Ditherer
    {
        public FloydSteinbergDitherer(Image<Rgb24> image, int maxDiff = 255) : base(image, maxDiff)
        {
        }
        private int[,] _coeficcientMatrix = new int[,] {
                { 0, 0, 7 },
                { 3, 5, 1 }
        };
        protected override int[,] coefficientMatrix => _coeficcientMatrix;

        protected override int matrixPosH => 0;

        protected override int matrixPosW => 1;

        protected override int divisor => 16;
    }
}
