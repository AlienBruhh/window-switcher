using System.IO;
using System.Windows.Threading;
using WindowToggleLauncher.Models;
using WindowToggleLauncher.Services;

namespace WindowToggleLauncher.Services;

public class ToggleService
{
    private readonly Dispatcher _dispatcher;

    public ToggleService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<bool> ToggleAppAsync(AppButton app, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(app.ExecutablePath) || !File.Exists(app.ExecutablePath))
            return false;

        uint? pid = ProcessService.FindProcessIdByPath(app.ExecutablePath);
        IntPtr? hwnd = null;

        if (pid.HasValue)
        {
            hwnd = WindowService.FindMainWindow(pid.Value);
        }

        if (hwnd == null)
        {
            pid = ProcessService.LaunchProcess(app.ExecutablePath, app.Arguments);
            if (pid == null)
                return false;

            await Task.Delay(1000, cancellationToken);

            hwnd = WindowService.FindMainWindow(pid.Value);
            if (hwnd == null)
                return true;

            WindowService.RestoreWindow(hwnd.Value);
            return true;
        }

        if (WindowService.IsMinimized(hwnd.Value))
        {
            await _dispatcher.InvokeAsync(() => WindowService.RestoreWindow(hwnd.Value), DispatcherPriority.Normal, cancellationToken);
            return true;
        }

        if (WindowService.IsForegroundWindow(hwnd.Value))
        {
            await _dispatcher.InvokeAsync(() => WindowService.MinimizeWindow(hwnd.Value), DispatcherPriority.Normal, cancellationToken);
            return true;
        }

        await _dispatcher.InvokeAsync(() => WindowService.RestoreWindow(hwnd.Value), DispatcherPriority.Normal, cancellationToken);
        return true;
    }
}
