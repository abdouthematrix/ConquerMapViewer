namespace ConquerMapViewer.WPF.Services;

public sealed class DialogService : IDialogService
{
    public void ShowError(string message, string title = "Error")
    {
        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowInfo(string message, string title = "Information")
    {
        System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public bool ShowConfirmation(string message, string title = "Confirm")
    {
        var result = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public string? ShowInputDialog(string message, string title = "Input", string defaultValue = "")
    {
        var dialog = new InputDialog(message, title, defaultValue);
        return dialog.ShowDialog() == true ? dialog.InputText : null;
    }
}
