using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using C5;
using Embroider.Ditherers;

namespace Embroider.Quantizers
{
    public class ModifiedMedianCutQuantizer : Quantizer
    {
        private int sigBits;
        private float fractionByPopulation;
        public ModifiedMedianCutQuantizer(Image<Lab, double> image, int _sigBits = 6, float _fractionByPopulation = 0.85f, DithererType dithererType = DithererType.None) : base(image, dithererType)
        {
            sigBits = _sigBits;
            fractionByPopulation = _fractionByPopulation;
        }
        protected override void MakePalette(int paletteSize)
        {
            int[] histogram = generateHistogram();
            //check if image has less colors than paletteSize
            int colorCount = 0;
            for (int i=0; i<histogram.Length; i++)
            {
                if (histogram[i] > 0)
                    colorCount++;
            }
            if (colorCount <= paletteSize)
            {
                for (int i = 0; i < histogram.Length; i++)
                    if (histogram[i] > 0)
                        Palette.Add(getColorFromIndex(i, sigBits));
                return;
            }

            var vbox = new VBox(
                0, (1 << sigBits) - 1,
                0, (1 << sigBits) - 1,
                0, (1 << sigBits) - 1);
            vbox.SetPixelCount(histogram, sigBits);
            var queue = new IntervalHeap<VBox>(paletteSize, new CompareVBox());
            queue.Add(vbox);

            colorCount = 0;
            int iterCount = 0;
            int colorsByPopulation = (int)(fractionByPopulation * paletteSize);
           // VBox vbox1 = null, vbox2 = null;
            // Generate colors by making median cuts based on population
            while (true)
            {
                vbox = queue.DeleteMax();
                if (vbox.SetPixelCount(histogram, sigBits) == 0)
                {
                    queue.Add(vbox);
                    continue;
                }
                VBox vbox1 = null, vbox2 = null;
                (vbox1, vbox2) = applyMedianCut(histogram, vbox);
                if (vbox1.Volume > 1)
                    vbox1.SortValue = vbox1.PixelCount;
                queue.Add(vbox1);

                if (vbox2 != null)
                {
                    if (vbox2.Volume > 1)
                        vbox2.SortValue = vbox2.PixelCount;
                    queue.Add(vbox2);
                    colorCount++;
                }

                if (colorCount >= colorsByPopulation)
                    break;
                if (iterCount++ >= 10000)
                    break;
            }

            var queue2 = new IntervalHeap<VBox>(paletteSize, new CompareVBox());
            // Re-sort queue elements by population * volume
            var vboxCount = queue.Count;
            for (int i=0; i<vboxCount; i++)
            {
                var box = queue.DeleteMin();
                box.SortValue = box.PixelCount * (box.Volume / 1000.0f);
                queue2.Add(box);
            }
            // Generate remaining colors by applying median-cuts based on population * volume of VBoxes
            while (true)
            {
                vbox = queue2.DeleteMax();
                if (vbox.SetPixelCount(histogram, sigBits) == 0)
                {
                    queue2.Add(vbox);
                    continue;
                }
                VBox vbox1 = null, vbox2 = null;
                (vbox1, vbox2) = applyMedianCut(histogram, vbox);
                if (vbox1.Volume > 1)
                    vbox1.SortValue = vbox1.PixelCount * (vbox1.Volume / 1000.0f);
                queue2.Add(vbox1);

                if (vbox2 != null)
                {
                    if (vbox2.Volume > 1)
                        vbox2.SortValue = vbox2.PixelCount * (vbox2.Volume / 1000.0f);
                    queue2.Add(vbox2);
                    colorCount++;
                }

                if (colorCount >= paletteSize)
                    break;
                if (iterCount++ > 10000)
                    break;
            }
            vboxCount = queue2.Count;
            for (int i=0; i<vboxCount; i++)
            {
                vbox = queue2.DeleteMax();
                Palette.Add(vbox.GetAverageColor(histogram, sigBits));
            }
        }

        private int[] generateHistogram()
        {
            int size = 1 << (3 * sigBits);
            var histogram = new int[size];
            for (int i=0; i<pixels.Count; i++)
            {
                var index = getColorIndex(pixels[i], sigBits);
                histogram[index]++;
            }
            return histogram;
        }

