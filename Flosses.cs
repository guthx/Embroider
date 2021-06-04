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
        private static List<Floss> dmcFlosses;
        public static List<Floss> Dmc()
        {
            if (dmcFlosses == null)
            {
                dmcFlosses = new List<Floss>();
                using (var reader = new StreamReader(@"dmc_lab2.csv"))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    dmcFlosses = csv.GetRecords<Floss>().ToList();
                }
            }
            return dmcFlosses;
        }
    }
}
