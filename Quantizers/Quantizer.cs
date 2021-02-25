using Embroider.Comparers;
using Embroider.Ditherers;
using Emgu.CV;
using Emgu.CV.Structure;
using OfficeOpenXml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Embroider.Enums;

namespace Embroider.Quantizers
{
    public abstract class Quantizer
    {
        public List<Color> Palette;
        public List<DmcFloss> DmcPalette;
        protected List<Color> pixels;
        protected Image<Rgb, double> _image;
        public ConcurrentDictionary<DmcFloss, int> DmcFlossCount;
        public DmcFloss[,] DmcFlossMap;
        public Ditherer ditherer;
        public ColorComparer colorComparer;
        
        public Quantizer(Image<Rgb, double> image, 
            DithererType dithererType = DithererType.None, 
            ColorComparerType colorComparer = ColorComparerType.DE76, 
            int dithererStrength = 255)
        {
            DmcFlossCount = new ConcurrentDictionary<DmcFloss, int>();
            DmcFlossMap = new DmcFloss[image.Height, image.Width];
            DmcPalette = new List<DmcFloss>();
            pixels = new List<Color>();
            Palette = new List<Color>();
            _image = image;
            SetDitherer(dithererType, dithererStrength);
            SetColorComparer(colorComparer);
        }
        public void SetDitherer(DithererType type, int dithererStrength)
        {
            switch (type)
            {
                case DithererType.None:
                    ditherer = new NoneDitherer(_image);
                    break;
                case DithererType.FloydSteinberg:
                    ditherer = new FloydSteinbergDitherer(_image, dithererStrength);
                    break;
                case DithererType.Atkinson:
                    ditherer = new AtkinsonDitherer(_image, dithererStrength);
                    break;
                case DithererType.Sierra:
                    ditherer = new SierraDitherer(_image, dithererStrength);
                    break;
                case DithererType.Stucki:
                    ditherer = new StuckiDitherer(_image, dithererStrength);
                    break;
                case DithererType.Pigeon:
                    ditherer = new PigeonDitherer(_image, dithererStrength);
                    break;
                default:
                    ditherer = new NoneDitherer(_image);
                    break;
            }
        }
        public void SetColorComparer(ColorComparerType type)
        {
            switch (type)
            {
                case ColorComparerType.DE76:
                    colorComparer = new DE76Comparer();
                    break;
                case ColorComparerType.DE2000:
                    colorComparer = new DE2000Comparer();
                    break;
                case ColorComparerType.CMC:
                    colorComparer = new CMCComparer();
                    break;
                case ColorComparerType.EuclideanDistance:
                    colorComparer = new EuclideanDistanceComparer();
                    break;
                case ColorComparerType.WeightedEuclideanDistance:
                    colorComparer = new WeightedEuclideanDistanceComparer();
                    break;
                default:
                    colorComparer = new DE76Comparer();
                    break;
            }
        }
        public virtual void SetImage(Image<Rgb, double> image)
        {
            _image = image;
            pixels.Clear();
            Palette.Clear();
            DmcPalette.Clear();
            DmcFlossCount.Clear();
            DmcFlossMap = new DmcFloss[image.Height, image.Width];
        }
        public virtual void GeneratePalette<T>(int paletteSize, bool generateDmcPalette = true) where T : struct, Emgu.CV.IColor
        {
            pixels.Clear();
            var cImage = _image.Convert<T, byte>();
            for (int h = 0; h < _image.Height; h++)
            {
                for (int w = 0; w < _image.Width; w++)
                {
                    pixels.Add(new Color((int)cImage.Data[h, w, 0], (int)cImage.Data[h, w, 1], (int)cImage.Data[h, w, 2]));
                }
            }
            Palette.Clear();
            DmcPalette.Clear();
            MakePalette(paletteSize);
            //convert palette back to RGB
            var convertHelper = new Image<T, byte>(1, Palette.Count);
            for (int i = 0; i < Palette.Count; i++)
            {
                convertHelper.Data[i, 0, 0] = (byte)Palette[i].X;
                convertHelper.Data[i, 0, 1] = (byte)Palette[i].Y;
                convertHelper.Data[i, 0, 2] = (byte)Palette[i].Z;
            }
            var rgb = convertHelper.Convert<Rgb, byte>();
            for (int i = 0; i < Palette.Count; i++)
            {
                Palette[i] = new Color((int)rgb.Data[i, 0, 0], (int)rgb.Data[i, 0, 1], (int)rgb.Data[i, 0, 2]);
            }
            if (generateDmcPalette)
                GenerateDmcPalette();

            
        }

        protected abstract void MakePalette(int paletteSize);

