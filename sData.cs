using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sac;

internal static class sData
{
    // Collection of all countries and their cities/time zones which are loaded from the sac_timezones_data.csv file.
    public static List<Country> Countries { get; set; } = new List<Country>();
}
