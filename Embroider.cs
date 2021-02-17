using Embroider.Quantizers;
using Emgu.CV;
using Emgu.CV.Structure;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Embroider
{
    public class Embroider
    {
        private Image<Lab, double> _image;
        private Image<Lab, double> _reducedImage;
        private Quantizer _quantizer;
        private Image<Lab, double> _quantizedImage;
        private ExcelPackage _spreadsheet;
        private EmbroiderOptions _options;
        public EmbroiderOptions Options {
            get => _options;
            set
            {
                if (value.StichSize != _options.StichSize)
                {
                    setReducedImage(_image, value.StichSize);
                    setQuantizer(value.QuantizerType, value.OctreeMode);
                    _quantizedImage = null;
                    _spreadsheet = null;
                }
                else if (value.QuantizerType != _options.QuantizerType ||
                    value.OctreeMode != _options.OctreeMode)
                {
                    setQuantizer(value.QuantizerType, value.OctreeMode);
                    _quantizedImage = null;
                    _spreadsheet = null;
                }  
                else if (value.MaxColors != _options.MaxColors ||
                    value.OperationOrder != Options.OperationOrder)
                {
                    _quantizedImage = null;
                    _spreadsheet = null;
                }
                _options = value;
            }
        }
        public Embroider(Image<Lab, double> image)
        {
            _options = new EmbroiderOptions();
            _image = image;
            setReducedImage(_image, Options.StichSize);
            setQuantizer(Options.QuantizerType, Options.OctreeMode);
        }
        public Embroider(Image<Lab, double> image, EmbroiderOptions options)
        {
            _options = options;
            _image = image;
            setReducedImage(_image, Options.StichSize);
            setQuantizer(Options.QuantizerType, Options.OctreeMode);
        }
        public Image<Lab, double> GenerateImage()
        {
            if (_quantizedImage == null)
            {
                if (Options.OperationOrder == OperationOrder.ReplacePixelsFirst)
                {
                    var newImage = _reducedImage.Copy();
                    ImageProcessing.ReplacePixelsWithDMC(newImage);
                    _quantizer.SetImage(newImage);
                }
                else
                {
                    _quantizer.SetImage(_reducedImage);
                }
                _quantizer.GeneratePalette(Options.MaxColors);
                _quantizedImage = _quantizer.GetQuantizedImage();
            }
            return ImageProcessing.Stretch(_quantizedImage, Options.OutputStitchSize, Options.Net);
        }
        public ExcelPackage GenerateExcelSpreadsheet()
        {
            if (_quantizedImage == null)
                GenerateImage();
            if (_spreadsheet == null)
                _spreadsheet = _quantizer.GenerateExcelSpreadsheet();
            return _spreadsheet;
        }
        private void setReducedImage(Image<Lab, double> image, int size)
        {
            _reducedImage = ImageProcessing.MeanReduce(image, size);
        }
        private void setQuantizer(QuantizerType type, MergeMode octreeMode = MergeMode.LEAST_IMPORTANT)
        {
            switch (type)
            {
                case QuantizerType.SimplePopularity:
                    _quantizer = new SimplePopularityQuantizer(_reducedImage);
                    break;
                case QuantizerType.Popularity:
                    _quantizer = new PopularityQuantizer(_reducedImage);
                    break;
                case QuantizerType.MedianCut:
                    _quantizer = new MedianCutQuantizer(_reducedImage);
                    break;
                case QuantizerType.KMeans:
                    _quantizer = new KMeansQuantizer(_reducedImage);
                    break;
                case QuantizerType.Octree:
                    _quantizer = new OctreeQuantizer(_reducedImage, 8, octreeMode);
                    break;
                default:
                    _quantizer = new OctreeQuantizer(_reducedImage, 8);
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
        /// Default: ReplacePixelsFirst
        /// </summary>
        public OperationOrder OperationOrder { get; set; } = OperationOrder.ReplacePixelsFirst;
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
    }

    public enum QuantizerType
    {
        SimplePopularity, Popularity, Octree, MedianCut, KMeans
    }

    public enum OperationOrder
    {
        QuantizeFirst, ReplacePixelsFirst
    }


}
