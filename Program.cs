using CsvHelper;
using CsvHelper.Configuration;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Embroider.Quantizers;

namespace Embroider
{
    class DMCRGB
    {
        public string rgb { get; set; }
        public string number { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            
            var ahri = new Image<Lab, double>(@"F:\Inne\ahri\ahri_new.jpg");
            //ImageProcessing.MeanReduce(ahri, 4).Convert<Bgr, byte>().Save(@"F:\Inne\ahri\reduced.png");

            
            var embroider = new Embroider(ahri, new EmbroiderOptions
            {
                OperationOrder = OperationOrder.QuantizeFirst,
                StichSize = 4,
                MaxColors = 64,
                QuantizerType = QuantizerType.ModifiedMedianCut,
                OutputStitchSize = 4,
                DithererType = Ditherers.DithererType.Atkinson
            });
            embroider.GenerateImage().Convert<Bgr, byte>().Save(@"F:\Inne\ahri\embroider.png");
            
            //ImageProcessing.Stretch(ahri, 4).Convert<Bgr, byte>().Save(@"F:\Inne\ahri\test.png");
            // var predictor = ImageProcessing.BuildClusterModel(ImageProcessing.GetPixelValues(ahri), 64);
            // ImageProcessing.Stretch(ImageProcessing.ClusterizeImage(predictor, ahri, 64, true), 8).Convert<Bgr, byte>().Save(@"F:\Inne\ahri\ahri_kmeans_dmc.png");
            
            /*
            var flosses = new List<DMCRGB>();
            var flossesLab = new List<DmcFloss>();
            var convertHelper = new Image<Rgb, byte>(1, 1);
            using (var reader = new StreamReader(@"F:\Inne\ahri\palette.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                flosses = csv.GetRecords<DMCRGB>().ToList();
                for (int i = 0; i < flosses.Count; i++)
                {
                    string rgbCode = flosses[i].rgb.Trim();
                    string redCode = rgbCode.Substring(1, 2);
                    string greenCode = rgbCode.Substring(3, 2);
                    string blueCode = rgbCode.Substring(5, 2);
                    int red = int.Parse(redCode, System.Globalization.NumberStyles.HexNumber);
                    int green = int.Parse(greenCode, System.Globalization.NumberStyles.HexNumber);
                    int blue = int.Parse(blueCode, System.Globalization.NumberStyles.HexNumber);
                    convertHelper[0, 0] = new Rgb(red, green, blue);
                    var convertLab = convertHelper.Convert<Lab, double>();
                    flossesLab.Add(new DmcFloss
                    {
                        Red = red,
                        Green = green,
                        Blue = blue,
                        L = convertLab[0, 0].X,
                        a = convertLab[0, 0].Y,
                        b = convertLab[0, 0].Z,
                        Number = flosses[i].number,
                        Description = ""
                    });
                }
            }
            using (var writer = new StreamWriter(@"F:\Inne\ahri\dmc_lab2.csv"))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords<DmcFloss>(flossesLab);
            }
            */
        }


    }
}

