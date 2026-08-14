using System.Runtime.InteropServices;
using WindowToggleLauncher.Interop;

namespace WindowToggleLauncher.Services;

internal static class WindowService
{
    public static IntPtr? FindMainWindow(uint processId)
    {
        IntPtr? result = null;

        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            if (result.HasValue)
                return false;

            if (!NativeMethods.IsWindowVisible(hWnd))
                return true;

            if (IsToolWindow(hWnd))
                return true;

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint windowPid);
            if (windowPid != processId)
                return true;

            if (NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER) != IntPtr.Zero)
                return true;

            result = hWnd;
            return false;
        }, IntPtr.Zero);

        return result;
    }

    public static bool IsMinimized(IntPtr hWnd)
    {
        return NativeMethods.IsIconic(hWnd);
    }

    public static bool IsForegroundWindow(IntPtr hWnd)
    {
        return NativeMethods.GetForegroundWindow() == hWnd;
    }

    public static void RestoreWindow(IntPtr hWnd)
    {
        try
        {
            if (NativeMethods.IsIconic(hWnd))
            {
                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            }
            else
            {
                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_SHOW);
            }

            NativeMethods.BringWindowToTop(hWnd);
            NativeMethods.SetForegroundWindow(hWnd);
        }
        catch
        {
        }
    }

    public static void MinimizeWindow(IntPtr hWnd)
    {
        try
        {
            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_MINIMIZE);
        }
        catch
        {
        }
    }

    private static bool IsToolWindow(IntPtr hWnd)
    {
        int exStyle = NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE);
        return (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
    }
}
