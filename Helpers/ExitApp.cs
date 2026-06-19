using System;
using System.Windows.Forms;

namespace sac.Helpers;

public static class ExitApp
{
    public static void Exit()
    {
        try
        {
            // Try to perform a graceful UI-thread shutdown if a WinForms message loop exists.
            if (Application.OpenForms.Count > 0)
            {
                var form = Application.OpenForms[0];
                if (form != null && form.IsHandleCreated && !form.IsDisposed)
                {
                    try
                    {
                        // If we're not on the UI thread for the form, invoke and wait so exit happens immediately.
                        if (form.InvokeRequired)
                        {
                            form.Invoke((Action)(() =>
                            {
                                try { Application.Exit(); } catch { }
                            }));
                        }
                        else
                        {
                            Application.Exit();
                        }
                    }
                    catch
                    {
                        // Swallow exceptions during shutdown attempt and fall back to hard exit below.
                    }

                    return;
                }
            }
        }
        catch
        {
            // Ignore any problems inspecting OpenForms and fall through to forced exit.
        }

        // No UI loop available or graceful exit failed — force process termination (0 = success).
        Environment.Exit(0);
    }
}
