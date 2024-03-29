﻿using Embroider.Ditherers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;
using static Embroider.Enums;

namespace Embroider.Quantizers
{
    public class MedianCutQuantizer : Quantizer
    {
        public MedianCutQuantizer(Image<Rgb24> image, 
            DithererType dithererType, 
            ColorComparerType colorComparerType,
            int dithererStrength = 255) : base(image, dithererType, colorComparerType, dithererStrength) { }


        private void split(List<Color> pixels, int depth)
        {
            if (depth == 0)
            {
                Palette.Add(Color.Average(pixels));
                return;
            }

            double xMax = 0, xMin = 255, yMax = 0, yMin = 255, zMax = 0, zMin = 255;
            foreach(var pixel in pixels)
            {
                if (pixel.X > xMax)
                    xMax = pixel.X;
                if (pixel.X < xMin)
                    xMin = pixel.X;
                if (pixel.Y > yMax)
                    yMax = pixel.Y;
                if (pixel.Y < yMin)
                    yMin = pixel.Y;
                if (pixel.Z > zMax)
                    zMax = pixel.Z;
                if (pixel.Z < zMin)
                    zMin = pixel.Z;
            }

            double xRange = xMax - xMin;
            double yRange = yMax - yMin;
            double zRange = zMax - zMin;

            if (xRange >= yRange && xRange >= zRange)
            {
                pixels.Sort(delegate (Color pixel1, Color pixel2)
                {
                    if (pixel1.X > pixel2.X)
                        return 1;
                    else if (pixel1.X < pixel2.X)
                        return -1;
                    else
                        return 0;
                });
            }
            else if (yRange >= xRange && yRange >= zRange)
            {
                pixels.Sort(delegate (Color pixel1, Color pixel2)
                {
                    if (pixel1.Y > pixel2.Y)
                        return 1;
                    else if (pixel1.Y < pixel2.Y)
                        return -1;
                    else
                        return 0;
                });
            }
            else
            {
                pixels.Sort(delegate (Color pixel1, Color pixel2)
                {
                    if (pixel1.Z > pixel2.Z)
                        return 1;
                    else if (pixel1.Z < pixel2.Z)
                        return -1;
                    else
                        return 0;
                });
            }

            int medianIndex = (pixels.Count - 1) / 2;
            split(pixels.GetRange(0, medianIndex), depth - 1);
            split(pixels.GetRange(medianIndex, pixels.Count/2), depth - 1);
        }

        protected override void MakePalette(int paletteSize)
        {
            int depth = (int)Math.Log2(paletteSize);
            Palette.Clear();
            split(pixels, depth);
        }

    }
}
