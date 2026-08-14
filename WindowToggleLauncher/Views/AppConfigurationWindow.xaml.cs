using System.Windows;
using Microsoft.Win32;
using WindowToggleLauncher.ViewModels;

namespace WindowToggleLauncher.Views;

public partial class AppConfigurationWindow : Window
{
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
