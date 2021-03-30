using Embroider.Quantizers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Ditherers
{
    public abstract class Ditherer
    {
        private Image<Rgb24> _image;
        private int _maxDif;
        protected abstract int[,] coefficientMatrix { get; }
        protected abstract int matrixPosH { get; }
        protected abstract int matrixPosW { get; }
        protected abstract int divisor { get; }


        public Ditherer(Image<Rgb24> image, int maxDif = 255)
        {
            _image = image;
            _maxDif = maxDif;
        }
        public void SetImage(Image<Rgb24> image)
        {
            _image = image;
        }

        public virtual void Dither(int h, int w, Quantizers.Color color)
        {
            var errorX = _image[w, h].R - color.X;
            var errorY = _image[w, h].G - color.Y;
            var errorZ = _image[w, h].B - color.Z;

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
                        _image[w2, h2] = new Rgb24(
                            (byte)Math.Clamp(_image[w2, h2].R + errorX * coefficientMatrix[i, j] / divisor, 0, 255),
                            (byte)Math.Clamp(_image[w2, h2].G + errorY * coefficientMatrix[i, j] / divisor, 0, 255),
                            (byte)Math.Clamp(_image[w2, h2].B + errorZ * coefficientMatrix[i, j] / divisor, 0, 255)
                            );
                    }
                }
        }
    }
}
