using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sac;

// Uploaded .csv file with timezones is stored in LIST of Country objects (stored in sData), each containing a LIST of City objects.

internal class Country
{
    public string Name { get; set; } // Country name
    public string Code { get; set; } // Country flag code

    // List of cities in the country. Usually only one city per country,
    // but some countries have more than one city with different time zones (e.g. USA, Russia, etc.)
    public List<City> Cities { get; set; } = new List<City>(); 
    
    
    public Country(string name, string code)
    {
        this.Name = name;
        this.Code = code;
    }

    public void AddCity(City city)
    {
        this.Cities.Add(city);
    }
}
