using sac;
using sac.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace sac;

public static class sFileManager
{

    // Methods for managing external data files.

    // There are two external data files:
    // 1) tz.csv - contains list of countries, cities and their IANA time zones
    //    This file is located in the installation directory of the application and after first run,
    //    it is copied to the user's AppData directory.
    //    (...\AppData\Local\vt-soft\sac\) under name "sac_timezones_data.csv". User can modify it here if needed.
    // 2) sac_config.json - contains user settings and selected clocks.
    //    This file is also located in the user's AppData directory. 
    //    This file is created during the first run of the application.

    private static Settings settings;


    // *** public methods ******************************************************************************************

    /// <summary>
    /// Open/create necessary data files.
    /// </summary>
    public static void CheckFiles(Settings settings)
    {
        // Method will test if sac_timezones_data.csv and sac_config.json exist in AppData folder,
        // if not, it will create them here.

        sFileManager.settings = settings;
        OpenTimeZonesFile();
        OpenConfigFile();
    }

    /// <summary>
    /// Method is called when user clicks "Save" button in settings form or when ClockForm is resized.
    /// <exception cref="Exception"></exception>
    public static void SaveConfigFile()
    {
        try
        {
            string filePath = GetFilePath("sac_config.json");
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Serialize current in-memory settings
            string jsonContent = JsonSerializer.Serialize(settings, options);

            // Overwrite/create file
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.Write(jsonContent);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving config file: {ex.Message}", ex);
        }
    }



    // *** private methods ****************************************************************************************


    // ************************************************************************************************************
    // ** Time Zones Data File Methods ****************************************************************************


    /// <summary>
    /// Open and parse (into sData.Countries) the time zone (sac_timezones_data.csv) file.
    /// </summary>
    /// <exception cref="Exception"></exception>
    private static void OpenTimeZonesFile()
    {
        string filePath = GetFilePath("sac_timezones_data.csv");

        if (!File.Exists(filePath))
        {
            CreateTimeZoneFile(filePath);
        }
        try
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Skip comment lines starting with #
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("#"))
                    continue;

                // Parse CSV line (expected format: CountryName;CountryCode;CityName;IANATimeZone)
                string[] parts = trimmedLine.Split(';');

                if (parts.Length != 4) 
                {
                    continue; // Skip invalid lines - only line with 4 ";" in file are accepted. 
                }

                string countryName = parts[0].Trim();
                string countryCode = parts[1].Trim();
                string cityName = parts[2].Trim();
                string IANATimeZone = parts[3].Trim();

                if (isIANAvalid(IANATimeZone)==false)
                {
                    Debug.Print($"Invalid IANA time zone: {IANATimeZone} in line: {line}");
                    continue; // skip lines with invalid IANA time zone
                }

                // Check if country already exists
                Country? existingCountry = sData.Countries.FirstOrDefault(c => c.Name.Equals(countryName, StringComparison.OrdinalIgnoreCase));

