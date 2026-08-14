using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using WindowToggleLauncher.Models;
using WindowToggleLauncher.Services;

namespace WindowToggleLauncher.ViewModels;

internal class MainViewModel : ObservableObject
{
    private readonly ConfigurationService _configService;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _refreshTimer;
    private HotkeyService? _hotkeyService;
    private TrayIconService? _trayIcon;

    public ObservableCollection<AppButtonViewModel> Apps { get; } = new();

    public ICommand AddAppCommand { get; }
    public ICommand RemoveAppCommand { get; }
    public ICommand EditAppCommand { get; }
    public ICommand ToggleAppCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand ShowCommand { get; }

    public MainViewModel(ConfigurationService configService, Dispatcher dispatcher)
    {
        _configService = configService;
        _dispatcher = dispatcher;

        AddAppCommand = new RelayCommand(_ => AddApp());
        RemoveAppCommand = new RelayCommand(param => RemoveApp(param), param => param is AppButtonViewModel);
        EditAppCommand = new RelayCommand(param => EditApp(param), param => param is AppButtonViewModel);
        ToggleAppCommand = new RelayCommand(param => ToggleApp(param), param => param is AppButtonViewModel);
        ExitCommand = new RelayCommand(_ => Exit());
        ShowCommand = new RelayCommand(_ => Show());

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _refreshTimer.Tick += (s, e) => RefreshState();
        _refreshTimer.Start();

        LoadConfiguration();
    }

    public void InitializeHotkeys()
    {
        _hotkeyService = new HotkeyService(Application.Current.MainWindow);
        RebuildHotkeys();
    }

    private void RebuildHotkeys()
    {
        if (_hotkeyService == null)
            return;

        _hotkeyService.UnregisterAll();
        var failures = new List<string>();

        foreach (var app in Apps)
        {
            if (!string.IsNullOrWhiteSpace(app.Hotkey))
            {
                var result = _hotkeyService.RegisterHotkey(app.Hotkey, () => { _ = app.ToggleAsync(); });
                if (!result.IsRegistered)
                {
                    failures.Add($"{app.Name} ({app.Hotkey}): {result.Message}");
                }
            }
        }

        if (failures.Count > 0)
        {
            MessageBox.Show(
                string.Join(Environment.NewLine, failures),
                "Some hotkeys could not be registered",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    public void SetTrayIcon(TrayIconService trayIcon)
    {
        _trayIcon = trayIcon;
    }

    private void LoadConfiguration()
    {
        var config = _configService.Load();
        Apps.Clear();

        foreach (var app in config.Apps)
        {
            var vm = new AppButtonViewModel(app, new ToggleService(_dispatcher));
            vm.UpdateState();
            Apps.Add(vm);
        }

        _refreshTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, config.RefreshIntervalSeconds));
    }

    private void SaveConfiguration()
    {
        var config = new AppConfiguration
        {
            Apps = Apps.Select(vm => new AppButton
            {
                Id = vm.Id,
                Name = vm.Name,
                ExecutablePath = vm.ExecutablePath,
                Arguments = vm.Arguments,
                Hotkey = vm.Hotkey,
                StartWithWindows = vm.StartWithWindows
            }).ToList(),
            RefreshIntervalSeconds = (int)_refreshTimer.Interval.TotalSeconds
        };

        _configService.Save(config);
    }

    private void AddApp()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select Application"
        };

        if (dialog.ShowDialog() == true)
        {
            var app = new AppButton
            {
                Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                ExecutablePath = dialog.FileName
            };

            var vm = new AppButtonViewModel(app, new ToggleService(_dispatcher));
            Apps.Add(vm);
            SaveConfiguration();
            RebuildHotkeys();
        }
    }

    private void RemoveApp(object? parameter)
    {
        if (parameter is AppButtonViewModel vm)
        {
            RegistryService.SetStartWithWindows(vm.Id, vm.ExecutablePath, false);
            Apps.Remove(vm);
            SaveConfiguration();
            RebuildHotkeys();
        }
    }

    private void EditApp(object? parameter)
    {
        if (parameter is not AppButtonViewModel vm)
            return;

        var window = new Views.AppConfigurationWindow(vm)
        {
            Owner = Application.Current.MainWindow
        };

        if (window.ShowDialog() == true)
        {
            if (window.DeleteRequested)
            {
                RegistryService.SetStartWithWindows(vm.Id, vm.ExecutablePath, false);
                Apps.Remove(vm);
            }
            else
            {
                vm.UpdateState();
            }
            SaveConfiguration();
            RebuildHotkeys();
        }
    }

    private async void ToggleApp(object? parameter)
    {
        if (parameter is AppButtonViewModel vm)
        {
            await vm.ToggleAsync();
            SaveConfiguration();
        }
    }

    private void RefreshState()
    {
        foreach (var app in Apps)
        {
            app.UpdateState();
        }
    }

    private void Show()
    {
        Application.Current.MainWindow.Show();
        Application.Current.MainWindow.WindowState = WindowState.Normal;
        Application.Current.MainWindow.Activate();
    }

    private void Exit()
    {
        _refreshTimer.Stop();
        _hotkeyService?.Dispose();
        Application.Current.Shutdown();
    }
}
