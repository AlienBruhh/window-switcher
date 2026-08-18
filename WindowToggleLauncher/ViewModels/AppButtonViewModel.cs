using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using WindowToggleLauncher.Models;
using WindowToggleLauncher.Services;

namespace WindowToggleLauncher.ViewModels;

public class AppButtonViewModel : ObservableObject
{
    private readonly AppButton _app;
    private readonly ToggleService _toggleService;
    private bool _isRunning;
    private bool _isMinimized;

    public AppButtonViewModel(AppButton app, ToggleService toggleService)
    {
        _app = app;
        _toggleService = toggleService;
        ToggleCommand = new RelayCommand(async _ => await ToggleAsync());
    }

    public string Id => _app.Id;
    public string Name => _app.Name;
    public string ExecutablePath => _app.ExecutablePath;
    public string? Arguments => _app.Arguments;
    public string? Hotkey
    {
        get => _app.Hotkey;
        set
        {
            _app.Hotkey = value;
            OnPropertyChanged();
        }
    }
    public bool StartWithWindows
    {
        get => _app.StartWithWindows;
        set
        {
            _app.StartWithWindows = value;
            OnPropertyChanged();
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public bool IsMinimized
    {
        get => _isMinimized;
        set => SetProperty(ref _isMinimized, value);
    }

    private string? _iconBase64;
    public string? IconBase64
    {
        get
        {
            if (_iconBase64 == null && !string.IsNullOrWhiteSpace(_app.ExecutablePath))
            {
                _iconBase64 = ProcessService.GetAppIconBase64(_app.ExecutablePath);
            }
            return _iconBase64;
        }
    }

    public ICommand ToggleCommand { get; }

    public async Task ToggleAsync()
    {
        await _toggleService.ToggleAppAsync(_app);
        UpdateState();
    }

    public void UpdateState()
    {
        IntPtr? hwnd = null;
        try
        {
            var pid = ProcessService.FindProcessIdByPath(_app.ExecutablePath);
            if (pid.HasValue)
            {
                hwnd = WindowService.FindMainWindow(pid.Value);
            }
        }
        catch
        {
        }

        if (hwnd == null)
        {
            IsRunning = false;
            IsMinimized = false;
            return;
        }

        IsRunning = true;
        IsMinimized = WindowService.IsMinimized(hwnd.Value);
    }

    public void UpdateFromEdit(string name, string executablePath, string? arguments)
    {
        _iconBase64 = null;
        _app.Name = name;
        _app.ExecutablePath = executablePath;
        _app.Arguments = arguments;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(ExecutablePath));
        OnPropertyChanged(nameof(Arguments));
        OnPropertyChanged(nameof(IconBase64));
        UpdateState();
    }
}
