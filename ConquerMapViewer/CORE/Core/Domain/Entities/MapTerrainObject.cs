namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class MapTerrainObject
{
    public string AniPath { get; set; } = string.Empty;
    public string AniName { get; set; } = string.Empty;
    public MapPoint Location { get; set; }
    public MapSize Size { get; set; }
    public MapPoint ImageOffset { get; set; }
    public int Interval { get; set; }
    /// <summary>_new format only — 16-bit (local_1a4 &amp; 0xFFFF).</summary>
    public ushort ShowWay { get; set; }

    public int PicWidth { get; set; }
    public int PicHeight { get; set; }
    public bool Interactive { get; set; }

}
