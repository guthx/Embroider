﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Embroider.Ditherers;
using static Embroider.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Embroider.Quantizers
{
    public class SimplePopularityQuantizer : Quantizer
    {
        public SimplePopularityQuantizer(Image<Rgb24> image, 
            DithererType dithererType, 
            ColorComparerType colorComparerType,
            int dithererStrength = 255) : base(image, dithererType, colorComparerType, dithererStrength) { }
        protected override void MakePalette(int paletteSize)
        {
            Palette.Clear();
            var colors = new ConcurrentDictionary<Color, int>();
            foreach(var pixel in pixels)
            {
                colors.AddOrUpdate(pixel, 1, (color, count) => count + 1);
            }
            var colorsList = colors.ToList();
            colorsList.Sort(delegate (KeyValuePair<Color, int> color1, KeyValuePair<Color, int> color2)
            {
                if (color1.Value > color2.Value)
                    return 1;
                else if (color1.Value < color2.Value)
                    return -1;
                else
                    return 0;
            });
            colorsList.Reverse();

            for (int i=0; i<paletteSize && i<colorsList.Count; i++)
            {
                Palette.Add(colorsList[i].Key);
            }
        }
    }
}
