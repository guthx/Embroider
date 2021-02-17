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
        public string DMC_COLOR { get; set; }
        public string COLOR_NAME { get; set; }
        public string RGB_COLOR { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            
            var ahri = new Image<Lab, double>(@"F:\Inne\ahri\ahri_new.jpg");
            ahri = ImageProcessing.MeanReduce(ahri, 4);
            var quantizer = new OctreeQuantizer(ahri, 8, MergeMode.LEAST_IMPORTANT);
            quantizer.MakePalette(75);
            ImageProcessing.Stretch(quantizer.GetQuantizedImage(), 8).Convert<Bgr, byte>().Save(@"F:\Inne\ahri\ahri_oct.png");
            quantizer.ChangePaletteToDMC();
            ImageProcessing.Stretch(quantizer.GetQuantizedImage(), 8).Convert<Bgr, byte>().Save(@"F:\Inne\ahri\ahri_oct_dmc.png");
           // var predictor = ImageProcessing.BuildClusterModel(ImageProcessing.GetPixelValues(ahri), 64);
           // ImageProcessing.Stretch(ImageProcessing.ClusterizeImage(predictor, ahri, 64, true), 8).Convert<Bgr, byte>().Save(@"F:\Inne\ahri\ahri_kmeans_dmc.png");
            /*
            var flosses = new List<DMCRGB>();
            var flossesLab = new List<DmcFloss>();
            var convertHelper = new Image<Rgb, byte>(1, 1);
            using (var reader = new StreamReader(@"F:\Inne\ahri\result.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                flosses = csv.GetRecords<DMCRGB>().ToList();
                for (int i = 0; i < flosses.Count; i++)
                {
                    string rgbCode = flosses[i].RGB_COLOR.Trim();
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
                        L = convertLab[0, 0].X,
                        a = convertLab[0, 0].Y,
                        b = convertLab[0, 0].Z,
                        Number = flosses[i].DMC_COLOR,
                        Description = flosses[i].COLOR_NAME
                    });
                }
            }
            using (var writer = new StreamWriter(@"F:\Inne\ahri\dmc_lab.csv"))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords<DmcFloss>(flossesLab);
            }
            */
        }


    }
}

