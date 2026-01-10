namespace ConquerMapViewer.WPF.Services;

public interface IDialogService
{
    void ShowError(string message, string title = "Error");
    void ShowWarning(string message, string title = "Warning");
    void ShowInfo(string message, string title = "Information");
    bool ShowConfirmation(string message, string title = "Confirm");
    string? ShowInputDialog(string message, string title = "Input", string defaultValue = "");
}
