using Embroider.Quantizers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Ditherers
{
    public class NoneDitherer : Ditherer
    {
        public NoneDitherer(Image<Rgb24> image, int maxDif = 255) : base(image, maxDif)
        {
        }

        protected override int[,] coefficientMatrix => null;

        protected override int matrixPosH => 0;

        protected override int matrixPosW => 0;

        protected override int divisor => 0;

        public override void Dither(int h, int w, Quantizers.Color color)
        {
            return;
        }
    }
}
