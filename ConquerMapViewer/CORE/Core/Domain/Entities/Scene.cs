namespace ConquerMapViewer.Core.Domain.Entities;

public class Scene
{
    public List<ScenePart> SceneParts { get; set; } = new();
}

public class ScenePart
{
    public string AniPath { get; set; } = string.Empty;
    public string AniName { get; set; } = string.Empty;
    public MapPoint Location { get; set; }
    public int Interval { get; set; }
    public MapSize Size { get; set; }
    public int Thick { get; set; }
    public MapPoint ImageOffset { get; set; }
    public int Height { get; set; }
    public MapCell[,] Cells { get; set; } = new MapCell[0, 0];
}