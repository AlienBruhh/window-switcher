namespace WindowToggleLauncher.Models;

public class AppButton
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? Hotkey { get; set; }
    public string? IconPath { get; set; }
    public bool StartWithWindows { get; set; }
}
