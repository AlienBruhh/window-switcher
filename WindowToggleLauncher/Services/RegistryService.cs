using Microsoft.Win32;

namespace WindowToggleLauncher.Services;

internal static class RegistryService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WindowToggleLauncher";

    public static void SetStartWithWindows(string id, string executablePath, bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null)
                return;

            var valueName = $"{AppName}_{id}";

            if (enabled)
            {
                key.SetValue(valueName, $"\"{executablePath}\"");
            }
            else
            {
                key.DeleteValue(valueName, throwOnMissingValue: false);
            }
        }
        catch
        {
        }
    }
}