        protected virtual void GenerateDmcPalette()
        {
            var dmcColors = Flosses.Dmc;
            for (int i = 0; i < Palette.Count; i++)
            {
                var deltaE = new double[dmcColors.Count];
                var color1 = new Color(Palette[i].X, Palette[i].Y, Palette[i].Z);
                for (int j = 0; j < dmcColors.Count; j++)
                {
                    var color2 = new Color(dmcColors[j].Red, dmcColors[j].Green, dmcColors[j].Blue);
                    deltaE[j] = colorComparer.Compare(color1, color2);
                }
                var dmc = dmcColors[Array.IndexOf(deltaE, deltaE.Min())];
                if (!DmcPalette.Contains(dmc))
                    DmcPalette.Add(dmc);
            }
        }

        public virtual Image<Rgb, double> GetQuantizedImage(bool useDmcColors = true)
        {
            if (useDmcColors && (DmcPalette.Count == 0))
                throw new Exception("Cannot quantize image with DMC colors without generating a DMC palette");

            var newImage = _image.Copy();
            ditherer.SetImage(newImage);
            if (!useDmcColors)
            {
                for (int h = 0; h < newImage.Height; h++)
                {
                    for (int w = 0; w < newImage.Width; w++)
                    {
                        var deltaE = new double[Palette.Count];
                        var color1 = new Color(newImage.Data[h, w, 0], newImage.Data[h, w, 1], newImage.Data[h, w, 2]);
                        for (int i = 0; i < Palette.Count; i++)
                        {
                            var color2 = new Color(Palette[i].X, Palette[i].Y, Palette[i].Z);
                            deltaE[i] = colorComparer.Compare(color1, color2);
                        }
                        var color = Palette[Array.IndexOf(deltaE, deltaE.Min())];
                        ditherer.Dither(h, w, color);
                        newImage[h, w] = new Rgb(color.X, color.Y, color.Z);
                        
                    }
                }
            }
            else
            {
                DmcFlossCount.Clear();
                for (int h = 0; h < newImage.Height; h++)
                {
                    for (int w = 0; w < newImage.Width; w++)
                    {
                        var deltaE = new double[DmcPalette.Count];
                        var color1 = new Color(newImage.Data[h, w, 0], newImage.Data[h, w, 1], newImage.Data[h, w, 2]);
                        for (int i = 0; i < DmcPalette.Count; i++)
                        {
                            var color2 = new Color(DmcPalette[i].Red, DmcPalette[i].Green, DmcPalette[i].Blue);
                            deltaE[i] = colorComparer.Compare(color1, color2);
                        }
                        var dmc = DmcPalette[Array.IndexOf(deltaE, deltaE.Min())];
                        ditherer.Dither(h, w, new Color((int)dmc.Red, (int)dmc.Green, (int)dmc.Blue));
                        newImage[h, w] = new Rgb(dmc.Red, dmc.Green, dmc.Blue);
                        DmcFlossCount.AddOrUpdate(dmc, 1, (dmc, count) => count + 1);
                        DmcFlossMap[h, w] = dmc;
                    }
                }
            }
            return newImage;
        }

        public virtual ExcelPackage GenerateExcelSpreadsheet()
        {
            if (DmcFlossCount.Count == 0)
                throw new Exception("Cannot generate Excel spreadsheet without generating a quantized image with DMC flosses");

            var p = new ExcelPackage();
                var flossesUsed = new List<DmcFloss>();
            var flossCountList = DmcFlossCount.ToList();
            foreach (var floss in DmcFlossCount)
            {
                flossesUsed.Add(floss.Key);
            }
            var worksheet = p.Workbook.Worksheets.Add("Image");
            worksheet.DefaultRowHeight = 18.75;
            worksheet.DefaultColWidth = 2.86 * 1.25;
            for (int h = 0; h < _image.Height; h++)
            {
                for (int w = 0; w < _image.Width; w++)
                {
                    var floss = DmcFlossMap[h, w];
                    worksheet.Cells[h + 1, w + 1].Value = flossesUsed.IndexOf(floss) + 1;
                    var color = System.Drawing.Color.FromArgb((int)floss.Red, (int)floss.Green, (int)floss.Blue);
                    worksheet.Cells[h + 1, w + 1].Style.Fill.SetBackground(color);
                    worksheet.Cells[h + 1, w + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[h + 1, w + 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;


                }
            }
            var legend = p.Workbook.Worksheets.Add("Floss legend");
            legend.Cells["A1"].Value = "No.";
            legend.Cells["B1"].Value = "DMC ID";
            legend.Cells["C1"].Value = "Name";
            legend.Cells["d1"].Value = "No. of stiches";
            for (int i = 0; i < DmcFlossCount.Count; i++)
            {
                legend.Cells[i + 2, 1].Value = i + 1;
                legend.Cells[i + 2, 2].Value = flossCountList[i].Key.Number;
                legend.Cells[i + 2, 3].Value = flossCountList[i].Key.Description;
                legend.Cells[i + 2, 4].Value = flossCountList[i].Value;
            }
            legend.DefaultColWidth = 0;
            for (int i = 1; i < 5; i++)
                legend.Column(i).AutoFit();

            return p;

        }
    }
}
