using System.Collections.Generic;

namespace Embroider.Quantizers
{
    public class VBox
    {
        private int _pixelCount;
        public float SortValue { get; set; }
        public int PixelCount
        {
            get => _pixelCount;
        }
        public int Xmin { get; set; }
        public int Xmax { get; set; }
        public int Ymin { get; set; }
        public int Ymax { get; set; }
        public int Zmin { get; set; }
        public int Zmax { get; set; }
        public int Volume
        {
            get => (Xmax - Xmin + 1) * (Ymax - Ymin + 1) * (Zmax - Zmin + 1);
        }
        public VBox(int xmin, int xmax, int ymin, int ymax, int zmin, int zmax)
        {
            Xmin = xmin;
            Xmax = xmax;
            Ymin = ymin;
            Ymax = ymax;
            Zmin = zmin;
            Zmax = zmax;
            SortValue = 0;
        }
        public int SetPixelCount(int[] histogram, int sigBits)
        {
            int pixelCount = 0;
            for (int x = Xmin; x<=Xmax; x++)
                for (int y = Ymin; y<=Ymax; y++)
                    for (int z = Zmin; z<=Zmax; z++)
                    {
                        int index = (x << (sigBits * 2)) + (y << sigBits) + z;
                        pixelCount += histogram[index];
                    }
            _pixelCount = pixelCount;
            return pixelCount;
        }

        public Color GetAverageColor(int[] histogram, int sigBits)
        {
            int xSum = 0, ySum = 0, zSum = 0;
            int mult = 1 << (8 - sigBits);
            int pixelCount = 0;

            for (int x = Xmin; x <= Xmax; x++)
                for (int y = Ymin; y <= Ymax; y++)
                    for (int z = Zmin; z <= Zmax; z++)
                    {
                        int index = (x << (sigBits * 2)) + (y << sigBits) + z;
                        pixelCount += histogram[index];
                        xSum += (int)(histogram[index] * (x + 0.5) * mult);
                        ySum += (int)(histogram[index] * (y + 0.5) * mult);
                        zSum += (int)(histogram[index] * (z + 0.5) * mult);
                    }
            if (pixelCount == 0)
            {
                var x = mult * (Xmin + Xmax + 1) / 2;
                var y = mult * (Ymin + Ymax + 1) / 2;
                var z = mult * (Zmin + Zmax + 1) / 2;
                return new Color(x, y, z);
            } else
            {
                return new Color(xSum / pixelCount, ySum / pixelCount, zSum / pixelCount);
            }
        }

        public VBox Copy()
        {
            var vbox = new VBox(Xmin, Xmax, Ymin, Ymax, Zmin, Zmax);
            vbox._pixelCount = PixelCount;
            return vbox;
        }
        
    }

    public class CompareVBox : IComparer<VBox>
    {
        public int Compare(VBox vBox1, VBox vBox2)
        {
            if (vBox1.SortValue > vBox2.SortValue)
                return 1;
            else if (vBox1.SortValue < vBox2.SortValue)
                return -1;
            else
                return 0;
        }
    }
}
