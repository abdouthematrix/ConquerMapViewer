namespace ConquerMapViewer.WPF.Services;

public interface IFileDialogService
{
    string? OpenFolder(string title = "Select Folder");
    string? OpenFile(string title = "Open File", string filter = "All Files (*.*)|*.*");
    string? SaveFile(string title = "Save File", string filter = "All Files (*.*)|*.*", string defaultFileName = "");
}
