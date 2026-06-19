using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;

namespace sac;

public class Settings
{
    // Default global settings values 
    public int ClockSize { get; set; } 
    public bool ShowFlag { get; set; } 
    public bool ShowDayAndFullTime { get; set; } 
    public bool ShowSecondHand { get; set; } 
    public bool ShowWorkingHours { get; set; } 
    public TimeOnly WorkingHoursStart { get; set; } 
    public TimeOnly WorkingHoursStop { get; set; } 
    public bool RunAtStartup { get; set; } 
    public int ClockFormXPosition { get; set; } 
    public int ClockFormYPosition { get; set; } 
    public int ClockFormWidth { get; set; } 
    public int ClockFormHeight { get; set; } 

    public List<Clock> Clocks { get; set; } = new List<Clock>();


    /// <summary>
    /// Default settings with 15 popular cities around the world. These are used if the config file is not found.
    /// <returns></returns>
    public static Settings CreateDefaults()
    {
        var s = new Settings
        {
            ClockSize = 1,
            ShowFlag = true,
            ShowDayAndFullTime = true,
            ShowSecondHand = true,
            ShowWorkingHours = true,
            WorkingHoursStart = new TimeOnly(9, 0),
            WorkingHoursStop = new TimeOnly(17, 0),
            RunAtStartup = false,
            ClockFormXPosition = 800,
            ClockFormYPosition = 200,
            ClockFormWidth = 650,
            ClockFormHeight = 800
        };

        s.Clocks.Add(new Clock("Toronto", "Canada", "Toronto", "America/Toronto", "ca", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("New York", "United States", "New York", "America/New_York", "us", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("Buenos Aires", "Argentina", "Buenos Aires", "America/Argentina/Buenos_Aires", "ar", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("São Paulo", "Brazil", "São Paulo", "America/Sao_Paulo", "br", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("London", "United Kingdom", "London", "Europe/London", "gb", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("Warsaw", "Poland", "Warsaw", "Europe/Warsaw", "pl", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("Johannesburg", "South Africa", "Pretoria", "Africa/Johannesburg", "za", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("Cairo", "Egypt", "Cairo", "Africa/Cairo", "eg", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("Moscow", "Russia", "Moscow", "Europe/Moscow", "ru", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("New Delhi", "India", "New Delhi", "Asia/Kolkata", "in", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("Jakarta", "Indonesia", "Jakarta", "Asia/Jakarta", "id", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("Beijing", "China", "Beijing", "Asia/Shanghai", "cn", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("Seoul", "South Korea", "Seoul", "Asia/Seoul", "kr", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("Tokyo", "Japan", "Tokyo", "Asia/Tokyo", "jp", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        s.Clocks.Add(new Clock("Sydney", "Australia", "Sydney", "Australia/Sydney", "au", 1, true, true, true, true, new TimeOnly(9, 0), new TimeOnly(17, 0)));

 
        return s;
    }
}


