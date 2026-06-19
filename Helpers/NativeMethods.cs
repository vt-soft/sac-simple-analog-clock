using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace sac.Helpers;

internal static class NativeMethods
{
    // Form transparency and mouse events:
    // These methods are used to set a low-level mouse hook to capture global mouse events.
    // Transparent parts of the form (TransparencyKey) do not receive mouse events,
    // so a low-level mouse hook is a way how to get global mouse move events.

    public delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

    public const int WH_MOUSE_LL = 14;
    public const int WM_MOUSEMOVE = 0x0200;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern nint GetModuleHandle(string lpModuleName);

    public static nint SetHook(LowLevelMouseProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        nint moduleHandle = GetModuleHandle(curModule.ModuleName);
        return SetWindowsHookEx(WH_MOUSE_LL, proc, moduleHandle, 0);
    }
}
