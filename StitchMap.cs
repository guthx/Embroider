using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider
{
    [Serializable]
    public class Stitch
    {
        public int ColorIndex { get; set; }
        public bool Stitched { get; set; } = false;
    }
    [Serializable]
    public class StitchMap
    {
        public Stitch[,] Stitches { get; set; }
        public DmcFloss[] DmcFlosses { get; set; }

        public StitchMap(DmcFloss[,] dmcFlossMap, List<DmcFloss> palette)
        {
            var stitches = new Stitch[dmcFlossMap.GetLength(1), dmcFlossMap.GetLength(0)];
            for (int h=0; h<dmcFlossMap.GetLength(1); h++)
                for (int w=0; w<dmcFlossMap.GetLength(0); w++)
                {
                    var index = palette.IndexOf(dmcFlossMap[w, h]);
                    stitches[h, w] = new Stitch
                    {
                        ColorIndex = index,
                        Stitched = false
                    };
                }
            DmcFlosses = palette.ToArray();
            Stitches = stitches;
        }
    }
}
