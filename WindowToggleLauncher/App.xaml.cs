using System.IO;
using System.Windows;
using WindowToggleLauncher.Services;
using WindowToggleLauncher.ViewModels;
using WindowToggleLauncher.Views;

namespace WindowToggleLauncher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configService = new ConfigurationService();
        var mainViewModel = new MainViewModel(configService, Dispatcher);
        var mainWindow = new MainWindow { DataContext = mainViewModel };
        mainWindow.Loaded += (s, args) => mainViewModel.InitializeHotkeys();
        MainWindow = mainWindow;
        mainWindow.InitializeTrayIcon();
        mainWindow.Show();
    }
}
