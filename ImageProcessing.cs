using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.IO;
using CsvHelper;
using System.Globalization;
using System.Linq;

namespace Embroider
{
    public static class ImageProcessing
    {
        public static List<DmcFloss> DmcFlosses;
        public static void IncreaseContrast(Image<Rgb, double> image, float contrast)
        {
            double sigmoid(double x)
            {
                return 1*(-128 + 256 * (1 / (1 + Math.Exp(-x / 40))));
            }
            for (int w = 0; w < image.Width; w++)
                for (int h = 0; h < image.Height; h++)
                {
                    var newY = sigmoid(image.Data[h, w, 1] - 128) + 128;
                    var newZ = sigmoid(image.Data[h, w, 2] - 128) + 128;

                    // var newY = (image.Data[h, w, 1] - 128) * contrast + image.Data[h, w, 1];
                    // var newZ = (image.Data[h, w, 2] - 128) * contrast + image.Data[h, w, 2];

                    image[h, w] = new Rgb(image.Data[h, w, 0], newY, newZ);
                }
                    
        }
        public static PixelValue[] GetPixelValues(Image<Rgb, double> image)
        {
            var pixelValues = new PixelValue[image.Width * image.Height];
            int i = 0;
            for (int w = 0; w < image.Width; w++)
            {
                for (int h = 0; h < image.Height; h++)
                {
                    pixelValues[i] = new PixelValue(image.Data[h, w, 0]/2.55, (image.Data[h, w, 1]-128), (image.Data[h, w, 2]-128));
                    i++;
                }
            }

            return pixelValues;
            
        }

        public static Image<Rgb, double> MeanReduce(Image<Rgb, double> image, int pixelSize)
        {
            var newImage = new Image<Rgb, double>((image.Width - 1) / pixelSize + 1, (image.Height - 1) / pixelSize + 1);
            var pixelValues = new double[(image.Height - 1) / pixelSize + 1, (image.Width - 1) / pixelSize + 1, 3];
            var pixelCount = new int[(image.Height - 1) / pixelSize + 1, (image.Width - 1) / pixelSize + 1];
            for (int h=0; h<image.Height; h++)
            {
                for(int w=0; w<image.Width; w++)
                {
                    pixelValues[h / pixelSize, w / pixelSize, 0] += image.Data[h, w, 0];
                    pixelValues[h / pixelSize, w / pixelSize, 1] += image.Data[h, w, 1];
                    pixelValues[h / pixelSize, w / pixelSize, 2] += image.Data[h, w, 2];
                    pixelCount[h / pixelSize, w / pixelSize]++;
                }
            }
            for (int h=0; h<newImage.Height; h++)
            {
                for(int w=0; w<newImage.Width; w++)
                {
                    var x = pixelValues[h, w, 0] / pixelCount[h, w];
                    var y = pixelValues[h, w, 1] / pixelCount[h, w];
                    var z = pixelValues[h, w, 2] / pixelCount[h, w];
                    newImage[h, w] = new Rgb(x, y, z);
                }
            }
            return newImage;
        }

        public static Image<Rgb, double> Stretch(Image<Rgb, double> image, int sizeMultiplier, bool net = false)
        {
            if (!net)
            {
                var newImage = new Image<Rgb, double>(image.Width * sizeMultiplier, image.Height * sizeMultiplier);
                for (int h=0; h<newImage.Height; h++)
                {
                    for(int w=0; w<newImage.Width; w++)
                    {
                        newImage[h, w] = image[h / sizeMultiplier, w / sizeMultiplier];
                    }
                }
                return newImage;
            }
            else
            {
                var newImage = new Image<Rgb, double>(image.Width * sizeMultiplier + image.Width - 1, image.Height * sizeMultiplier + image.Height - 1);
                for (int h=0; h<newImage.Height; h++)
                {
                    for (int w=0; w<newImage.Width; w++)
                    {
                        if ( (h+1) % (sizeMultiplier+1) == 0 || (w+1) % (sizeMultiplier+1) == 0)
                        {
                            newImage[h, w] = new Rgb(120, 120, 120);
                        }
                        else
                            newImage[h, w] = image[(h - (h+1) / (sizeMultiplier+1)) / sizeMultiplier, (w - (w+1) / (sizeMultiplier+1)) / sizeMultiplier];
                    }
                }
                return newImage;
            }
            
        }

