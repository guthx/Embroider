using Embroider.Quantizers;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Ditherers
{
    public class NoneDitherer : Ditherer
    {
        public NoneDitherer(Image<Lab, double> image, int maxDif = 255) : base(image, maxDif)
        {
        }

        protected override int[,] coefficientMatrix => null;

        protected override int matrixPosH => 0;

        protected override int matrixPosW => 0;

        public override void Dither(int h, int w, Color color)
        {
            return;
        }
    }
}
