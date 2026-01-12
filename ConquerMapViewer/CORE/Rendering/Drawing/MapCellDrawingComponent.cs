namespace ConquerMapViewer.Rendering.Drawing;

/// <summary>
/// Renders map cell outlines in isometric projection
/// </summary>
public sealed class MapCellDrawingComponent : BaseDrawingComponent, IDisposable
{
    private const int CELL_WIDTH = 64;
    private const int CELL_HALF_WIDTH = 32;
    private const int CELL_HEIGHT = 32;
    private const int CELL_HALF_HEIGHT = 16;

    private readonly MapCellCollection _cells;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly CellVertexBuilder _vertexBuilder;

    public MapCellDrawingComponent(
        MapCellCollection cells,
        IsometricCoordinateSystem coordinateSystem,
        GraphicsDevice graphicsDevice)
    {
        _cells = cells;
        _coordinateSystem = coordinateSystem;
        _vertexBuilder = new CellVertexBuilder(coordinateSystem.CellPoints, graphicsDevice);
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        // Calculate grid bounds for custom isometric layout
        var numCellsWidth = screenRect.Size.X / CELL_WIDTH + 2;
        var numCellHeight = screenRect.Size.X / CELL_HEIGHT + 2;
        var estimatedCells = numCellsWidth * 2 * numCellHeight;

        _vertexBuilder.Begin(estimatedCells);

        var xOffset = screenRect.X % CELL_WIDTH + CELL_HALF_WIDTH;
        var yOffset = screenRect.Y % CELL_HEIGHT;
        var drawX = -xOffset;
        var drawY = -yOffset;

        var mapWidth = _cells.CollectionSize.Width;
        var mapHeight = _cells.CollectionSize.Height;
        var screenLocX = screenRect.Location.X;
        var screenLocY = screenRect.Location.Y;

        for (var x = 0; x < numCellsWidth * 2; x++)
        {
            var xBase = x * CELL_HALF_WIDTH + drawX;
            var yRowOffset = drawY - (x & 1) * CELL_HALF_HEIGHT; // x & 1 is faster than x % 2

            for (var y = 0; y < numCellHeight; y++)
            {
                var yBase = y * CELL_HEIGHT + yRowOffset;

                var screenPos = new Point(
                    xBase + screenLocX + CELL_HALF_WIDTH,
                    yBase + screenLocY + CELL_HALF_HEIGHT
                );

                var mapCoord = _coordinateSystem.ScreenToMap(screenPos);

                // Bounds check
                if (mapCoord.X >= 0 && mapCoord.X < mapWidth &&
                    mapCoord.Y >= 0 && mapCoord.Y < mapHeight)
                {
                    var cell = _cells[(int)mapCoord.X, (int)mapCoord.Y];
                    var cellPos = new Vector2(xBase, yBase);
                    _vertexBuilder.AddCell(cellPos, cell.AccessColor);
                }
            }
        }

        _vertexBuilder.End();
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        _vertexBuilder.Draw(transformMatrix);
    }

    public void Dispose()
    {
        _vertexBuilder?.Dispose();
    }
}