        public static PredictionEngine<PixelValue, PixelPrediciton> BuildClusterModel(PixelValue[] pixels, int numOfClusters)
        {
            var ctx = new MLContext();

            IDataView data = ctx.Data.LoadFromEnumerable<PixelValue>(pixels);
            var dataProcessPipeline = ctx.Transforms.Concatenate("Features", nameof(PixelValue.v0), nameof(PixelValue.v1), nameof(PixelValue.v2));
           // var trainer = ctx.Clustering.Trainers.KMeans("Features", null, numOfClusters);
            
            var trainer = ctx.Clustering.Trainers.KMeans(new Microsoft.ML.Trainers.KMeansTrainer.Options
            {
                FeatureColumnName = "Features",
                InitializationAlgorithm = Microsoft.ML.Trainers.KMeansTrainer.InitializationAlgorithm.KMeansPlusPlus,
                MaximumNumberOfIterations = 100000,
                OptimizationTolerance = 0.00001f,
                NumberOfClusters = numOfClusters
            });
            
            var trainingPipeline = dataProcessPipeline.Append(trainer);

            var trainedModel = trainingPipeline.Fit(data);
            var predictor = ctx.Model.CreatePredictionEngine<PixelValue, PixelPrediciton>(trainedModel);

            return predictor;
        }

        public static Image<Rgb, double> ClusterizeImage(PredictionEngine<PixelValue, PixelPrediciton> predictor, Image<Rgb, double> image, int numOfClusters, bool useDmcColors = true)
        {
            var imageBgr = image.Convert<Bgr, byte>();
            var convertHelper = new Image<Bgr, byte>(1, 1);
            var pixelClusters = new uint[image.Height, image.Width];
            var pixelCount = new int[numOfClusters];
            var clusterMean = new double[numOfClusters, 3];
            for (int w = 0; w < image.Width; w++)
            {
                for (int h=0; h<image.Height; h++)
                {
                    var pixelVal = new PixelValue(image.Data[h, w, 0]/2.55, (image.Data[h, w, 1]-128), (image.Data[h, w, 2]-128));
                    var cluster = predictor.Predict(pixelVal);
                    pixelClusters[h, w] = cluster.Cluster - 1;
                    pixelCount[cluster.Cluster - 1]++;
                    
                    clusterMean[cluster.Cluster - 1, 0] += image.Data[h, w, 0];
                    clusterMean[cluster.Cluster - 1, 1] += image.Data[h, w, 1];
                    clusterMean[cluster.Cluster - 1, 2] += image.Data[h, w, 2];
                    /*
                    clusterMean[cluster.Cluster - 1, 0] += imageBgr[h, w].Blue * imageBgr[h, w].Blue;
                    clusterMean[cluster.Cluster - 1, 1] += imageBgr[h, w].Green * imageBgr[h, w].Green;
                    clusterMean[cluster.Cluster - 1, 2] += imageBgr[h, w].Red * imageBgr[h, w].Red;
                    */
                }
                
            }
            for (int i = 0; i < numOfClusters; i++)
            {
                
                clusterMean[i, 0] /= pixelCount[i];
                clusterMean[i, 1] /= pixelCount[i];
                clusterMean[i, 2] /= pixelCount[i];
                /*
                clusterMean[i, 0] = Math.Sqrt(clusterMean[i, 0] / pixelCount[i]);
                clusterMean[i, 1] = Math.Sqrt(clusterMean[i, 1] / pixelCount[i]);
                clusterMean[i, 2] = Math.Sqrt(clusterMean[i, 2] / pixelCount[i]);
                convertHelper[0, 0] = new Bgr(clusterMean[i, 0], clusterMean[i, 1], clusterMean[i, 2]);
                var convertLab = convertHelper.Convert<Lab, double>();
                clusterMean[i, 0] = convertLab[0, 0].X;
                clusterMean[i, 1] = convertLab[0, 0].Y;
                clusterMean[i, 2] = convertLab[0, 0].Z;
                */
            }

            if (useDmcColors)
            {
                if (DmcFlosses == null)
                {
                    DmcFlosses = new List<DmcFloss>();
                    using (var reader = new StreamReader(@"F:\Inne\ahri\dmc_lab.csv"))
                        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        DmcFlosses = csv.GetRecords<DmcFloss>().ToList();
                    }
                }
                
                for (int i=0; i<numOfClusters; i++)
                {
                    var deltaE = new double[DmcFlosses.Count];
                    for (int j=0; j<DmcFlosses.Count; j++)
                    {
                        var color1 = new Lab2(DmcFlosses[j].L / 2.55, (DmcFlosses[j].a - 128), (DmcFlosses[j].b - 128));
                        var color2 = new Lab2(clusterMean[i,0]/2.55, (clusterMean[i,1]-128), (clusterMean[i, 2]-128));
                        deltaE[j] = Lab2.CompareCMC(color2, color1);
                        /*
                        deltaE[j] = Math.Sqrt(
                            Math.Pow((DmcFlosses[j].L/2.55) - (clusterMean[i, 0]/2.55), 2) + 
                            Math.Pow(DmcFlosses[j].a - clusterMean[i, 1], 2) + 
                            Math.Pow(DmcFlosses[j].b - clusterMean[i, 2], 2));
                        */
                    }
                    var dmc = DmcFlosses[Array.IndexOf(deltaE, deltaE.Min())];
                    clusterMean[i, 0] = dmc.L;
                    clusterMean[i, 1] = dmc.a;
                    clusterMean[i, 2] = dmc.b;
                }
            }

