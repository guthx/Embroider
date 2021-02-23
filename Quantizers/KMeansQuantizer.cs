using Embroider.Ditherers;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Quantizers
{
    public class KMeansQuantizer : Quantizer
    {
        public KMeansQuantizer(Image<Lab, double> image, DithererType dithererType) : base(image, dithererType) { }

        protected override void MakePalette(int paletteSize)
        {
            var pixelValues = new PixelValue[pixels.Count];
            for (int i = 0; i < pixels.Count; i++)
                pixelValues[i] = new PixelValue(pixels[i].X / 2.55, pixels[i].Y - 128, pixels[i].Z - 128);
            var predictor = BuildClusterModel(pixelValues, paletteSize);
            var pixelClusters = new uint[pixels.Count];
            var pixelCount = new int[paletteSize];
            var clusterMean = new double[paletteSize, 3];
            for (int i=0; i<pixels.Count; i++)
            {
                var pixelVal = new PixelValue(pixels[i].X / 2.55, pixels[i].Y - 128, pixels[i].Z - 128);
                var cluster = predictor.Predict(pixelVal);
                pixelClusters[i] = cluster.Cluster - 1;
                pixelCount[cluster.Cluster - 1]++;

                clusterMean[cluster.Cluster - 1, 0] += pixels[i].X;
                clusterMean[cluster.Cluster - 1, 1] += pixels[i].Y;
                clusterMean[cluster.Cluster - 1, 2] += pixels[i].Z;
            }
            for (int i = 0; i < paletteSize; i++)
            {
                clusterMean[i, 0] /= pixelCount[i];
                clusterMean[i, 1] /= pixelCount[i];
                clusterMean[i, 2] /= pixelCount[i];
                Palette.Add(new Color((int)clusterMean[i, 0], (int)clusterMean[i, 1], (int)clusterMean[i, 2]));
            }
        }

        private PredictionEngine<PixelValue, PixelPrediciton> BuildClusterModel(PixelValue[] pixels, int numOfClusters)
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


    }

    internal class PixelValue
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

    internal class PixelPrediciton
    {
        [ColumnName("PredictedLabel")]
        public uint Cluster { get; set; }
        [ColumnName("Score")]
        public float[] Distances { get; set; }
    }
}
