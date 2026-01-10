namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class MapSound
{
    public string SoundPath { get; set; } = string.Empty;
    public MapPoint Location { get; set; }
    public int Volume { get; set; }
    public int Range { get; set; }
}
