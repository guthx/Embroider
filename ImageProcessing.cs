using Emgu.CV;
using Emgu.CV.Structure;
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

namespace Embroider
{
    public static class ImageProcessing
    {
        public static List<DmcFloss> DmcFlosses;
        public static void IncreaseContrast(Image<Rgb, double> image, float contrast)
        {
            double sigmoid(double x)
            {
                return 1*(-128 + 256 * (1 / (1 + Math.Exp(-x / 40))));
            }
            for (int w = 0; w < image.Width; w++)
                for (int h = 0; h < image.Height; h++)
                {
                    var newY = sigmoid(image.Data[h, w, 1] - 128) + 128;
                    var newZ = sigmoid(image.Data[h, w, 2] - 128) + 128;

                    // var newY = (image.Data[h, w, 1] - 128) * contrast + image.Data[h, w, 1];
                    // var newZ = (image.Data[h, w, 2] - 128) * contrast + image.Data[h, w, 2];

                    image[h, w] = new Rgb(image.Data[h, w, 0], newY, newZ);
                }
                    
        }

        public static Image<Rgb, double> MeanReduce(Image<Rgb, double> image, int pixelSize)
        {
            var newImage = new Image<Rgb, double>((image.Width - 1) / pixelSize + 1, (image.Height - 1) / pixelSize + 1);
            var pixelValues = new double[(image.Height - 1) / pixelSize + 1, (image.Width - 1) / pixelSize + 1, 3];
            var pixelCount = new int[(image.Height - 1) / pixelSize + 1, (image.Width - 1) / pixelSize + 1];
            for (int h=0; h<image.Height; h++)
            {
                for(int w=0; w<image.Width; w++)
                {
                    pixelValues[h / pixelSize, w / pixelSize, 0] += image.Data[h, w, 0];
                    pixelValues[h / pixelSize, w / pixelSize, 1] += image.Data[h, w, 1];
                    pixelValues[h / pixelSize, w / pixelSize, 2] += image.Data[h, w, 2];
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
                    newImage[h, w] = new Rgb(x, y, z);
                }
            }
            return newImage;
        }

        public static Image<Rgb, double> Stretch(Image<Rgb, double> image, int sizeMultiplier, bool net = false)
        {
            if (!net)
            {
                var newImage = new Image<Rgb, double>(image.Width * sizeMultiplier, image.Height * sizeMultiplier);
                for (int h=0; h<newImage.Height; h++)
                {
                    for(int w=0; w<newImage.Width; w++)
                    {
                        newImage[h, w] = image[h / sizeMultiplier, w / sizeMultiplier];
                    }
                }
                return newImage;
            }
            else
            {
                var newImage = new Image<Rgb, double>(image.Width * sizeMultiplier + image.Width - 1, image.Height * sizeMultiplier + image.Height - 1);
                for (int h=0; h<newImage.Height; h++)
                {
                    for (int w=0; w<newImage.Width; w++)
                    {
                        if ( (h+1) % (sizeMultiplier+1) == 0 || (w+1) % (sizeMultiplier+1) == 0)
                        {
                            newImage[h, w] = new Rgb(120, 120, 120);
                        }
                        else
                            newImage[h, w] = image[(h - (h+1) / (sizeMultiplier+1)) / sizeMultiplier, (w - (w+1) / (sizeMultiplier+1)) / sizeMultiplier];
                    }
                }
                return newImage;
            }
            
        }
        public static void ReplacePixelsWithDMC(Image<Rgb, double> image, ColorComparer colorComparer, Ditherer ditherer)
        {
            var colorsCount = new Dictionary<DmcFloss, int>();
            DmcFlosses = Flosses.Dmc;

            for(int h=0; h<image.Height; h++)
            {
                for(int w=0; w<image.Width; w++)
                {
                    var deltaE = new double[DmcFlosses.Count];
                    for (int i = 0; i < DmcFlosses.Count; i++)
                    {
                        var color1 = new Color(DmcFlosses[i].Red, DmcFlosses[i].Green, DmcFlosses[i].Blue);
                        var color2 = new Color(image.Data[h, w, 0], image.Data[h, w, 1], image.Data[h, w, 2]);
                        /*
                        deltaE[i] = Math.Sqrt(
                            Math.Pow((DmcFlosses[i].L) - (image.Data[h, w, 0]), 2) +
                            Math.Pow(DmcFlosses[i].a - image.Data[h, w, 1], 2) +
                            Math.Pow(DmcFlosses[i].b - image.Data[h, w, 2], 2));
                        */
                        deltaE[i] = colorComparer.Compare(color1, color2);
                        //deltaE[i] = Lab2.Compare(color1, color2);
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
                    image[h, w] = new Rgb(dmc.Red, dmc.Green, dmc.Blue);
                }
            }
        }
    }
}