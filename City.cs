using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sac;

// Uploaded .csv file (with timezones) is stored in LIST of Country objects (stored in sData), each containing a LIST of City objects.
internal class City
{
    public string Name { get; set; }  // City name
    public string IANATimeZone { get; set; }  // City 's IANA time zone (e.g. "America/New_York", "Europe/London", etc.)

    public City(string name, string ianaTimeZone)
    {
        this.Name = name;
        this.IANATimeZone = ianaTimeZone;
    }
}
