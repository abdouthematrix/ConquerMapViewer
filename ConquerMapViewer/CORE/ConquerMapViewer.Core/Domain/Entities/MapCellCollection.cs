namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class MapCellCollection
{
    public MapCell[,] CellData { get; }
    public MapSize CollectionSize { get; set; }
    public int CellWidth { get; set; } = 64;
    public int CellDepth { get; set; } = 32;

    public MapCellCollection(MapSize size)
    {
        CellData = new MapCell[size.Width, size.Height];
        CollectionSize = size;
    }

    public MapCell this[int x, int y]
    {
        get => CellData[x, y];
        set => CellData[x, y] = value;
    }
}
