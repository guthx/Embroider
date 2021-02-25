using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Ditherers
{
    public class SierraDitherer : Ditherer
    {
        public SierraDitherer(Image<Rgb, double> image, int maxDif = 255) : base(image, maxDif)
        {
        }
        private int[,] _coefficientMatrix = new int[,]
        {
            { 0, 0, 0, 5, 3 },
            { 2, 4, 5, 4, 2 },
            { 0, 2, 3, 2, 0 }
        };
        protected override int[,] coefficientMatrix => _coefficientMatrix;

        protected override int matrixPosH => 0;

        protected override int matrixPosW => 2;

        protected override int divisor => 32;
    }
}
