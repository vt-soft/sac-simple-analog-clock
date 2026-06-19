// **************************************************************************************************************************
// *  Project information:                                                                                                  *
// *                                                                                                                        *
// *  Name        :  SAC - simple analog clock                                                                             *
// *  Description :  Analog clocks for your desktop. Useful for people working with colleagues/companies                    *
// *                 from different time zones.                                                                             *
// *  Language    :  C# 14                                                                                                  *
// *  Framework   :  .NET 10.0                                                                                              *
// *  UI          :  WinForms                                                                                               *
// *  NuGet       :  TimeZoneConverter by Matt Johnson-Pint (https://www.nuget.org/packages/TimeZoneConverter/)             *
// *  Icons       :  By Icons8 (https://icons8.com/)                                                                        *
// *  Web         :  https://www.vt-soft.com/sac-simple-analog-clock                                                       *
// *                                                                                                                        *
// *  Please be aware that I am not professional developer so this code is not perfect                                      *
// *  and it is probably not following all best programming practices.                                                      *
// *  However I still hope that you will find it useful and that it will help you to create your own application.           *
// *  For more projects please check https://www.vt-soft.com/                                                               *
// *  Any link to this site is highly appreciated. Enjoy the code! :)                                                       *
// *                                                                                                                        *
// *  Copyright(c) 2026, vt-soft                                                                                            *
// *  All rights reserved.                                                                                                  *
// *                                                                                                                        *
// *  This source code is licensed under the MIT-style license.                                                             *
// *  More info in the license.txt file in the root directory of this source tree.                                          *
// **************************************************************************************************************************


using System;
using System.Threading;
using System.Windows.Forms;

namespace sac;

static class Program
{
    private static Mutex _mutex;

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Create a unique name for your application
        string mutexName = "Global\\SAC-SimpleAnalogClocks-SingleInstance";

        // Try to create a new mutex with that name
        _mutex = new Mutex(true, mutexName, out bool isNewInstance);

        // If another instance already owns the mutex, exit
        if (!isNewInstance)
        {
            MessageBox.Show("SAC - Simple Analog Clock is already running!", "Application Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new ClockForm());
        }
        finally
        {
            // Release the mutex when the application closes
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }
}