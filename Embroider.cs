using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider
{
    public class Embroider
    {
        private Image<Luv, double> image;
        private int width;
        private int height;

        public Embroider(string imagePath)
        {
            image = new Image<Luv, double>(imagePath);
            width = image.Width;
            height = image.Height;
        }


    }
}
