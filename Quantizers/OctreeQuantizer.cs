using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Embroider.Ditherers;

namespace Embroider.Quantizers
{
    public enum MergeMode
    {
        LEAST_IMPORTANT, MOST_IMPORTANT
    }
    public class OctreeQuantizer : Quantizer
    {
        public OctreeNode Root;
        public List<OctreeNode>[] Levels;
        public int MaxDepth { get; }
        private MergeMode _mergeMode;

        public OctreeQuantizer(Image<Lab, double> image, int maxDepth, MergeMode mergeMode = MergeMode.LEAST_IMPORTANT, DithererType dithererType = DithererType.None) : base(image, dithererType)
        {
            MaxDepth = maxDepth;
            _mergeMode = mergeMode;
            Levels = new List<OctreeNode>[maxDepth];
            for (int i = 0; i < maxDepth; i++)
                Levels[i] = new List<OctreeNode>();
            Root = new OctreeNode(0, this);
        }

        override protected void MakePalette(int paletteSize)
        {
            Root = new OctreeNode(0, this);
            Palette.Clear();
            for (int i=0; i<pixels.Count; i++)
            {
                addColor(pixels[i]);
            }

            var leafCount = Root.LeafNodes.Length;
            for (int i = MaxDepth - 1; i >= 0; i--)
            {
                if (Levels[i].Count > 0)
                {
                    Levels[i].Sort(delegate (OctreeNode node1, OctreeNode node2)
                    {
                        int count1 = node1.PixelCount;
                        int count2 = node2.PixelCount;
                        if (count1 > count2)
                            return 1;
                        else if (count1 < count2)
                            return -1;
                        else
                            return 0;
                    });
                    if (_mergeMode == MergeMode.MOST_IMPORTANT)
                        Levels[i].Reverse();

                    foreach(var node in Levels[i])
                    {
                        leafCount -= node.MergeLeaves();
                        if (leafCount <= paletteSize)
                            break;
                    }
                    if (leafCount <= paletteSize)
                        break;
                    Levels[i].Clear();
                }
            }

            foreach(var node in Root.LeafNodes)
            {
                Palette.Add(node.Color);
            }
        }

        public void AddLevelNode(int level, OctreeNode node)
        {
            Levels[level].Add(node);
        }
        private void addColor(Color color)
        {
            Root.AddColor(color, 0, this);
        }

    }
}
