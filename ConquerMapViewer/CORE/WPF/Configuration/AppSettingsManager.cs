namespace ConquerMapViewer.WPF.Configuration;

public sealed class AppSettingsManager
{
    private readonly string _settingsPath;
    private AppSettings _settings;

    public AppSettings Settings => _settings;

    public AppSettingsManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "ConquerMapViewer");
        Directory.CreateDirectory(appFolder);

        _settingsPath = Path.Combine(appFolder, "settings.json");
        _settings = AppSettings.LoadFromFile(_settingsPath);
    }

    public void Save()
    {
        _settings.SaveToFile(_settingsPath);
    }

    public void UpdateLastMap(string mapPath)
    {
        _settings.LastMapPath = mapPath;
        Save();
    }

    public void UpdateConquerDirectory(string directory)
    {
        _settings.ConquerDirectory = directory;
        _settings.GameMapFilePath = Path.Combine(directory, "ini", "gamemap.dat");
        _settings.IsFirstRun = false;
        Save();
    }

    public void CompleteFirstRun()
    {
        _settings.IsFirstRun = false;
        Save();
    }
}
