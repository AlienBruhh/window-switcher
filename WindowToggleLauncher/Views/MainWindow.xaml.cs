using System.Windows;
using WindowToggleLauncher.Services;
using WindowToggleLauncher.ViewModels;

namespace WindowToggleLauncher.Views;

public partial class MainWindow : Window
{
    private TrayIconService? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void InitializeTrayIcon()
    {
        if (DataContext is MainViewModel viewModel)
        {
            _trayIcon = new TrayIconService(this);
            viewModel.SetTrayIcon(_trayIcon);
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_trayIcon?.IsExiting == true)
        {
            _trayIcon.Dispose();
            return;
        }

        e.Cancel = true;
        _trayIcon?.Hide();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        
        if (WindowState == WindowState.Minimized)
        {
            _trayIcon?.Hide();
        }
    }
}