        private (VBox, VBox) applyMedianCut(int[] histogram, VBox vbox)
        {
            int xWidth = vbox.Xmax - vbox.Xmin + 1;
            int yWidth = vbox.Ymax - vbox.Ymin + 1;
            int zWidth = vbox.Zmax - vbox.Zmin + 1;
            if (xWidth == 1 && yWidth == 1 && zWidth == 1)
            {
                return (vbox.Copy(), null);
            }

            int maxWidth = Math.Max(xWidth, yWidth);
            maxWidth = Math.Max(maxWidth, zWidth);
            int total = 0;
            int[] partialSum = new int[128];
            if (maxWidth == xWidth)
            {
                for (int x=vbox.Xmin; x <= vbox.Xmax; x++)
                {
                    int sum = 0;
                    for (int y = vbox.Ymin; y <= vbox.Ymax; y++)
                        for (int z = vbox.Zmin; z <= vbox.Zmax; z++)
                        {
                            int index = (x << (2 * sigBits)) + (y << sigBits) + z;
                            sum += histogram[index];
                        }
                    total += sum;
                    partialSum[x] = total;
                }
            } else if (maxWidth == yWidth)
            {
                for (int y = vbox.Ymin; y <= vbox.Ymax; y++)
                {
                    int sum = 0;
                    for (int x = vbox.Xmin; x <= vbox.Xmax; x++)
                        for (int z = vbox.Zmin; z <= vbox.Zmax; z++)
                        {
                            int index = (x << (2 * sigBits)) + (y << sigBits) + z;
                            sum += histogram[index];
                        }
                    total += sum;
                    partialSum[y] = total;
                }
            } else
            {
                for (int z = vbox.Zmin; z <= vbox.Zmax; z++)
                {
                    int sum = 0;
                    for (int x = vbox.Xmin; x <= vbox.Xmax; x++)
                        for (int y = vbox.Ymin; y <= vbox.Ymax; y++)
                        {
                            int index = (x << (2 * sigBits)) + (y << sigBits) + z;
                            sum += histogram[index];
                        }
                    total += sum;
                    partialSum[z] = total;
                }
            }
            VBox vbox1 = null, vbox2 = null;
            if (maxWidth == xWidth)
            {
                for (int i = vbox.Xmin; i <= vbox.Xmax; i++)
                {
                    if (partialSum[i] > total / 2)
                    {
                        vbox1 = vbox.Copy();
                        vbox2 = vbox.Copy();
                        int left = i - vbox.Xmin;
                        int right = vbox.Xmax - i;
                        if (left <= right)
                        {
                            vbox1.Xmax = Math.Min(vbox.Xmax - 1, i + right / 2);
                        } else
                        {
                            vbox1.Xmax = Math.Max(vbox.Xmin, i - 1 - left / 2);
                        }
                        vbox2.Xmin = vbox1.Xmax + 1;
                        break;
                    }
                }
            } else if (maxWidth == yWidth)
            {
                for (int i = vbox.Ymin; i <= vbox.Ymax; i++)
                {
                    if (partialSum[i] > total / 2)
                    {
                        vbox1 = vbox.Copy();
                        vbox2 = vbox.Copy();
                        int left = i - vbox.Ymin;
                        int right = vbox.Ymax - i;
                        if (left <= right)
                        {
                            vbox1.Ymax = Math.Min(vbox.Ymax - 1, i + right / 2);
                        }
                        else
                        {
                            vbox1.Ymax = Math.Max(vbox.Ymin, i - 1 - left / 2);
                        }
                        vbox2.Ymin = vbox1.Ymax + 1;
                        break;
                    }
                }
            } else
            {
                for (int i = vbox.Zmin; i <= vbox.Zmax; i++)
                {
                    if (partialSum[i] > total / 2)
                    {
                        vbox1 = vbox.Copy();
                        vbox2 = vbox.Copy();
                        int left = i - vbox.Zmin;
                        int right = vbox.Zmax - i;
                        if (left <= right)
                        {
                            vbox1.Zmax = Math.Min(vbox.Zmax - 1, i + right / 2);
                        }
                        else
                        {
                            vbox1.Zmax = Math.Max(vbox.Zmin, i - 1 - left / 2);
                        }
                        vbox2.Zmin = vbox1.Zmax + 1;
                        break;
                    }
                }
            }

            vbox1.SetPixelCount(histogram, sigBits);
            vbox2.SetPixelCount(histogram, sigBits);

            return (vbox1, vbox2);
        }

        private static int getColorIndex(Color color, int sigBits)
        {
            int x = color.X >> (8 - sigBits);
            int y = color.Y >> (8 - sigBits);
            int z = color.Z >> (8 - sigBits);
            return (x << (sigBits*2)) + (y << sigBits) + z;
        }

        private static Color getColorFromIndex(int index, int sigBits)
        {
            int x = (index >> (sigBits * 2)) << (8 - sigBits);
            int y = (index >> (sigBits)) << (8 - sigBits);
            int z = (index) << (8 - sigBits);
            return new Color(x, y, z);
        }

    }
}
