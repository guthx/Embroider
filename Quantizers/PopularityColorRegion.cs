using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Quantizers
{
    public class PopularityColorRegion
    {
        public int PixelCount;
        private Color _color;

        public PopularityColorRegion(Color color)
        {
            _color = color;
        }

        public Color Color
        {
            get
            {
                return _color.Normalized(PixelCount);
            }
        }

        public PopularityColorRegion AddColor(Color color)
        {
            _color.Add(color);
            PixelCount++;
            return this;
        }
    }
}
