namespace ConquerMapViewer.Core.Domain.Entities;

public class MapLayer
{
    public int Index { get; set; }
    public int Type { get; set; }
    public int RateX { get; set; }
    public int RateY { get; set; }
    public List<MapBackdrop> Backdrops { get; set; }
    public List<MapTerrainObject> TerrainObjects { get; set; }
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
