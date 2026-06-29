namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class MapData
{
    public uint DMapVersion { get; set; }
    public uint DMapData { get; set; }
    public string PuzzlePath { get; set; } = string.Empty;
    public MapSize Bounds { get; set; }
    public MapCellCollection Cells { get; set; } = null!;
    public List<MapPortal> Portals { get; set; } = new();
    public List<MapTerrainObject> TerrainObjects { get; set; } = new();
    public List<MapScene> Scenes { get; set; } = new();
    public List<MapSound> Sounds { get; set; } = new();
    public List<Map3DEffect> Effects { get; set; } = new();
    public List<MapLayer> Layers { get; set; } = new();

    // Terrain section handles (FUN_00d204b7 section A)
    public List<int> TerrainHandles { get; } = new();
}
