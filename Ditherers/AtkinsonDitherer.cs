using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Ditherers
{
    public class AtkinsonDitherer : Ditherer
    {
        public AtkinsonDitherer(Image<Lab, double> image, int maxDif = 12) : base(image, maxDif)
        {
        }
        private int[,] _coeficcientMatrix = new int[,]
        {
            { 0, 0, 1, 1 },
            { 1, 1, 1, 0 },
            { 0, 1, 0, 0 }
        };
        protected override int[,] coefficientMatrix => _coeficcientMatrix;

        protected override int matrixPosH => 0;

        protected override int matrixPosW => 1;
    }
}
