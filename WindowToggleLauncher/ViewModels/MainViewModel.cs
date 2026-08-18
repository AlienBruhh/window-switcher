using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
    private readonly RemoteAuthService _authService;
    private readonly RemoteServerService _remoteServerService;
    private HotkeyService? _hotkeyService;
    private TrayIconService? _trayIcon;

    private BitmapSource? _qrCodeImage;
    private string _connectionUrl = string.Empty;
    private string _localIp = "127.0.0.1";
    private int _serverPort = 8765;
    private string _pairingStatus = "Waiting for phone...";
    private int _connectedDeviceCount;
    private bool _isServerRunning;

    public ObservableCollection<AppButtonViewModel> Apps { get; } = new();

    public BitmapSource? QrCodeImage
    {
        get => _qrCodeImage;
        private set => SetProperty(ref _qrCodeImage, value);
    }

    public string ConnectionUrl
    {
        get => _connectionUrl;
        private set => SetProperty(ref _connectionUrl, value);
    }

    public string LocalIp
    {
        get => _localIp;
        private set => SetProperty(ref _localIp, value);
    }

    public int ServerPort
    {
        get => _serverPort;
        private set => SetProperty(ref _serverPort, value);
    }

    public string PairingStatus
    {
        get => _pairingStatus;
        private set => SetProperty(ref _pairingStatus, value);
    }

    public int ConnectedDeviceCount
    {
        get => _connectedDeviceCount;
        private set
        {
            if (SetProperty(ref _connectedDeviceCount, value))
            {
                OnPropertyChanged(nameof(IsPhoneConnected));
                UpdatePairingStatusText();
            }
        }
    }

    public bool IsPhoneConnected => ConnectedDeviceCount > 0;

    public bool IsServerRunning
    {
        get => _isServerRunning;
        private set => SetProperty(ref _isServerRunning, value);
    }

    public ICommand AddAppCommand { get; }
    public ICommand RemoveAppCommand { get; }
    public ICommand EditAppCommand { get; }
    public ICommand ToggleAppCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand ShowCommand { get; }
    public ICommand RegenerateQrCommand { get; }
    public ICommand DisconnectPhoneCommand { get; }
    public ICommand CopyConnectionUrlCommand { get; }
    public ICommand OpenWebRemoteCommand { get; }

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
        RegenerateQrCommand = new RelayCommand(_ => RegenerateQr());
        DisconnectPhoneCommand = new RelayCommand(async _ => await DisconnectAllPhonesAsync());
        CopyConnectionUrlCommand = new RelayCommand(_ => CopyConnectionUrl());
        OpenWebRemoteCommand = new RelayCommand(_ => OpenWebRemoteInBrowser());

        _authService = new RemoteAuthService();
        _authService.SessionsChanged += () =>
        {
            _dispatcher.BeginInvoke(() =>
            {
                UpdatePairingStatusText();
            });
        };

        _remoteServerService = new RemoteServerService(
            _authService,
            GetAppDtos,
            ToggleAppByIdAsync
        );
        _remoteServerService.ClientCountChanged += () =>
        {
            _dispatcher.BeginInvoke(() =>
            {
                ConnectedDeviceCount = _remoteServerService.ConnectedClientCount;
            });
        };

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _refreshTimer.Tick += (s, e) => RefreshState();
        _refreshTimer.Start();

        LoadConfiguration();
        _ = InitializeServerAsync();
    }

    private async Task InitializeServerAsync()
    {
        try
        {
            LocalIp = NetworkService.GetLocalIpAddress();
            ServerPort = NetworkService.FindAvailablePort(8765);

            await _remoteServerService.StartAsync(ServerPort);
            IsServerRunning = _remoteServerService.IsRunning;

            UpdateQrCode();
        }
        catch (Exception ex)
        {
            PairingStatus = $"Server Error: {ex.Message}";
        }
    }

    private void UpdateQrCode()
    {
        var token = _authService.CurrentPairingToken;
        ConnectionUrl = $"http://{LocalIp}:{ServerPort}/connect?token={token}";

        try
        {
            QrCodeImage = QrCodeService.GenerateQrCodeImage(ConnectionUrl, 8);
        }
        catch
        {
            QrCodeImage = null;
        }

        UpdatePairingStatusText();
    }

    private void RegenerateQr()
    {
        _authService.GenerateNewPairingToken();
        UpdateQrCode();
    }

    private async Task DisconnectAllPhonesAsync()
    {
        _authService.RevokeAllSessions();
        await _remoteServerService.BroadcastUnpairAsync();
        RegenerateQr();
    }

    private void CopyConnectionUrl()
    {
        if (!string.IsNullOrEmpty(ConnectionUrl))
        {
            try
            {
                Clipboard.SetText(ConnectionUrl);
                MessageBox.Show("Connection URL copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
            }
        }
    }

    private void OpenWebRemoteInBrowser()
    {
        if (!string.IsNullOrEmpty(ConnectionUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ConnectionUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }
    }

    private void UpdatePairingStatusText()
    {
        if (_remoteServerService.ConnectedClientCount > 0)
        {
            var count = _remoteServerService.ConnectedClientCount;
            PairingStatus = count == 1 ? "Phone connected (1 device)" : $"Phones connected ({count} devices)";
        }
        else if (_authService.ActiveSessionCount > 0)
        {
            PairingStatus = "Paired (Waiting for active connection...)";
        }
        else
        {
            PairingStatus = "Waiting for phone...";
        }
    }

    public List<AppDto> GetAppDtos()
    {
        return Apps.Select(vm => new AppDto
        {
            Id = vm.Id,
            Name = vm.Name,
            Hotkey = vm.Hotkey,
            IconBase64 = vm.IconBase64,
            IsRunning = vm.IsRunning,
            IsMinimized = vm.IsMinimized
        }).ToList();
    }

    public async Task<bool> ToggleAppByIdAsync(string appId)
    {
        return await _dispatcher.InvokeAsync(async () =>
        {
            var app = Apps.FirstOrDefault(a => 
                string.Equals(a.Id, appId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Name, appId, StringComparison.OrdinalIgnoreCase));

            if (app == null)
                return false;

            await app.ToggleAsync();
            BroadcastCurrentState();
            return true;
        }).Task.Unwrap();
    }

    private void BroadcastCurrentState()
    {
        if (_remoteServerService.IsRunning && _remoteServerService.ConnectedClientCount > 0)
        {
            _ = _remoteServerService.BroadcastAppsAsync(GetAppDtos());
        }
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
                var result = _hotkeyService.RegisterHotkey(app.Hotkey, () =>
                {
                    _ = app.ToggleAsync().ContinueWith(_ =>
                    {
                        _dispatcher.BeginInvoke(BroadcastCurrentState);
                    });
                });
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
        BroadcastCurrentState();
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
        BroadcastCurrentState();
    }

    private void Show()
    {
        Application.Current.MainWindow.Show();
        Application.Current.MainWindow.WindowState = WindowState.Normal;
        Application.Current.MainWindow.Activate();
    }

    public async Task StopServerAsync()
    {
        await _remoteServerService.StopAsync();
    }

    private void Exit()
    {
        _refreshTimer.Stop();
        _ = _remoteServerService.StopAsync();
        _hotkeyService?.Dispose();
        Application.Current.Shutdown();
    }
}
