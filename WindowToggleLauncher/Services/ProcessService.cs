using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace WindowToggleLauncher.Services;

internal static class ProcessService
{
    public static uint? LaunchProcess(string executablePath, string? arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return null;

            process.WaitForInputIdle(5000);

            return (uint)process.Id;
        }
        catch
        {
            return null;
        }
    }

    public static uint? FindProcessIdByPath(string executablePath)
    {
        try
        {
            var exeName = Path.GetFileName(executablePath);
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeName));
            
            foreach (var process in processes)
            {
                try
                {
                    if (string.Equals(process.MainModule?.FileName, executablePath, StringComparison.OrdinalIgnoreCase))
                        return (uint)process.Id;
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
        return null;
    }

    public static bool IsProcessRunning(uint processId)
    {
        try
        {
            return Process.GetProcessById((int)processId) is not null;
        }
        catch
        {
            return false;
        }
    }

    public static void KillProcess(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            if (!process.HasExited)
                process.Kill();
        }
        catch
        {
        }
    }

    public static string? GetAppIconBase64(string executablePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                return null;

            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
            if (icon == null)
                return null;

            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
        }
        catch
        {
            return null;
        }
    }
}
