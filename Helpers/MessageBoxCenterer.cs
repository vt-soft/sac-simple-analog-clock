using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace sac.Helpers;

/// <summary>
/// Centers the next dialog (e.g., MessageBox) shown on the current thread over a given owner window.
/// Usage: using (new MessageBoxCenterer(owner)) { MessageBox.Show(owner, ...); }
/// </summary>
public sealed class MessageBoxCenterer : IDisposable
{
    private readonly IntPtr _owner;
    private IntPtr _hHook = IntPtr.Zero;
    private HookProc _hookProc;

    public MessageBoxCenterer(IWin32Window owner)
    {
        _owner = owner.Handle;
        _hookProc = HookFunc;
        _hHook = SetWindowsHookEx(WH_CBT, _hookProc, IntPtr.Zero, GetCurrentThreadId());
    }

    public void Dispose()
    {
        if (_hHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hHook);
            _hHook = IntPtr.Zero;
        }
    }

    private IntPtr HookFunc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode == HCBT_ACTIVATE)
        {
            if (GetWindowRect(_owner, out RECT rcOwner) && GetWindowRect(wParam, out RECT rcDlg))
            {
                int ownerW = rcOwner.Right - rcOwner.Left;
                int ownerH = rcOwner.Bottom - rcOwner.Top;
                int dlgW = rcDlg.Right - rcDlg.Left;
                int dlgH = rcDlg.Bottom - rcDlg.Top;
                int x = rcOwner.Left + (ownerW - dlgW) / 2;
                int y = rcOwner.Top + (ownerH - dlgH) / 2;
                MoveWindow(wParam, x, y, dlgW, dlgH, false);
            }
            UnhookWindowsHookEx(_hHook);
            _hHook = IntPtr.Zero;
        }
        return CallNextHookEx(_hHook, nCode, wParam, lParam);
    }

    private const int WH_CBT = 5;
    private const int HCBT_ACTIVATE = 5;

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
