using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Quantizers
{
    public class OctreeNode
    {
        private Color _color;
        private int pixelCount;
        public int PaletteIndex;
        public OctreeNode[] Children;

        public OctreeNode(int level, OctreeQuantizer parent)
        {
            _color = new Color(0, 0, 0);
            pixelCount = 0;
            PaletteIndex = 0;
            Children = new OctreeNode[8];
            if (level < parent.MaxDepth - 1)
                parent.AddLevelNode(level, this);
        }

        public int PixelCount
        {
            get
            {
                int count = 0;
                if (IsLeaf)
                    count = pixelCount;
                else
                {
                    foreach (var node in Children)
                    {
                        if (node != null)
                            count += node.PixelCount;
                    }
                }
                return count;
            }
        }
        public Color Color
        {
            get
            {
                return _color.Normalized(pixelCount);
            }
        }
        public bool IsLeaf { 
            get
            {
                return pixelCount > 0;
            } 
        }

        public OctreeNode[] LeafNodes
        {
            get
            {
                var leafNodes = new List<OctreeNode>();
                foreach(var node in Children)
                {
                    if (node != null)
                    {
                        if (node.IsLeaf)
                            leafNodes.Add(node);
                        else
                            leafNodes.AddRange(node.LeafNodes);
                    }
                    
                }
                return leafNodes.ToArray();
            }
        }

        public void AddColor(Color color, int level, OctreeQuantizer parent)
        {
            if (level >= parent.MaxDepth)
            {
                _color.Add(color);
                pixelCount++;
            } else
            {
                var index = GetColorIndex(color, level);
                if (Children[index] == null)
                    Children[index] = new OctreeNode(level, parent);
                Children[index].AddColor(color, level + 1, parent);
            }

        }

        public int MergeLeaves()
        {
            int result = 0;
            foreach (var node in Children)
            {
                if (node != null)
                {
                    _color.Add(node._color);
                    pixelCount += node.pixelCount;
                    result++;
                }
            }
            Children = new OctreeNode[8];
            return result - 1;
        }

        public static int GetColorIndex(Color color, int level)
        {
            int index = 0;
            int mask = 0b10000000 >> level;
            if (((int)color.X & mask) != 0)
                index |= 0b100;
            if (((int)color.Y & mask) != 0)
                index |= 0b010;
            if (((int)color.Z & mask) != 0)
                index |= 0b001;
            return index;
        }
    }
}
