using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.IO;
using CsvHelper;
using System.Globalization;
using System.Linq;
using Embroider.Comparers;
using Embroider.Ditherers;
using Embroider.Quantizers;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Embroider
{
    public static class ImageProcessing
    {
        public static List<Floss> DmcFlosses;

        public static Image<Rgb24> MeanReduce(Image<Rgb24> image, int pixelSize)
        {
            var newImage = new Image<Rgb24>((image.Width - 1) / pixelSize + 1, (image.Height - 1) / pixelSize + 1);
            var pixelValues = new double[(image.Height - 1) / pixelSize + 1, (image.Width - 1) / pixelSize + 1, 3];
            var pixelCount = new int[(image.Height - 1) / pixelSize + 1, (image.Width - 1) / pixelSize + 1];
            for (int h=0; h<image.Height; h++)
            {
                var pixelRow = image.GetPixelRowSpan(h);
                for(int w=0; w<image.Width; w++)
                {
                    pixelValues[h / pixelSize, w / pixelSize, 0] += pixelRow[w].R;
                    pixelValues[h / pixelSize, w / pixelSize, 1] += pixelRow[w].G;
                    pixelValues[h / pixelSize, w / pixelSize, 2] += pixelRow[w].B;
                    pixelCount[h / pixelSize, w / pixelSize]++;
                }
            }
            for (int h=0; h<newImage.Height; h++)
            {
                for(int w=0; w<newImage.Width; w++)
                {
                    var x = pixelValues[h, w, 0] / pixelCount[h, w];
                    var y = pixelValues[h, w, 1] / pixelCount[h, w];
                    var z = pixelValues[h, w, 2] / pixelCount[h, w];
                    newImage[w, h] = new Rgb24((byte)x, (byte)y, (byte)z);
                }
            }
            return newImage;
        }

        public static Image<Rgb24> Stretch(Image<Rgb24> image, int sizeMultiplier, bool net = false)
        {
            if (!net)
            {
                var newImage = new Image<Rgb24>(image.Width * sizeMultiplier, image.Height * sizeMultiplier);
                for (int h=0; h<newImage.Height; h++)
                {
                    var pixelRow = newImage.GetPixelRowSpan(h);
                    for(int w=0; w<newImage.Width; w++)
                    {
                        pixelRow[w] = image[w / sizeMultiplier, h / sizeMultiplier];
                    }
                }
                return newImage;
            }
            else
            {
                var newImage = new Image<Rgb24>(image.Width * sizeMultiplier + image.Width - 1, image.Height * sizeMultiplier + image.Height - 1);
                for (int h=0; h<newImage.Height; h++)
                {
                    var pixelRow = newImage.GetPixelRowSpan(h);
                    for (int w=0; w<newImage.Width; w++)
                    {
                        if ( (h+1) % (sizeMultiplier+1) == 0 || (w+1) % (sizeMultiplier+1) == 0)
                        {
                            pixelRow[w] = new Rgb24(120, 120, 120);
                        }
                        else
                            pixelRow[w] = image[(w - (w+1) / (sizeMultiplier+1)) / sizeMultiplier, (h - (h+1) / (sizeMultiplier+1)) / sizeMultiplier];
                    }
                }
                return newImage;
            }
            
        }
        public static void ReplacePixelsWithDMC(Image<Rgb24> image, ColorComparer colorComparer, Ditherer ditherer)
        {
            var colorsCount = new Dictionary<Floss, int>();
            DmcFlosses = Flosses.Dmc();

            for(int h=0; h<image.Height; h++)
            {
                var pixelRow = image.GetPixelRowSpan(h);
                for(int w=0; w<image.Width; w++)
                {
                    var deltaE = new double[DmcFlosses.Count];
                    for (int i = 0; i < DmcFlosses.Count; i++)
                    {
                        var color1 = new Quantizers.Color(DmcFlosses[i].Red, DmcFlosses[i].Green, DmcFlosses[i].Blue);
                        var color2 = new Quantizers.Color(pixelRow[w].R, pixelRow[w].G, pixelRow[w].B);
                        deltaE[i] = colorComparer.Compare(color1, color2);
                    }
                    var dmc = DmcFlosses[Array.IndexOf(deltaE, deltaE.Min())];
                    if (colorsCount.ContainsKey(dmc))
                    {
                        colorsCount[dmc]++;
                    }
                    else
                    {
                        colorsCount.Add(dmc, 1);
                    }
                    pixelRow[w] = new Rgb24((byte)dmc.Red, (byte)dmc.Green, (byte)dmc.Blue);
                }
            }
        }
    }
}