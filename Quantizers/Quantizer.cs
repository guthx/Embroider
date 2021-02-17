using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Embroider.Quantizers
{
    public abstract class Quantizer
    {
        public List<Color> Palette;
        protected List<Color> pixels;
        protected Image<Lab, double> _image;
        
        public Quantizer(Image<Lab, double> image)
        {
            pixels = new List<Color>();
            Palette = new List<Color>();
            _image = image;
            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    pixels.Add(new Color((int)image[h, w].X, (int)image[h, w].Y, (int)image[h, w].Z));
                }
            }
        }
        public abstract void MakePalette(int paletteSize);
        public virtual void ChangePaletteToDMC()
        {
            var dmcColors = Flosses.Dmc;
            for (int i = 0; i < Palette.Count; i++)
            {
                var deltaE = new double[dmcColors.Count];
                var color1 = new Lab2(Palette[i].X / 2.55, Palette[i].Y - 128, Palette[i].Z - 128);
                for (int j = 0; j < dmcColors.Count; j++)
                {
                    var color2 = new Lab2(dmcColors[j].L / 2.55, dmcColors[j].a - 128, dmcColors[j].b - 128);
                    deltaE[j] = Lab2.CompareCMC(color1, color2);
                }
                var dmc = dmcColors[Array.IndexOf(deltaE, deltaE.Min())];
                Palette[i] = new Color((int)dmc.L, (int)dmc.a, (int)dmc.b);
            }
        }

        public virtual Image<Lab, double> GetQuantizedImage()
        {
            var newImage = _image.Copy();
            for (int h = 0; h < newImage.Height; h++)
            {
                for (int w = 0; w < newImage.Width; w++)
                {
                    var deltaE = new double[Palette.Count];
                    var color1 = new Lab2(newImage[h, w].X / 2.55, newImage[h, w].Y - 128, newImage[h, w].Z - 128);
                    for (int i = 0; i < Palette.Count; i++)
                    {
                        var color2 = new Lab2(Palette[i].X / 2.55, Palette[i].Y - 128, Palette[i].Z - 128);
                        deltaE[i] = Lab2.CompareCMC(color1, color2);
                    }
                    var color = Palette[Array.IndexOf(deltaE, deltaE.Min())];
                    newImage[h, w] = new Lab(color.X, color.Y, color.Z);
                }
            }
            return newImage;
        }
    }
}
