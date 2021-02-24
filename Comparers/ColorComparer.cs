using Embroider.Quantizers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Comparers
{
    public abstract class ColorComparer
    {
        public abstract double Compare(Color color1, Color color2);
    }
}
