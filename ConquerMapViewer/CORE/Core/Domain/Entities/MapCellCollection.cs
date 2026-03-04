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

    public int m_posOriginX => CellWidth * CollectionSize.Width / 2;
    public int m_posOriginY => CellDepth / 2;
    public Vector2 World2Cell(int worldX, int worldY)
    {
        int cellX, cellY;
        worldX -= m_posOriginX;
        worldY -= m_posOriginY;

        double dWorldX = (double)worldX;
        double dWorldY = (double)worldY;
        double dCellWidth = (double)CellWidth;
        double dCellHeight = (double)CellDepth;

        double dTemp0 = (dWorldX / dCellWidth) + (dWorldY / dCellHeight);
        double dTemp1 = (dWorldY / dCellHeight) - (dWorldX / dCellWidth);

        cellX = Double2Int(dTemp0);
        cellY = Double2Int(dTemp1);
        return new Vector2(cellX, cellY);
    }
    public Vector2 Cell2World(int cellX, int cellY)
    {
        int worldX, worldY;
        worldX = CellWidth * (cellX - cellY) / 2 + m_posOriginX;
        worldY = CellDepth * (cellX + cellY) / 2 + m_posOriginY;
        return new Vector2(worldX, worldY);
    }
    private int Double2Int(double value)
    {
        if ((int)(value + 0.5) > (int)value)
            return (int)value + 1;
        else
            return (int)value;
    }
}
