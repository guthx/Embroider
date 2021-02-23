using Embroider.Quantizers;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Ditherers
{
    public enum DithererType
    {
        None,
        FloydSteinberg,
        Atkinson
    }
    public abstract class Ditherer
    {
        private Image<Lab, double> _image;
        private int _maxDif;
        protected abstract int[,] coefficientMatrix { get; }
        protected abstract int matrixPosH { get; }
        protected abstract int matrixPosW { get; }
        private int _coeficcientSum = 0;
        protected int coeficcientSum
        {
            get
            {
                if (_coeficcientSum == 0)
                {
                    for (int h = 0; h < coefficientMatrix.GetLength(0); h++)
                        for (int w = 0; w < coefficientMatrix.GetLength(1); w++)
                            _coeficcientSum += coefficientMatrix[h, w];
                }
                return _coeficcientSum;
            }
        }

        public Ditherer(Image<Lab, double> image, int maxDif = 255)
        {
            _image = image;
            _maxDif = maxDif;
        }
        public void SetImage(Image<Lab, double> image)
        {
            _image = image;
        }

        public virtual void Dither(int h, int w, Color color)
        {
            var errorX = _image[h, w].X - color.X;
            var errorY = _image[h, w].Y - color.Y;
            var errorZ = _image[h, w].Z - color.Z;

            if (errorX > _maxDif)
                errorX = _maxDif;
            else if (errorX < -_maxDif)
                errorX = -_maxDif;
            if (errorY > _maxDif)
                errorY = _maxDif;
            else if (errorY < -_maxDif)
                errorY = -_maxDif;
            if (errorZ > _maxDif)
                errorZ = _maxDif;
            else if (errorZ < -_maxDif)
                errorZ = -_maxDif;

            for (int i=0; i<coefficientMatrix.GetLength(0); i++)
                for (int j=0; j<coefficientMatrix.GetLength(1); j++)
                {
                    int h2 = h + i - matrixPosH;
                    int w2 = w + j - matrixPosW;
                    if (coefficientMatrix[i, j] != 0 && 
                        h2 < _image.Height &&
                        w2 < _image.Width &&
                        h2 >= 0 &&
                        w2 >= 0)
                    {
                        _image[h2, w2] = new Lab(
                            _image[h2, w2].X + errorX * coefficientMatrix[i, j] / coeficcientSum,
                            _image[h2, w2].Y + errorY * coefficientMatrix[i, j] / coeficcientSum,
                            _image[h2, w2].Z + errorZ * coefficientMatrix[i, j] / coeficcientSum
                            );
                    }
                }
        }
    }
}
