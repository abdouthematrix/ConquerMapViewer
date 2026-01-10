namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class Map3DEffect
{
    public string Effect { get; set; } = string.Empty;
    public MapPoint Location { get; set; }
}
