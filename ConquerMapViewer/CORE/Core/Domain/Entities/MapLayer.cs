namespace ConquerMapViewer.Core.Domain.Entities;

public struct MapLayer
{
    public int index;
    public int layertype;
    public int xInt;
    public int yInt;
    public List<MapBackdrop> Backdrops;
    public List<MapTerrainObject> TerrainObjects;
    

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
