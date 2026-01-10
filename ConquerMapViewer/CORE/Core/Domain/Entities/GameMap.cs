namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class GameMap
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public int TileSize { get; set; }
    
    public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(Path);
}
