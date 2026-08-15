using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WindowToggleLauncher.ViewModels;

namespace WindowToggleLauncher.Views;

public partial class AppConfigurationWindow : Window
{
    private bool _isCapturingHotkey;
    public bool DeleteRequested { get; private set; }

    public AppConfigurationWindow(AppButtonViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        NameTextBox.Text = viewModel.Name;
        PathTextBox.Text = viewModel.ExecutablePath;
        ArgumentsTextBox.Text = viewModel.Arguments ?? string.Empty;
        HotkeyTextBox.Text = viewModel.Hotkey ?? string.Empty;
        StartWithWindowsCheckBox.IsChecked = viewModel.StartWithWindows;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select Application"
        };

        if (dialog.ShowDialog() == true)
        {
            PathTextBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                NameTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    private void RegisterHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        _isCapturingHotkey = true;
        RegisterHotkeyButton.Content = "Press key";
        HotkeyCaptureStatusTextBlock.Text = "Press a key combination. Press Esc to cancel.";
        HotkeyTextBox.Focus();
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isCapturingHotkey)
            return;

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Escape)
        {
            StopHotkeyCapture("Hotkey capture cancelled.");
            return;
        }

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var parts = new List<string>();
        var modifiers = Keyboard.Modifiers;
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());

        HotkeyTextBox.Text = string.Join("+", parts);
        StopHotkeyCapture("Hotkey captured. Click Save to apply it.");
    }

    private void StopHotkeyCapture(string message)
    {
        _isCapturingHotkey = false;
        RegisterHotkeyButton.Content = "Register";
        HotkeyCaptureStatusTextBlock.Text = message;
    }
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppButtonViewModel vm)
        {
            vm.UpdateFromEdit(NameTextBox.Text, PathTextBox.Text, ArgumentsTextBox.Text);
            vm.Hotkey = string.IsNullOrWhiteSpace(HotkeyTextBox.Text) ? null : HotkeyTextBox.Text.Trim();
            vm.StartWithWindows = StartWithWindowsCheckBox.IsChecked == true;
        }

        DeleteRequested = false;
        DialogResult = true;
        Close();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppButtonViewModel vm)
        {
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to remove '{vm.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteRequested = true;
                DialogResult = true;
                Close();
            }
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteRequested = false;
        DialogResult = false;
        Close();
    }
}
