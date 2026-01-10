namespace ConquerMapViewer.Rendering.Coordinates;

public sealed class IsometricCoordinateSystem
{
    private readonly Puzzle _puzzle;
    private readonly MapData _mapData;
    public Vector2[] CellPoints { get; }

    public IsometricCoordinateSystem(Puzzle puzzle, MapData mapData)
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
            new Vector2(cellWidth / 2, cellDepth - 1),

            new Vector2(2, cellDepth / 2),
            new Vector2(cellWidth / 2, 2),
            new Vector2(cellWidth - 2, cellDepth / 2),
            new Vector2(cellWidth / 2, cellDepth - 2),

            new Vector2(3, cellDepth / 2),
            new Vector2(cellWidth / 2, 3),
            new Vector2(cellWidth - 3, cellDepth / 2),
            new Vector2(cellWidth / 2, cellDepth - 3)
        };
    }

    public Vector2 ScreenToMap(Point screenPoint) =>
        new(
            screenPoint.X / 64f + screenPoint.Y / 32f,
            screenPoint.Y / 32f + (_puzzle.Width - screenPoint.X) / 64f
        );

    public Vector2 MapToScreen(Vector2 mapCoordinate)
    {
        var x = (mapCoordinate.X - mapCoordinate.Y) * 32 + _puzzle.Width / 2f;
        var y = (mapCoordinate.X + mapCoordinate.Y - (_mapData.Bounds.Height - 1)) * 16 + _puzzle.Height / 2f;
        return new Vector2(x, y);
    }
}
