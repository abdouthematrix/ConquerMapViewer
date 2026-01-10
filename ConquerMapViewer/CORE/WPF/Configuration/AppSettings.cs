using System.IO;

namespace ConquerMapViewer.WPF.Configuration;

public sealed class AppSettings
{
    public string ConquerDirectory { get; set; } = string.Empty;
    public string GameMapFilePath { get; set; } = string.Empty;
    public int DefaultMapId { get; set; } = 1006;
    public float DefaultZoom { get; set; } = 0.5f;
    public bool LoadLastMap { get; set; } = true;
    public string LastMapPath { get; set; } = string.Empty;
    public bool IsFirstRun { get; set; } = true; // NEW

    public static AppSettings LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return CreateDefault();

        try
        {
            var json = File.ReadAllText(filePath);
            return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefault();
        }
        catch
        {
            return CreateDefault();
        }
    }

    public void SaveToFile(string filePath)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Log error
        }
    }

    private static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            ConquerDirectory = string.Empty,
            GameMapFilePath = string.Empty,
            IsFirstRun = true
        };
    }

    public bool IsValidConfiguration()
    {
        return !string.IsNullOrEmpty(ConquerDirectory) &&
               Directory.Exists(ConquerDirectory) &&
               !string.IsNullOrEmpty(GameMapFilePath) &&
               File.Exists(GameMapFilePath);
    }
}
