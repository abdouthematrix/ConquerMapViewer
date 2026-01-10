namespace ConquerMapViewer.Core.Domain.Entities;

public struct MapLayer
{
    public int index;
    public int layertype;
    public int xInt;
    public int yInt;
    public List<MapBackdrop> Backdrops;
    public List<MapTerrainObject> TerrainObjects;
}
