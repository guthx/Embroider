using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Embroider.Ditherers;
using static Embroider.Enums;

namespace Embroider.Quantizers
{
    public class PopularityQuantizer : Quantizer
    {
        public PopularityQuantizer(Image<Rgb, double> image, DithererType dithererType, ColorComparerType colorComparerType) : base(image, dithererType, colorComparerType) { }

        protected override void MakePalette(int paletteSize)
        {
            Palette.Clear();
            var colors = new ConcurrentDictionary<int, PopularityColorRegion>();
            foreach(var pixel in pixels)
            {
                int index = getColorIndex(pixel);
                colors.AddOrUpdate(index, new PopularityColorRegion(pixel), (i, region) => region.AddColor(pixel));
            }
            var regions = colors.ToList();
            regions.Sort(delegate (KeyValuePair<int, PopularityColorRegion> region1, KeyValuePair<int, PopularityColorRegion> region2)
            {
                if (region1.Value.PixelCount > region2.Value.PixelCount)
                    return 1;
                else if (region1.Value.PixelCount < region2.Value.PixelCount)
                    return -1;
                else
                    return 0;
            });
            regions.Reverse();
            for (int i=0; i<paletteSize && i<regions.Count; i++)
            {
                Palette.Add(regions[i].Value.Color);
            }
        }

        private static int getColorIndex(Color color)
        {
            int x = (int)color.X >> 2;
            int y = (int)color.Y >> 2;
            int z = (int)color.Z >> 2;
            return (x << 12) + (y << 6) + z;
        }
    }
}