                if (existingCountry == null)
                {
                    // Create new country
                    Country newCountry = new Country(countryName, countryCode);
                    City newCity = new City(cityName, IANATimeZone);
                    newCountry.AddCity(newCity);
                    sData.Countries.Add(newCountry);
                }
                else
                {
                    // Add city to existing country
                    City newCity = new City(cityName, IANATimeZone);
                    existingCountry.AddCity(newCity);
                }
            }
            // sort countries alphabetically
            sData.Countries = sData.Countries.OrderBy(c => c.Name).ToList();

        }
        catch (Exception ex)
        {
            // Handle errors appropriately (log, show message, etc.)
            throw new Exception($"Error reading data file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checking if IANA in csv is valid IANA
    /// </summary>
    /// <param name="tzIANA"></param>
    /// <returns></returns>
    private static bool isIANAvalid(string tzIANA)
    {
        string tzWin = tzIANA;
        try
        {
            // Try to map IANA to Windows timezone.
            if (TZConvert.TryIanaToWindows(tzIANA, out var mappedWindows))
            {
                tzWin= mappedWindows;
            }

            // Get the TimeZoneInfo object for the selected Windows timezone ID. 
             _ = TimeZoneInfo.FindSystemTimeZoneById(tzWin);
            
        }
        catch
        {
            // Maybe later we can log invalid IANA time zones to a file or show a message to the user.
            // For now we are just skipping lines with invalid IANA time zones.
            return false;
        }
        return true;
    }


    /// <summary>
    /// Create timezone file in the user's AppData directory.
    /// </summary>
    /// <param name="timeZonesFilePath"></param>
    /// <exception cref="FileNotFoundException"></exception>
    private static void CreateTimeZoneFile(string timeZonesFilePath)
    {
        string defaultCsvPath = Path.Combine(AppContext.BaseDirectory, "tz.csv");

        if (File.Exists(defaultCsvPath))
        {
            File.Copy(defaultCsvPath, timeZonesFilePath);
        }
        else
        {
            throw new FileNotFoundException($"File not found at: {defaultCsvPath}");
        }
    }


    /// <summary>
    /// Retrieves the path to the file in the user's AppData directory.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    private static string GetFilePath(string fileName)
    {
        // Define the AppData folder for your app
        string appDataFolder = Path.Combine(
                               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                               "vt-soft\\sac");

        // Ensure the folder exists
        Directory.CreateDirectory(appDataFolder);

        return Path.Combine(appDataFolder, fileName);

    }

    // **************************************************************************************************************
    // ** Config File Methods ***************************************************************************************   

    /// <summary>
    /// Open the config file (sac_config.json) from the user's AppData directory. 
    /// If it doesn't exist, create it with default settings.
    /// </summary>
    /// <exception cref="Exception"></exception>
    private static void OpenConfigFile()
    {
        string filePath = GetFilePath("sac_config.json");

        if (!File.Exists(filePath))
        {
            CreateConfigFile(filePath);
        }
        else
        {
            try // read existing config file
            {

                // Read the json file and convert it to a string.
                string jsonContent = File.ReadAllText(filePath, Encoding.UTF8);

                // Deserialize the json string to a GSettings object.
                Settings? loaded = JsonSerializer.Deserialize<Settings>(jsonContent);

                if (loaded != null && settings != null)
                {
                    // Update the existing instance so all holders (ClockForm, ClockManager, SettingForm) see updates
                    settings.ClockSize = loaded.ClockSize;
                    settings.ShowFlag = loaded.ShowFlag;
                    settings.ShowDayAndFullTime = loaded.ShowDayAndFullTime;
                    settings.ShowSecondHand = loaded.ShowSecondHand;
                    settings.ShowWorkingHours = loaded.ShowWorkingHours;
                    settings.WorkingHoursStart = loaded.WorkingHoursStart;
                    settings.WorkingHoursStop = loaded.WorkingHoursStop;
                    settings.RunAtStartup = loaded.RunAtStartup;
                    settings.ClockFormXPosition = loaded.ClockFormXPosition;
                    settings.ClockFormYPosition = loaded.ClockFormYPosition;
                    settings.ClockFormWidth = loaded.ClockFormWidth;
                    settings.ClockFormHeight = loaded.ClockFormHeight;

                    // Update Clocks list
                    settings.Clocks.Clear();
                    foreach (var clock in loaded.Clocks)
                    {
                        settings.Clocks.Add(clock);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading config file: {ex.Message}", ex);
            }
        }
    }


    private static void CreateConfigFile(string configFilePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        // Create defaults and copy them into the existing in-memory settings instance
        Settings defaults = Settings.CreateDefaults();

        if (settings != null)
        {
            // copy scalar properties (most already defaulted, but ensure consistency)
            settings.ClockSize = defaults.ClockSize;
            settings.ShowFlag = defaults.ShowFlag;
            settings.ShowDayAndFullTime = defaults.ShowDayAndFullTime;
            settings.ShowSecondHand = defaults.ShowSecondHand;
            settings.ShowWorkingHours = defaults.ShowWorkingHours;
            settings.WorkingHoursStart = defaults.WorkingHoursStart;
            settings.WorkingHoursStop = defaults.WorkingHoursStop;
            settings.RunAtStartup = defaults.RunAtStartup;
            settings.ClockFormXPosition = defaults.ClockFormXPosition;
            settings.ClockFormYPosition = defaults.ClockFormYPosition;
            settings.ClockFormWidth = defaults.ClockFormWidth;
            settings.ClockFormHeight = defaults.ClockFormHeight;

            // replace clocks collection contents while keeping the same List instance
            settings.Clocks.Clear();
            foreach (var c in defaults.Clocks)
            {
                settings.Clocks.Add(c);
            }
        }

        // Serialize current in-memory settings (now populated with defaults)
        string jsonContent = JsonSerializer.Serialize(settings ?? defaults, options);

        // Overwrite/create file
        using (var writer = new StreamWriter(configFilePath, false, Encoding.UTF8))
        {
            writer.Write(jsonContent);
        }
    }

    






   

}