            var newImage = new Image<Rgb, double>(image.Width, image.Height);
            
            for (int w=0; w<image.Width; w++)
            {
                for (int h=0; h<image.Height; h++)
                {
                    double x = clusterMean[pixelClusters[h, w], 0];
                    double y = clusterMean[pixelClusters[h, w], 1];
                    double z = clusterMean[pixelClusters[h, w], 2];
                    newImage[h, w] = new Rgb(x, y, z);
                }
            }
            return newImage;
        }

        public static Image<Rgb, double> ClusterizeImage2(PredictionEngine<PixelValue, PixelPrediciton> predictor, Image<Rgb, double> image, int numOfClusters)
        {
            if (DmcFlosses == null)
            {
                DmcFlosses = new List<DmcFloss>();
                using (var reader = new StreamReader(@"Resources\dmc_cotton_lab.csv"))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    DmcFlosses = csv.GetRecords<DmcFloss>().ToList();
                }
            }

            var pixelClusters = new uint[image.Height, image.Width];
            var clusterDmcCount = new int[numOfClusters][];
            for (int i = 0; i < numOfClusters; i++)
                clusterDmcCount[i] = new int[DmcFlosses.Count];
            var clusterColors = new DmcFloss[numOfClusters];
            for (int w = 0; w < image.Width; w++)
            {
                for (int h = 0; h < image.Height; h++)
                {
                    var pixelVal = new PixelValue(image.Data[h, w, 0] / 2.55, image.Data[h, w, 1] - 128, image.Data[h, w, 2] - 128);
                    var cluster = predictor.Predict(pixelVal);
                    pixelClusters[h, w] = cluster.Cluster - 1;
                    var deltaE = new double[DmcFlosses.Count];
                    for (int i = 0; i < DmcFlosses.Count; i++)
                    {
                        deltaE[i] = Math.Sqrt(
                            Math.Pow((DmcFlosses[i].L / 2.55) - (image.Data[h, w, 0] / 2.55), 2) +
                            Math.Pow(DmcFlosses[i].a - image.Data[h, w, 1], 2) +
                            Math.Pow(DmcFlosses[i].b - image.Data[h, w, 2], 2));
                    }
                    var index = Array.IndexOf(deltaE, deltaE.Min());
                    clusterDmcCount[cluster.Cluster - 1][index]++;


                }

            }
            for (int i = 0; i < numOfClusters; i++)
            {
                var index = Array.IndexOf(clusterDmcCount[i], clusterDmcCount[i].Max());
                clusterColors[i] = DmcFlosses[index];
            }

            var newImage = new Image<Rgb, double>(image.Width, image.Height);

