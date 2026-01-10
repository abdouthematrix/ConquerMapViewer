namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class MapScene
{
    public string ScenePath { get; set; } = string.Empty;
    public MapPoint Location { get; set; }
}
