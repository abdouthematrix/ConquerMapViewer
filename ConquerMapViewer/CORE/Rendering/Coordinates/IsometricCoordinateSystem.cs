namespace ConquerMapViewer.Rendering.Coordinates;

public sealed class IsometricCoordinateSystem
{
    private readonly MapSize _puzzle;
    private readonly MapData _mapData;
    public Vector2[] CellPoints { get; }

    public IsometricCoordinateSystem(MapSize puzzle, MapData mapData)
    {
        _puzzle = puzzle;
        _mapData = mapData;

        var cellWidth = mapData.Cells.CellWidth;
        var cellDepth = mapData.Cells.CellDepth;

        CellPoints = new[]
        {
            new Vector2(1, cellDepth / 2),
            new Vector2(cellWidth / 2, 1),
            new Vector2(cellWidth - 1, cellDepth / 2),
            new Vector2(cellWidth / 2, cellDepth - 1)
        };
    }

    public Vector2 ScreenToMap(Point screenPoint)
    {
        var a = (screenPoint.X - _puzzle.Width / 2f) / 32f;
        var b = (screenPoint.Y - _puzzle.Height / 2f) / 16f + (_mapData.Bounds.Height - 1);
        return new Vector2((b + a) / 2f, (b - a) / 2f);
    }

    public Vector2 ScreenToMap(Vector2 screenPoint)
    {
        var a = (screenPoint.X - _puzzle.Width / 2f) / 32f;
        var b = (screenPoint.Y - _puzzle.Height / 2f) / 16f + (_mapData.Bounds.Height - 1);
        return new Vector2((b + a) / 2f, (b - a) / 2f);
    }


    public Vector2 MapToScreen(Vector2 mapCoordinate)
    {
        var x = (mapCoordinate.X - mapCoordinate.Y) * 32 + _puzzle.Width / 2f;
        var y = (mapCoordinate.X + mapCoordinate.Y - (_mapData.Bounds.Height - 1)) * 16 + _puzzle.Height / 2f;
        return new Vector2(x, y);
    }
}