            for (int w = 0; w < image.Width; w++)
            {
                for (int h = 0; h < image.Height; h++)
                {
                    double x = clusterColors[pixelClusters[h, w]].L;
                    double y = clusterColors[pixelClusters[h, w]].a;
                    double z = clusterColors[pixelClusters[h, w]].b;
                    newImage[h, w] = new Rgb(x, y, z);
                }
            }
            return newImage;
        }
        public static void ReplacePixelsWithDMC(Image<Rgb, double> image, bool useDithering = false)
        {
            var colorsCount = new Dictionary<DmcFloss, int>();
            DmcFlosses = Flosses.Dmc;

            for(int h=0; h<image.Height; h++)
            {
                for(int w=0; w<image.Width; w++)
                {
                    var deltaE = new double[DmcFlosses.Count];
                    for (int i = 0; i < DmcFlosses.Count; i++)
                    {
                        var color1 = new Lab2(DmcFlosses[i].Red, DmcFlosses[i].Green, DmcFlosses[i].Blue);
                        var color2 = new Lab2(image.Data[h, w, 0], image.Data[h, w, 1], image.Data[h, w, 2]);
                        /*
                        deltaE[i] = Math.Sqrt(
                            Math.Pow((DmcFlosses[i].L) - (image.Data[h, w, 0]), 2) +
                            Math.Pow(DmcFlosses[i].a - image.Data[h, w, 1], 2) +
                            Math.Pow(DmcFlosses[i].b - image.Data[h, w, 2], 2));
                        */
                        deltaE[i] = Lab2.CompareCMC(color1, color2);
                        //deltaE[i] = Lab2.Compare(color1, color2);
                    }
                    var dmc = DmcFlosses[Array.IndexOf(deltaE, deltaE.Min())];
                    int count;
                    if (colorsCount.ContainsKey(dmc))
                    {
                        colorsCount[dmc]++;
                    }
                    else
                    {
                        colorsCount.Add(dmc, 1);
                    }
                    if (useDithering)
                    {
                        var errorR = image.Data[h, w, 0] - dmc.Red;
                        var errorG = image.Data[h, w, 1] - dmc.Green;
                        var errorB = image.Data[h, w, 2] - dmc.Blue;
                        /*
                        if (h < image.Height - 1)
                        {
                            if (w > 0)
                            image[h + 1, w - 1] = new Rgb(
                                image.Data[h + 1,  w - 1, 0] + errorL * 3 / 16,
                                image.Data[h + 1,  w - 1, 1] + errorA * 3 / 16,
                                image.Data[h + 1,  w - 1, 2] + errorB * 3 / 16);
                            image[h + 1, w] = new Rgb(
                                image.Data[h + 1,  w, 0] + errorL * 5 / 16,
                                image.Data[h + 1,  w, 1] + errorA * 5 / 16,
                                image.Data[h + 1,  w, 2] + errorB * 5 / 16);
                            if (w < image.Width - 1)
                            {
                                image[h + 1, w + 1] = new Rgb(
                                image.Data[h + 1,  w + 1, 0] + errorL * 1 / 16,
                                image.Data[h + 1,  w + 1, 1] + errorA * 1 / 16,
                                image.Data[h + 1,  w + 1, 2] + errorB * 1 / 16);
                            }
                        }
                        if (w < image.Width - 1)
                        {
                            image[h, w + 1] = new Rgb(
                            image.Data[h,  w + 1, 0] + errorL * 7 / 16,
                            image.Data[h,  w + 1, 1] + errorA * 7 / 16,
                            image.Data[h,  w + 1, 2] + errorB * 7 / 16);
                        }
                    }
                        */
                        if (w < image.Width - 1)
                        {
                            image[h, w + 1] = new Rgb(
                               image.Data[h,  w + 1, 0] + errorR * 1 / 8,
                               image.Data[h,  w + 1, 1] + errorG * 1 / 8,
                               image.Data[h,  w + 1, 2] + errorB * 1 / 8);
                            if (h < image.Height - 1)
                            {
                                image[h + 1, w + 1] = new Rgb(
                                image.Data[h + 1,  w + 1, 0] + errorR * 1 / 8,
                                image.Data[h + 1,  w + 1, 1] + errorG * 1 / 8,
                                image.Data[h + 1,  w + 1, 2] + errorB * 1 / 8);
                            }
                        }
                        if (w < image.Width - 2)
                        {
                            image[h, w + 2] = new Rgb(
                                image.Data[h,  w + 2, 0] + errorR * 1 / 8,
                                image.Data[h,  w + 2, 1] + errorG * 1 / 8,
                                image.Data[h,  w + 2, 2] + errorB * 1 / 8
                                );
                        }
                        if (h < image.Height - 1)
                        {
                            if (w > 0)
                                image[h + 1, w - 1] = new Rgb(
                                    image.Data[h + 1,  w - 1, 0] + errorR * 1 / 8,
                                    image.Data[h + 1,  w - 1, 1] + errorG * 1 / 8,
                                    image.Data[h + 1,  w - 1, 2] + errorB * 1 / 8
                                    );
                            image[h + 1, w] = new Rgb(
                                image.Data[h + 1,  w, 0] + errorR * 1 / 8,
                                image.Data[h + 1,  w, 1] + errorG * 1 / 8,
                                image.Data[h + 1,  w, 2] + errorB * 1 / 8
                                );
                        }
                        if (h < image.Height - 2)
                        {
                            image[h + 2, w] = new Rgb(
                                image.Data[h + 2,  w, 0] + errorR * 1 / 8,
                                image.Data[h + 2,  w, 1] + errorG * 1 / 8,
                                image.Data[h + 2,  w, 2] + errorB * 1 / 8
                                );
                        }
                    }
                    image[h, w] = new Rgb(dmc.Red, dmc.Green, dmc.Blue);
                }
            }
        }
    }

    

    public class PixelValue
    {
        public float v0;
        public float v1;
        public float v2;

        public PixelValue(double _v0, double _v1, double _v2)
        {
            v0 = (float)_v0;
            v1 = (float)_v1;
            v2 = (float)_v2;
        }
    }

    public class PixelPrediciton
    {
        [ColumnName("PredictedLabel")]
        public uint Cluster { get; set; }
        [ColumnName("Score")]
        public float[] Distances { get; set; }
    }

    public class Lab2
    {
        public double L;
        public double a;
        public double b;

        public Lab2(Lab lab)
        {
            L = lab.X / 2.55;
            a = (lab.Y - 128);
            b = (lab.Z - 128);
        }

        public Lab2(double R, double G, double B)
        {
            R = R / 255;
            G = G / 255;
            B = B / 255;

            R = (R > 0.04045) ? Math.Pow((R + 0.055) / 1.055, 2.4) : R / 12.92;
            G = (G > 0.04045) ? Math.Pow((G + 0.055) / 1.055, 2.4) : G / 12.92;
            B = (B > 0.04045) ? Math.Pow((B + 0.055) / 1.055, 2.4) : B / 12.92;

            var x = (R * 0.4124 + G * 0.3576 + B * 0.1805) / 0.95047;
            var y = (R * 0.2126 + G * 0.7152 + B * 0.0722) / 1.00000;
            var z = (R * 0.0193 + G * 0.1192 + B * 0.9505) / 1.08883;
            // 7.787
            // 903.3
            x = (x > 0.008856) ? Math.Pow(x, 1.0 / 3) : (7.787 * x) + 16.0 / 116;
            y = (y > 0.008856) ? Math.Pow(y, 1.0 / 3) : (7.787 * y) + 16.0 / 116;
            z = (z > 0.008856) ? Math.Pow(z, 1.0 / 3) : (7.787 * z) + 16.0 / 116;

            L = (116 * y) - 16;
            a = 500 * (x - y);
            b = 200 * (y - z);
        }

        public static double CompareDE74(Lab2 color1, Lab2 color2)
        {
            return Math.Sqrt(
                Math.Pow(color1.L - color2.L, 2) +
                Math.Pow(color1.a - color2.a, 2) +
                Math.Pow(color1.b - color2.b, 2));
        }
        public static double CompareDE2000(Lab2 color1, Lab2 color2)
        {
            double kL = 1.0;
            double kC = 1.0;
            double kH = 1.0;
            double lBarPrime = 0.5 * (color1.L + color2.L);
            double c1 = Math.Sqrt(color1.a * color1.a + color1.b * color1.b);
            double c2 = Math.Sqrt(color2.a * color2.a + color2.b * color2.b);
            double cBar = 0.5 * (c1 + c2);
            double cBar7 = cBar * cBar * cBar * cBar * cBar * cBar * cBar;
            double g = 0.5 * (1.0 - Math.Sqrt(cBar7 / (cBar7 + 6103515625.0)));  /* 6103515625 = 25^7 */
            double a1Prime = color1.a * (1.0 + g);
            double a2Prime = color2.a * (1.0 + g);
            double c1Prime = Math.Sqrt(a1Prime * a1Prime + color1.b * color1.b);
            double c2Prime = Math.Sqrt(a2Prime * a2Prime + color2.b * color2.b);
            double cBarPrime = 0.5 * (c1Prime + c2Prime);
            double h1Prime = (Math.Atan2(color1.b, a1Prime) * 180.0) / Math.PI;
            double dhPrime; // not initialized on purpose
            if (h1Prime < 0.0)
                h1Prime += 360.0;
            double h2Prime = (Math.Atan2(color2.b, a2Prime) * 180.0) / Math.PI;
            if (h2Prime < 0.0)
                h2Prime += 360.0;
            double hBarPrime = (Math.Abs(h1Prime - h2Prime) > 180.0) ? (0.5 * (h1Prime + h2Prime + 360.0)) : (0.5 * (h1Prime + h2Prime));
            double t = 1.0 -
            0.17 * Math.Cos(Math.PI * (hBarPrime - 30.0) / 180.0) +
            0.24 * Math.Cos(Math.PI * (2.0 * hBarPrime) / 180.0) +
            0.32 * Math.Cos(Math.PI * (3.0 * hBarPrime + 6.0) / 180.0) -
            0.20 * Math.Cos(Math.PI * (4.0 * hBarPrime - 63.0) / 180.0);
            if (Math.Abs(h2Prime - h1Prime) <= 180.0)
                dhPrime = h2Prime - h1Prime;
            else
                dhPrime = (h2Prime <= h1Prime) ? (h2Prime - h1Prime + 360.0) : (h2Prime - h1Prime - 360.0);
            double dLPrime = color2.L - color1.L;
            double dCPrime = c2Prime - c1Prime;
            double dHPrime = 2.0 * Math.Sqrt(c1Prime * c2Prime) * Math.Sin(Math.PI * (0.5 * dhPrime) / 180.0);
            double sL = 1.0 + ((0.015 * (lBarPrime - 50.0) * (lBarPrime - 50.0)) / Math.Sqrt(20.0 + (lBarPrime - 50.0) * (lBarPrime - 50.0)));
            double sC = 1.0 + 0.045 * cBarPrime;
            double sH = 1.0 + 0.015 * cBarPrime * t;
            double dTheta = 30.0 * Math.Exp(-((hBarPrime - 275.0) / 25.0) * ((hBarPrime - 275.0) / 25.0));
            double cBarPrime7 = cBarPrime * cBarPrime * cBarPrime * cBarPrime * cBarPrime * cBarPrime * cBarPrime;
            double rC = Math.Sqrt(cBarPrime7 / (cBarPrime7 + 6103515625.0));
            double rT = -2.0 * rC * Math.Sin(Math.PI * (2.0 * dTheta) / 180.0);
            return (Math.Sqrt(
                               (dLPrime / (kL * sL)) * (dLPrime / (kL * sL)) +
                               (dCPrime / (kC * sC)) * (dCPrime / (kC * sC)) +
                               (dHPrime / (kH * sH)) * (dHPrime / (kH * sH)) +
                               (dCPrime / (kC * sC)) * (dHPrime / (kH * sH)) * rT
                          )
             );
        }

        public static double CompareCMC(Lab2 color1, Lab2 color2, double l = 1, double c = 1)
        {
            double c1 = Math.Sqrt(color1.a * color1.a + color1.b * color1.b);
            double c2 = Math.Sqrt(color2.a * color2.a + color2.b * color2.b);
            double deltaC = c1 - c2;
            double deltaH = Math.Sqrt(Math.Pow(color1.a - color2.a, 2) + Math.Pow(color1.b - color2.b, 2) - Math.Pow(deltaC, 2));
            if (deltaH.Equals(double.NaN))
                deltaH = 0;
            double deltaL = color1.L - color2.L;
            double SL = 0.511;
            if (color1.L >= 16)
            {
                SL = (0.040975 * color1.L) / (1 + 0.01765 * color1.L);
            }
            double SC = (0.0638 * c1) / (1 + 0.0131 * c1) + 0.638;
            double H = Math.Atan2(color1.b, color1.a) * (180.0/Math.PI);
            double H1 = H;
            if (H < 0)
                H1 = H + 360;
            double F = Math.Sqrt(Math.Pow(c1, 4) / (Math.Pow(c1, 4) + 1900));
            double T;
            if (H1 >= 164 && H1 <= 345)
            {
                T = 0.56 + Math.Abs(0.2 * Math.Cos((H1+168) * (Math.PI / 180.0)));
            }
            else
            {
                T = 0.36 + Math.Abs(0.4 * Math.Cos((H1+35) * (Math.PI / 180.0)));
            }
            double SH = SC * (F * T + 1 - F);
            return Math.Sqrt(
                Math.Pow(deltaL/(l*SL), 2) +
                Math.Pow(deltaC/(c*SC), 2) +
                Math.Pow(deltaH/SH, 2)
                );
        }
    }
}