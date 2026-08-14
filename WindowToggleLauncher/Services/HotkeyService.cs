using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using WindowToggleLauncher.Interop;

namespace WindowToggleLauncher.Services;
internal enum HotkeyRegistrationStatus
{
    Registered,
    Empty,
    InvalidFormat,
    RegistrationFailed
}

internal readonly record struct HotkeyRegistrationResult(HotkeyRegistrationStatus Status, string? Message = null)
{
    public bool IsRegistered => Status == HotkeyRegistrationStatus.Registered;
}

internal class HotkeyService : IDisposable
{
    private readonly Window _window;
    private readonly Dictionary<int, (uint Modifiers, uint Key, Action Action)> _hotkeys = new();
    private int _nextId = 100;
    private bool _disposed;

    public HotkeyService(Window window)
    {
        _window = window;
        var source = System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(window).Handle);
        source.AddHook(WndProc);
    }

    public HotkeyRegistrationResult RegisterHotkey(string? hotkeyString, Action action)
    {
        if (string.IsNullOrWhiteSpace(hotkeyString))
            return new(HotkeyRegistrationStatus.Empty);

        var parts = hotkeyString.Split('+', StringSplitOptions.TrimEntries);
        uint modifiers = 0;
        uint key = 0;
        var keySpecified = false;

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                return new(HotkeyRegistrationStatus.InvalidFormat, "Use one key, optionally combined with Ctrl, Alt, Shift, or Win.");

            switch (part.ToUpper())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= NativeMethods.MOD_CONTROL;
                    break;
                case "ALT":
                    modifiers |= NativeMethods.MOD_ALT;
                    break;
                case "SHIFT":
                    modifiers |= NativeMethods.MOD_SHIFT;
                    break;
                case "WIN":
                    modifiers |= NativeMethods.MOD_WIN;
                    break;
                default:
                    if (keySpecified)
                        return new(HotkeyRegistrationStatus.InvalidFormat, "A hotkey can contain only one non-modifier key.");

                    if (part.Length == 1 && char.IsLetterOrDigit(part[0]))
                    {
                        key = (uint)char.ToUpperInvariant(part[0]);
                        keySpecified = true;
                    }
                    else if (Enum.TryParse<Key>(part, true, out var keyEnum) &&
                             KeyInterop.VirtualKeyFromKey(keyEnum) != 0)
                    {
                        key = (uint)KeyInterop.VirtualKeyFromKey(keyEnum);
                        keySpecified = true;
                    }
                    else
                    {
                        return new(HotkeyRegistrationStatus.InvalidFormat, $"'{part}' is not a supported key.");
                    }
                    break;
            }
        }

        if (!keySpecified || key == 0)
            return new(HotkeyRegistrationStatus.InvalidFormat, "Specify a key, for example A or Ctrl+Alt+A.");

        var id = _nextId++;
        var handle = new System.Windows.Interop.WindowInteropHelper(_window).Handle;

        if (!NativeMethods.RegisterHotKey(handle, id, modifiers, key))
            return new(HotkeyRegistrationStatus.RegistrationFailed, "Windows could not register it because it is already in use or reserved.");

        _hotkeys[id] = (modifiers, key, action);
        return new(HotkeyRegistrationStatus.Registered);
    }

    public void UnregisterAll()
    {
        var handle = new System.Windows.Interop.WindowInteropHelper(_window).Handle;
        foreach (var id in _hotkeys.Keys)
        {
            NativeMethods.UnregisterHotKey(handle, id);
        }
        _hotkeys.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_hotkeys.TryGetValue(id, out var hotkey))
            {
                hotkey.Action();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        UnregisterAll();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
