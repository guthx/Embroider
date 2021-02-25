using Embroider.Ditherers;
using Embroider.Quantizers;
using Emgu.CV;
using Emgu.CV.Structure;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static Embroider.Enums;

namespace Embroider
{
    public class Embroider
    {
        private Image<Rgb, double> _image;
        private Image<Rgb, double> _reducedImage;
        private Image<Rgb, double> _reducedDmcImage;
        private Quantizer _quantizer;
        private Image<Rgb, double> _quantizedImage;
        private EmbroiderOptions _options;
        public EmbroiderOptions Options {
            get => _options;
            set
            {
                if (value.StichSize != _options.StichSize)
                {
                    setReducedImage(_image, value.StichSize);
                    setQuantizer(value.QuantizerType, value.OctreeMode, value.DithererType);
                    _quantizedImage = null;
                    _reducedDmcImage = null;
                }
                else if (value.QuantizerType != _options.QuantizerType ||
                    value.OctreeMode != _options.OctreeMode ||
                    value.ColorComparerType != _options.ColorComparerType)
                {
                    setQuantizer(value.QuantizerType, value.OctreeMode, value.DithererType, value.ColorComparerType);
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
        public Embroider(Image<Rgb, double> image)
        {
            _options = new EmbroiderOptions();
            _image = image;
            setReducedImage(_image, Options.StichSize);
            setQuantizer(Options.QuantizerType, Options.OctreeMode, Options.DithererType, Options.ColorComparerType, Options.DithererStrength);
        }
        public Embroider(Image<Rgb, double> image, EmbroiderOptions options)
        {
            _options = options;
            _image = image;
            setReducedImage(_image, Options.StichSize);
            setQuantizer(Options.QuantizerType, Options.OctreeMode, Options.DithererType, Options.ColorComparerType, Options.DithererStrength);
        }
        public Image<Rgb, double> GenerateImage()
        {
            if (_quantizedImage == null)
            {
                if (Options.OperationOrder == OperationOrder.ReplacePixelsFirst)
                {
                    if (_reducedDmcImage == null)
                    {
                        _reducedDmcImage = _reducedImage.Copy();
                        ImageProcessing.ReplacePixelsWithDMC(_reducedDmcImage, _quantizer.colorComparer, _quantizer.ditherer);
                    }
                    _quantizer.SetImage(_reducedDmcImage);
                }
                else
                {
                    _quantizer.SetImage(_reducedImage);
                }
                switch (Options.ColorSpace)
                {
                    case ColorSpace.Rgb:
                        _quantizer.GeneratePalette<Rgb>(Options.MaxColors);
                        break;
                    case ColorSpace.Lab:
                        _quantizer.GeneratePalette<Lab>(Options.MaxColors);
                        break;
                    case ColorSpace.Hsv:
                        _quantizer.GeneratePalette<Hsv>(Options.MaxColors);
                        break;
                    case ColorSpace.Ycc:
                        _quantizer.GeneratePalette<Ycc>(Options.MaxColors);
                        break;
                    case ColorSpace.Luv:
                        _quantizer.GeneratePalette<Luv>(Options.MaxColors);
                        break;
                    default:
                        _quantizer.GeneratePalette<Rgb>(Options.MaxColors);
                        break;
                };
                _quantizedImage = _quantizer.GetQuantizedImage(true);
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
        private void setReducedImage(Image<Rgb, double> image, int size)
        {
            _reducedImage = ImageProcessing.MeanReduce(image, size);
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
        /// How many pixels (in width and height) from original image will make up a single stitch. <br/>
        /// Default: 4
        /// </summary>
        public int StichSize { get; set; } = 4;
        /// <summary>
        /// Maximum number of different colored stitches. <br/>
        /// Default: 64
        /// </summary>
        public int MaxColors { get; set; } = 64;
        private int _outputStitchSize = 0;
        /// <summary>
        /// How many pixels (in width and height) make up a single stitch in output image. <br/>
        /// If set to 0 (default) it will the number of pixels in StitchSize
        /// </summary>
        public int OutputStitchSize
        {
            get
            {
                if (_outputStitchSize == 0)
                    return StichSize;
                else
                    return _outputStitchSize;
            }
            set
            {
                _outputStitchSize = value;
            }
        }
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
    }

    


}
