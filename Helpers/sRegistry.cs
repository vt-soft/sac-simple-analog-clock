using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace sac.Helpers;

public static class sRegistry
{



    /// <summary>
    /// Method to add or remove the application from the Windows startup registry key based on the user's preference.
    /// </summary>
    /// <param name="runAtStartup"></param>
    public static void UpdateWindowsRegistry(bool runAtStartup)
    {
        // runAtStartup = true means the user wants the application to run at startup, false means they don't want it to run at startup.

        // Define the registry key path for the "Run" section in the current user's registry hive
        string runKey = System.IO.Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Run");
        string appName = "SAC_vt-soft";

        using (RegistryKey ?startupKey = Registry.CurrentUser.OpenSubKey(runKey, true))
        {

            var currentValue = startupKey?.GetValue(appName)?.ToString();
            string expectedValue = "\"" + Application.ExecutablePath + "\"";

            if (startupKey == null)
            {
                // Key doesn't exist, cannot proceed
                MessageBox.Show("Unable to access the Windows registry. The application will not be added to startup.", "Registry Access Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (runAtStartup == true)
            {
                // If the registry entry ("Value Name" in regedit.exe) exists but the app path ("Value data" in regedit.exe)
                // is incorrect or outdated then delete the registry entry. 
                if (currentValue != null && currentValue != expectedValue)
                {
                    startupKey.DeleteValue(appName, false);
                }

                // If the registry entry (Value Name)  does not exist, create it and set the application path as its Value data.
                if (startupKey.GetValue(appName) == null)
                {
                    startupKey.SetValue(appName, expectedValue);
                }
            }

            else // If you don't want the application to run at startup, delete the registry entry (Value Name)
            {
                startupKey.DeleteValue(appName, false);
            }
        }
    }


}
