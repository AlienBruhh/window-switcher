using System.IO;
using System.Windows;
using WindowToggleLauncher.Services;
using WindowToggleLauncher.ViewModels;
using WindowToggleLauncher.Views;

namespace WindowToggleLauncher;

public partial class App : Application
{
    private const string InstanceMutexName = "WindowToggleLauncher.SingleInstance";
    private const string ActivationEventName = "WindowToggleLauncher.Activate";
    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _activationEvent;
    private bool _isFirstInstance;

    private MainViewModel? _mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, InstanceMutexName, out var isFirstInstance);
        _isFirstInstance = isFirstInstance;
        if (!_isFirstInstance)
        {
            using var activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
            activationEvent.Set();
            Shutdown();
            return;
        }

        var configService = new ConfigurationService();
        _mainViewModel = new MainViewModel(configService, Dispatcher);
        var mainWindow = new MainWindow { DataContext = _mainViewModel };
        mainWindow.Loaded += (s, args) => _mainViewModel.InitializeHotkeys();
        MainWindow = mainWindow;
        mainWindow.InitializeTrayIcon();
        mainWindow.Show();

        _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        _ = Task.Run(WaitForActivation);
    }

    private void WaitForActivation()
    {
        while (_activationEvent?.WaitOne() == true)
        {
            try
            {
                Dispatcher.BeginInvoke(() =>
                {
                    if (MainWindow is Window mainWindow)
                    {
                        mainWindow.Show();
                        mainWindow.WindowState = WindowState.Normal;
                        mainWindow.Activate();
                        mainWindow.Topmost = true;
                        mainWindow.Topmost = false;
                        mainWindow.Focus();
                    }
                });
            }
            catch
            {
                return;
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _mainViewModel?.StopServerAsync().GetAwaiter().GetResult();
        }
        catch
        {
        }

        _activationEvent?.Set();
        _activationEvent?.Dispose();
        if (_isFirstInstance)
            _singleInstanceMutex?.ReleaseMutex();

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}