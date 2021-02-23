using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Embroider
{
    public static class Flosses
    {
        private static List<DmcFloss> dmcFlosses;

        public static List<DmcFloss> Dmc
        {
            get
            {
                if (dmcFlosses == null)
                {
                    dmcFlosses = new List<DmcFloss>();
                    using (var reader = new StreamReader(@"F:\Inne\ahri\dmc_lab2.csv"))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        dmcFlosses = csv.GetRecords<DmcFloss>().ToList();
                    }
                }
                return dmcFlosses;
            }
        }
    }
}
