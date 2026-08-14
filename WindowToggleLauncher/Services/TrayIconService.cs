using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using WindowToggleLauncher.Models;

namespace WindowToggleLauncher.Services;

internal class TrayIconService : IDisposable
{
    private readonly Window _mainWindow;
    private readonly NotifyIcon _notifyIcon;
    private bool _disposed;

    public TrayIconService(Window mainWindow)
    {
        _mainWindow = mainWindow;
        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Window Toggle Launcher",
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => RestoreWindow();
        _notifyIcon.ContextMenuStrip = CreateContextMenu();
    }

    public void Show()
    {
        if (_mainWindow.Visibility != Visibility.Visible)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
        }
    }

    public void Hide()
    {
        _mainWindow.Hide();
    }

    public void RestoreWindow()
    {
        Show();
        _mainWindow.Activate();
    }

    public void Exit()
    {
        _notifyIcon.Visible = false;
        _mainWindow.Close();
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        
        var restoreItem = new ToolStripMenuItem("Restore");
        restoreItem.Click += (s, e) => RestoreWindow();
        menu.Items.Add(restoreItem);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => Exit();
        menu.Items.Add(exitItem);

        return menu;
    }

    private Icon LoadIcon()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("WindowToggleLauncher.Resources.trayicon.ico");
            if (stream != null)
                return new Icon(stream);
        }
        catch
        {
        }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
