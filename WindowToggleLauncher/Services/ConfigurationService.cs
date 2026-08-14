using System.IO;
using System.Text.Json;
using WindowToggleLauncher.Models;

namespace WindowToggleLauncher.Services;

internal class ConfigurationService
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowToggleLauncher");

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

    public AppConfiguration Load()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
                return new AppConfiguration();

            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<AppConfiguration>(json);
            return config ?? new AppConfiguration();
        }
        catch
        {
            return new AppConfiguration();
        }
    }

    public void Save(AppConfiguration configuration)
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch
        {
        }
    }
}
