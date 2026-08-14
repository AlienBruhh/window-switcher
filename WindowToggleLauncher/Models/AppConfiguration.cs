namespace WindowToggleLauncher.Models;

public class AppConfiguration
{
    public List<AppButton> Apps { get; set; } = new();
    public int RefreshIntervalSeconds { get; set; } = 3;
}
