using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Ditherers
{
    public class PigeonDitherer : Ditherer
    {
        public PigeonDitherer(Image<Rgb24> image, int maxDif = 255) : base(image, maxDif)
        {
        }
        private int[,] _coefficientMatrix = new int[,]
        {
            { 0, 0, 0, 2, 1 },
            { 0, 2, 2, 2, 0 },
            { 1, 0, 1, 0, 1 }
        };
        protected override int[,] coefficientMatrix => _coefficientMatrix;

        protected override int matrixPosH => 0;

        protected override int matrixPosW => 2;

        protected override int divisor => 14;
    }
}
