namespace ConquerMapViewer.Core.Domain.Entities;

public class MapLayer
{
    public int Index;
    public int Type;
    public int RateX;
    public int RateY;
    public List<MapBackdrop> Backdrops;

    public int Alpha { get; set; }
    public int ColorR { get; set; }
    public int Light { get; set; }
    public int ColorG { get; set; }
    public int PuzzleAlpha { get; set; }
    public int PuzzleColorR { get; set; }
    public int PuzzleColorB { get; set; }
    public int ColorB { get; set; }
    public int PuzzleLight { get; set; }
    public int PuzzleColorG { get; set; }
    public int NewA { get; set; }
    public int NewB { get; set; }
    public int NewC { get; set; }
}
