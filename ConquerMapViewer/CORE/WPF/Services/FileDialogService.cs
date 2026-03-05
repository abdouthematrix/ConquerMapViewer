namespace ConquerMapViewer.WPF.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? OpenFolder(string title = "Select Folder")
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = title,
            ShowNewFolderButton = true
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    public string? OpenFile(string title = "Open File", string filter = "All Files (*.*)|*.*")
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = filter
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveFile(string title = "Save File", string filter = "All Files (*.*)|*.*", string defaultFileName = "")
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = defaultFileName
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
