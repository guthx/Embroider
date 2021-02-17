using Emgu.CV;
using Emgu.CV.Structure;
using OfficeOpenXml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Embroider.Quantizers
{
    public abstract class Quantizer
    {
        public List<Color> Palette;
        public List<DmcFloss> DmcPalette;
        protected List<Color> pixels;
        protected Image<Lab, double> _image;
        public ConcurrentDictionary<DmcFloss, int> DmcFlossCount;
        public DmcFloss[,] DmcFlossMap;
        
        public Quantizer(Image<Lab, double> image)
        {
            DmcFlossCount = new ConcurrentDictionary<DmcFloss, int>();
            DmcFlossMap = new DmcFloss[image.Height, image.Width];
            DmcPalette = new List<DmcFloss>();
            pixels = new List<Color>();
            Palette = new List<Color>();
            _image = image;
            
        }
        public virtual void SetImage(Image<Lab, double> image)
        {
            _image = image;
            pixels.Clear();
            Palette.Clear();
            DmcPalette.Clear();
            DmcFlossCount.Clear();
            DmcFlossMap = new DmcFloss[image.Height, image.Width];
        }
        public virtual void GeneratePalette(int paletteSize, bool generateDmcPalette = true) 
        {
            pixels.Clear();
            for (int h = 0; h < _image.Height; h++)
            {
                for (int w = 0; w < _image.Width; w++)
                {
                    pixels.Add(new Color((int)_image[h, w].X, (int)_image[h, w].Y, (int)_image[h, w].Z));
                }
            }
            Palette.Clear();
            DmcPalette.Clear();
            MakePalette(paletteSize);
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
                var color1 = new Lab2(Palette[i].X / 2.55, Palette[i].Y - 128, Palette[i].Z - 128);
                for (int j = 0; j < dmcColors.Count; j++)
                {
                    var color2 = new Lab2(dmcColors[j].L / 2.55, dmcColors[j].a - 128, dmcColors[j].b - 128);
                    deltaE[j] = Lab2.CompareCMC(color1, color2);
                }
                var dmc = dmcColors[Array.IndexOf(deltaE, deltaE.Min())];
                if (!DmcPalette.Contains(dmc))
                    DmcPalette.Add(dmc);
            }
        }

        public virtual Image<Lab, double> GetQuantizedImage(bool useDmcColors = true)
        {
            if (useDmcColors && (DmcPalette.Count == 0))
                throw new Exception("Cannot quantize image with DMC colors without generating a DMC palette");

            var newImage = _image.Copy();
            if (!useDmcColors)
            {
                for (int h = 0; h < newImage.Height; h++)
                {
                    for (int w = 0; w < newImage.Width; w++)
                    {
                        var deltaE = new double[Palette.Count];
                        var color1 = new Lab2(newImage[h, w].X / 2.55, newImage[h, w].Y - 128, newImage[h, w].Z - 128);
                        for (int i = 0; i < Palette.Count; i++)
                        {
                            var color2 = new Lab2(Palette[i].X / 2.55, Palette[i].Y - 128, Palette[i].Z - 128);
                            deltaE[i] = Lab2.CompareCMC(color1, color2);
                        }
                        var color = Palette[Array.IndexOf(deltaE, deltaE.Min())];
                        newImage[h, w] = new Lab(color.X, color.Y, color.Z);
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
                        var color1 = new Lab2(newImage[h, w].X / 2.55, newImage[h, w].Y - 128, newImage[h, w].Z - 128);
                        for (int i = 0; i < DmcPalette.Count; i++)
                        {
                            var color2 = new Lab2(DmcPalette[i].L / 2.55, DmcPalette[i].a - 128, DmcPalette[i].b - 128);
                            deltaE[i] = Lab2.CompareCMC(color1, color2);
                        }
                        var dmc = DmcPalette[Array.IndexOf(deltaE, deltaE.Min())];
                        newImage[h, w] = new Lab(dmc.L, dmc.a, dmc.b);
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
                    var color = System.Drawing.Color.FromArgb((int)floss.R, (int)floss.G, (int)floss.B);
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
