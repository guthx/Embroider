using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider
{
    [Serializable]
    public class DmcFloss
    {
        public string Number { get; set; }
        public string Description { get; set; }
        public double L { get; set; }
        public double a { get; set; }
        public double b { get; set; }
        public double Red { get; set; }
        public double Green { get; set; }
        public double Blue { get; set; }
    }
}
