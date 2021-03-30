using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Ditherers
{
    public class StuckiDitherer : Ditherer
    {
        public StuckiDitherer(Image<Rgb24> image, int maxDif = 255) : base(image, maxDif) { }
        private int[,] _coefficientMatrix = new int[,]
        {
            { 0, 0, 0, 8, 4 },
            { 2, 4, 8, 4, 2 },
            { 1, 2, 4, 2, 1 },
        };
        protected override int[,] coefficientMatrix => _coefficientMatrix;

        protected override int matrixPosH => 0;

        protected override int matrixPosW => 2;

        protected override int divisor => 42;
    }
}
