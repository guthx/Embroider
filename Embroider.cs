using Embroider.Ditherers;
using Embroider.Quantizers;
using OfficeOpenXml;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static Embroider.Enums;

namespace Embroider
{
    public class Embroider
    {
        private Image<Rgb24> _image;
        private Image<Rgb24> _reducedImage;
        private Image<Rgb24> _reducedDmcImage;
        private Quantizer _quantizer;
        private Image<Rgb24> _quantizedImage;
        private EmbroiderOptions _options;
        private Dictionary<string, List<DmcFloss>> _palettes;
        public EmbroiderOptions Options {
            get => _options;
            set
            {
                if (value.WidthStitchCount != _options.WidthStitchCount ||
                    value.StitchSize != _options.StitchSize)
                {
                    setReducedImage(_image, value.StitchSize, value.WidthStitchCount);
                    setQuantizer(value.QuantizerType, value.OctreeMode, value.DithererType, value.ColorComparerType, value.DithererStrength);
                    _quantizedImage = null;
                    _reducedDmcImage = null;
                }
                else if (value.QuantizerType != _options.QuantizerType ||
                    value.OctreeMode != _options.OctreeMode ||
                    value.ColorComparerType != _options.ColorComparerType)
                {
                    setQuantizer(value.QuantizerType, value.OctreeMode, value.DithererType, value.ColorComparerType, value.DithererStrength);
                    _quantizedImage = null;
                }
                else if (value.DithererType != _options.DithererType ||
                    value.DithererStrength != _options.DithererStrength)
                {
                    _quantizer.SetDitherer(value.DithererType, value.DithererStrength);
                    _quantizedImage = null;
                }
                else if (value.MaxColors != _options.MaxColors ||
                    value.OperationOrder != Options.OperationOrder ||
                    value.ColorSpace != Options.ColorSpace)
                {
                    _quantizedImage = null;
                }
                _options = value;
            }
        }
        public Embroider(Image<Rgb24> image)
        {
            _options = new EmbroiderOptions();
            _image = image;
            setReducedImage(_image, Options.StitchSize, Options.WidthStitchCount);
            setQuantizer(Options.QuantizerType, Options.OctreeMode, Options.DithererType, Options.ColorComparerType, Options.DithererStrength);
            _palettes = new Dictionary<string, List<DmcFloss>>();
        }
        public Embroider(Image<Rgb24> image, EmbroiderOptions options)
        {
            _options = options;
            _image = image;
            setReducedImage(_image, Options.StitchSize, Options.WidthStitchCount);
            setQuantizer(Options.QuantizerType, Options.OctreeMode, Options.DithererType, Options.ColorComparerType, Options.DithererStrength);
            _palettes = new Dictionary<string, List<DmcFloss>>();
        }
        public Image<Rgb24> GenerateImage()
        {
            if (_quantizedImage == null)
            {
                if (Options.OperationOrder == OperationOrder.ReplacePixelsFirst)
                {
                    if (_reducedDmcImage == null)
                    {
                        _reducedDmcImage = _reducedImage.Clone();
                        ImageProcessing.ReplacePixelsWithDMC(_reducedDmcImage, _quantizer.colorComparer, _quantizer.ditherer);
                    }
                    _quantizer.SetImage(_reducedDmcImage);
                }
                else
                {
                    _quantizer.SetImage(_reducedImage);
                }
                var settingsCode = Options.GetSettingsCode();
                List<DmcFloss> palette;
                if (!_palettes.TryGetValue(settingsCode, out palette))
                {
                    switch (Options.ColorSpace)
                    {
                        case ColorSpace.Rgb:
                            palette = _quantizer.GeneratePalette(Options.MaxColors, ColorSpace.Rgb);
                            break;
                        case ColorSpace.Lab:
                            palette = _quantizer.GeneratePalette(Options.MaxColors, ColorSpace.Lab);
                            break;
                        default:
                            palette = _quantizer.GeneratePalette(Options.MaxColors, ColorSpace.Rgb);
                            break;
                    };
                    _palettes[settingsCode] = palette;
                }
                _quantizedImage = _quantizer.GetQuantizedImage(palette);
            }
            return ImageProcessing.Stretch(_quantizedImage, Options.OutputStitchSize, Options.Net);
        }
        public ExcelPackage GenerateExcelSpreadsheet()
        {
            if (_quantizedImage == null)
                GenerateImage();
            return _quantizer.GenerateExcelSpreadsheet();
        }
        public Summary GetSummary()
        {
            return new Summary
            {
                FlossCount = _quantizer.DmcFlossCount,
                Height = _quantizedImage.Height,
                Width = _quantizedImage.Width
            };
        }

