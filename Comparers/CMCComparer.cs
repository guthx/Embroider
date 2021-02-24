using Embroider.Quantizers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Comparers
{
    public class CMCComparer : ColorComparer
    {
        public override double Compare(Color color1, Color color2)
        {
            double l = 1, c = 1;
            var lab1 = color1.RgbToLab();
            var lab2 = color2.RgbToLab();

            double c1 = Math.Sqrt(lab1.Y * lab1.Y + lab1.Z * lab1.Z);
            double c2 = Math.Sqrt(lab2.Y * lab2.Y + lab2.Z * lab2.Z);
            double deltaC = c1 - c2;
            double deltaH = Math.Sqrt(Math.Pow(lab1.Y - lab2.Y, 2) + Math.Pow(lab1.Z - lab2.Z, 2) - Math.Pow(deltaC, 2));
            if (deltaH.Equals(double.NaN))
                deltaH = 0;
            double deltaL = lab1.X - lab2.X;
            double SL = 0.511;
            if (lab1.X >= 16)
            {
                SL = (0.040975 * lab1.X) / (1 + 0.01765 * lab1.X);
            }
            double SC = (0.0638 * c1) / (1 + 0.0131 * c1) + 0.638;
            double H = Math.Atan2(lab1.Z, lab1.Y) * (180.0 / Math.PI);
            double H1 = H;
            if (H < 0)
                H1 = H + 360;
            double F = Math.Sqrt(Math.Pow(c1, 4) / (Math.Pow(c1, 4) + 1900));
            double T;
            if (H1 >= 164 && H1 <= 345)
            {
                T = 0.56 + Math.Abs(0.2 * Math.Cos((H1 + 168) * (Math.PI / 180.0)));
            }
            else
            {
                T = 0.36 + Math.Abs(0.4 * Math.Cos((H1 + 35) * (Math.PI / 180.0)));
            }
            double SH = SC * (F * T + 1 - F);
            return Math.Sqrt(
                Math.Pow(deltaL / (l * SL), 2) +
                Math.Pow(deltaC / (c * SC), 2) +
                Math.Pow(deltaH / SH, 2)
                );
        }
    }
}
