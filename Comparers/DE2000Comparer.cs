using Embroider.Quantizers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Embroider.Comparers
{
    public class DE2000Comparer : ColorComparer
    {
        public override double Compare(Color color1, Color color2)
        {
            var lab1 = color1.RgbToLab();
            var lab2 = color2.RgbToLab();

            double kL = 1.0;
            double kC = 1.0;
            double kH = 1.0;
            double lBarPrime = 0.5 * (lab1.X + lab2.X);
            double c1 = Math.Sqrt(lab1.Y * lab1.Y + lab1.Z * lab1.Z);
            double c2 = Math.Sqrt(lab2.Y * lab2.Y + lab2.Z * lab2.Z);
            double cBar = 0.5 * (c1 + c2);
            double cBar7 = cBar * cBar * cBar * cBar * cBar * cBar * cBar;
            double g = 0.5 * (1.0 - Math.Sqrt(cBar7 / (cBar7 + 6103515625.0)));  /* 6103515625 = 25^7 */
            double a1Prime = lab1.Y * (1.0 + g);
            double a2Prime = lab2.Y * (1.0 + g);
            double c1Prime = Math.Sqrt(a1Prime * a1Prime + lab1.Z * lab1.Z);
            double c2Prime = Math.Sqrt(a2Prime * a2Prime + lab2.Z * lab2.Z);
            double cBarPrime = 0.5 * (c1Prime + c2Prime);
            double h1Prime = (Math.Atan2(lab1.Z, a1Prime) * 180.0) / Math.PI;
            double dhPrime; // not initialized on purpose
            if (h1Prime < 0.0)
                h1Prime += 360.0;
            double h2Prime = (Math.Atan2(lab2.Z, a2Prime) * 180.0) / Math.PI;
            if (h2Prime < 0.0)
                h2Prime += 360.0;
            double hBarPrime = (Math.Abs(h1Prime - h2Prime) > 180.0) ? (0.5 * (h1Prime + h2Prime + 360.0)) : (0.5 * (h1Prime + h2Prime));
            double t = 1.0 -
            0.17 * Math.Cos(Math.PI * (hBarPrime - 30.0) / 180.0) +
            0.24 * Math.Cos(Math.PI * (2.0 * hBarPrime) / 180.0) +
            0.32 * Math.Cos(Math.PI * (3.0 * hBarPrime + 6.0) / 180.0) -
            0.20 * Math.Cos(Math.PI * (4.0 * hBarPrime - 63.0) / 180.0);
            if (Math.Abs(h2Prime - h1Prime) <= 180.0)
                dhPrime = h2Prime - h1Prime;
            else
                dhPrime = (h2Prime <= h1Prime) ? (h2Prime - h1Prime + 360.0) : (h2Prime - h1Prime - 360.0);
            double dLPrime = lab2.X - lab1.X;
            double dCPrime = c2Prime - c1Prime;
            double dHPrime = 2.0 * Math.Sqrt(c1Prime * c2Prime) * Math.Sin(Math.PI * (0.5 * dhPrime) / 180.0);
            double sL = 1.0 + ((0.015 * (lBarPrime - 50.0) * (lBarPrime - 50.0)) / Math.Sqrt(20.0 + (lBarPrime - 50.0) * (lBarPrime - 50.0)));
            double sC = 1.0 + 0.045 * cBarPrime;
            double sH = 1.0 + 0.015 * cBarPrime * t;
            double dTheta = 30.0 * Math.Exp(-((hBarPrime - 275.0) / 25.0) * ((hBarPrime - 275.0) / 25.0));
            double cBarPrime7 = cBarPrime * cBarPrime * cBarPrime * cBarPrime * cBarPrime * cBarPrime * cBarPrime;
            double rC = Math.Sqrt(cBarPrime7 / (cBarPrime7 + 6103515625.0));
            double rT = -2.0 * rC * Math.Sin(Math.PI * (2.0 * dTheta) / 180.0);
            return (Math.Sqrt(
                               (dLPrime / (kL * sL)) * (dLPrime / (kL * sL)) +
                               (dCPrime / (kC * sC)) * (dCPrime / (kC * sC)) +
                               (dHPrime / (kH * sH)) * (dHPrime / (kH * sH)) +
                               (dCPrime / (kC * sC)) * (dHPrime / (kH * sH)) * rT
                          )
             );
        }
    }
}
