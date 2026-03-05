namespace ConquerMapViewer.WPF.Views;

public partial class WelcomeDialog : Window
{
    public string? SelectedDirectory { get; private set; }

    public WelcomeDialog()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select your Conquer Online installation directory",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DirectoryPathTextBox.Text = dialog.SelectedPath;
            ValidateDirectory(dialog.SelectedPath);
        }
    }

    private void ValidateDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            StatusTextBlock.Text = "Please select a directory";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            OkButton.IsEnabled = false;
            return;
        }

        if (!Directory.Exists(path))
        {
            StatusTextBlock.Text = "❌ Directory does not exist";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            OkButton.IsEnabled = false;
            return;
        }

        var gameMapPath = Path.Combine(path, "ini", "gamemap.dat");
        if (!File.Exists(gameMapPath))
        {
            StatusTextBlock.Text = "❌ Invalid Conquer directory (gamemap.dat not found)";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            OkButton.IsEnabled = false;
            return;
        }

        StatusTextBlock.Text = "✅ Valid Conquer Online directory detected";
        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
        OkButton.IsEnabled = true;
        SelectedDirectory = path;
    }

    private void DirectoryPathTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ValidateDirectory(DirectoryPathTextBox.Text);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TryCommonLocations_Click(object sender, RoutedEventArgs e)
    {
        var commonLocations = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Conquer Online"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Conquer Online"),
            @"C:\Games\Conquer Online",
            @"D:\Games\Conquer Online",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "CO"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "CO", "6090")
        };

        foreach (var location in commonLocations)
        {
            if (Directory.Exists(location))
            {
                var gameMapPath = Path.Combine(location, "ini", "gamemap.dat");
                if (File.Exists(gameMapPath))
                {
                    DirectoryPathTextBox.Text = location;
                    return;
                }
            }
        }

        System.Windows.MessageBox.Show(
            "Could not find Conquer Online in common locations. Please browse manually.",
            "Not Found",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }
}
