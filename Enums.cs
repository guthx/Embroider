using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider
{
    public static class Enums
    {
        public enum QuantizerType
        {
            SimplePopularity, Popularity, Octree, MedianCut, KMeans, ModifiedMedianCut
        }

        public enum OperationOrder
        {
            QuantizeFirst, ReplacePixelsFirst
        }

        public enum ColorSpace
        {
            Rgb, Hsv, Lab, Ycc, Luv
        }

        public enum MergeMode
        {
            LEAST_IMPORTANT, MOST_IMPORTANT
        }
        public enum DithererType
        {
            None,
            FloydSteinberg,
            Atkinson
        }
        public enum ColorComparerType
        {
            DE76, DE2000, CMC, EuclideanDistance, WeightedEuclideanDistance
        }
    }
}