        public StitchMap GetStitchMap()
        {
            return new StitchMap(_quantizer.DmcFlossMap, _quantizer.DmcPalette);
        }
        private void setReducedImage(Image<Rgb24> image, int stitchSize, int width)
        {
            if (stitchSize > 0)
                _reducedImage = ImageProcessing.MeanReduce(image, stitchSize);
            else
                _reducedImage = image.Clone(x => x.Resize(width, (int)((double)width / image.Width), KnownResamplers.Lanczos3));

        }
        private void setQuantizer(QuantizerType type, 
            MergeMode octreeMode = MergeMode.LEAST_IMPORTANT, 
            DithererType dithererType = DithererType.None, 
            ColorComparerType colorComparerType = ColorComparerType.DE76,
            int dithererStrength = 255)
        {
            switch (type)
            {
                case QuantizerType.SimplePopularity:
                    _quantizer = new SimplePopularityQuantizer(_reducedImage, dithererType, colorComparerType, dithererStrength);
                    break;
                case QuantizerType.Popularity:
                    _quantizer = new PopularityQuantizer(_reducedImage, dithererType, colorComparerType, dithererStrength);
                    break;
                case QuantizerType.MedianCut:
                    _quantizer = new MedianCutQuantizer(_reducedImage, dithererType, colorComparerType, dithererStrength);
                    break;
                case QuantizerType.KMeans:
                    _quantizer = new KMeansQuantizer(_reducedImage, dithererType, colorComparerType, dithererStrength);
                    break;
                case QuantizerType.Octree:
                    _quantizer = new OctreeQuantizer(_reducedImage, 8, octreeMode, dithererType, colorComparerType, dithererStrength);
                    break;
                case QuantizerType.ModifiedMedianCut:
                    _quantizer = new ModifiedMedianCutQuantizer(_reducedImage, 6, 0.85f, dithererType, colorComparerType, dithererStrength);
                    break;
                default:
                    _quantizer = new ModifiedMedianCutQuantizer(_reducedImage, 6, 0.85f, dithererType, colorComparerType, dithererStrength);
                    break;
            }
        }

    }

    public class EmbroiderOptions
    {
        /// <summary>
        /// The algorithm used to quantize image colors. <br/>
        /// Default: Octree
        /// </summary>
        public QuantizerType QuantizerType { get; set; } = QuantizerType.Octree;
        /// <summary>
        /// The order of operations when preparing an image for stitching. <br/>
        /// ReplacePixelsFirst first sets the color of each stitch into a DMC color, and then reduces the number of colors with a quantizer. <br/>
        /// QuantizeFirst first reduces the number of colors in the original image, and then replaces them by DMC colors. <br/>
        /// Default: QuantizeFirst
        /// </summary>
        public OperationOrder OperationOrder { get; set; } = OperationOrder.QuantizeFirst;
        /// <summary>
        /// How many stitches will the output image have in width <br/>
        /// Default: 100
        /// </summary>
        public int WidthStitchCount { get; set; } = 0;
        public int StitchSize { get; set; } = 4;
        /// <summary>
        /// Maximum number of different colored stitches. <br/>
        /// Default: 64
        /// </summary>
        public int MaxColors { get; set; } = 64;
        /// <summary>
        /// How many pixels (in width and height) make up a single stitch in output image. <br/>
        /// If set to 0 (default) it will the number of pixels in StitchSize
        /// </summary>
        public int OutputStitchSize { get; set; } = 4;
        /// <summary>
        /// Set to true to draw a net separating stitches in the output image. <br/>
        /// Default: false
        /// </summary>
        public bool Net { get; set; } = false;
        /// <summary>
        /// If Octree algorithm is selected in QuantizerType, selects the order in which colors are merged during quantization. <br/>
        /// LEAST_IMPORTANT (default): Colors with least representation are merged first. <br/>
        /// MOST_IMPORTANT: Colors with most representation are merged first. <br/>
        /// </summary>
        public MergeMode OctreeMode { get; set; } = MergeMode.LEAST_IMPORTANT;
        public DithererType DithererType { get; set; } = DithererType.None;
        public ColorSpace ColorSpace { get; set; } = ColorSpace.Rgb;
        public ColorComparerType ColorComparerType { get; set; } = ColorComparerType.DE76;
        public int DithererStrength { get; set; } = 255;

        public string GetSettingsCode()
        {
            var codeBuilder = new StringBuilder();
            codeBuilder.Append((int)QuantizerType);
            codeBuilder.Append((int)WidthStitchCount);
            codeBuilder.Append((int)StitchSize);
            codeBuilder.Append((int)MaxColors);
            codeBuilder.Append((int)OctreeMode);
            codeBuilder.Append((int)DithererType);
            codeBuilder.Append((int)DithererStrength);
            codeBuilder.Append((int)ColorSpace);
            codeBuilder.Append((int)ColorComparerType);

            return codeBuilder.ToString();
        }
    }

    


}
