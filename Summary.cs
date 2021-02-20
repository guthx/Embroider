using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Embroider
{
    public class Summary
    {
        public ConcurrentDictionary<DmcFloss, int> FlossCount { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }
}
