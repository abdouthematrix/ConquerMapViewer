namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class MapTerrainObject
{
    public string AniPath { get; set; } = string.Empty;
    public string AniName { get; set; } = string.Empty;
    public MapPoint Location { get; set; }
    public MapSize Size { get; set; }
    public MapPoint ImageOffset { get; set; }
    public int Interval { get; set; }
}
