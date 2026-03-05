namespace ConquerMapViewer.Rendering.Drawing;

/// <summary>
/// Renders a grid overlay aligned to puzzle tile boundaries with optional debug information
/// </summary>
public class PuzzleGridDrawingComponent : BaseDrawingComponent
{
    private record struct GridTile(Rectangle Bounds);

    protected readonly Puzzle _puzzle;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<GridTile> _gridTiles = new();
    private Texture2D? _pixelTexture;
    private float _currentZoom = 1f;

    private const int EXTRA_TILES = 2;
    private const float MIN_LINE_THICKNESS = 1f;

    public Color GridColor { get; set; } = new Color(0, 255, 0, 128); // Semi-transparent green

    public PuzzleGridDrawingComponent(
        Puzzle puzzle,
        GraphicsDevice graphicsDevice)
    {
        _puzzle = puzzle;
        _graphicsDevice = graphicsDevice;

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        _gridTiles.Clear();

        if (!Enabled)
            return;

        // Calculate which puzzle tiles are visible
        var startTileX = Math.Max(0, screenRect.X / _puzzle.TileSize);
        var startTileY = Math.Max(0, screenRect.Y / _puzzle.TileSize);
        var endTileX = Math.Min(_puzzle.HorizontalTiles, (screenRect.Right / _puzzle.TileSize) + 1);
        var endTileY = Math.Min(_puzzle.VerticalTiles, (screenRect.Bottom / _puzzle.TileSize) + 1);

        // Generate grid tiles for visible area
        for (var tileX = startTileX; tileX < endTileX; tileX++)
        {
            for (var tileY = startTileY; tileY < endTileY; tileY++)
            {
                // Calculate world position of this tile
                var worldX = tileX * _puzzle.TileSize;
                var worldY = tileY * _puzzle.TileSize;

                // Convert to screen space (relative to screenRect)
                var screenX = worldX - screenRect.X;
                var screenY = worldY - screenRect.Y;

                var bounds = new Rectangle(screenX, screenY, _puzzle.TileSize, _puzzle.TileSize);
                _gridTiles.Add(new GridTile(bounds));
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (_pixelTexture == null || !Enabled)
            return;

        // Extract zoom from transform matrix
        _currentZoom = transformMatrix.M11; // Assumes uniform scale

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);

        foreach (var tile in _gridTiles)
        {
            DrawRectangleOutline(spriteBatch, tile.Bounds, GridColor);
        }

        spriteBatch.End();
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        if (_pixelTexture == null)
            return;

        // Calculate line thickness that maintains visibility at any zoom level
        // At low zoom, lines need to be thicker in world space to appear as 1 pixel on screen
        var lineThickness = (int)Math.Ceiling(MIN_LINE_THICKNESS / _currentZoom);

        // Top
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, lineThickness), color);
        // Bottom
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - lineThickness, rect.Width, lineThickness), color);
        // Left
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, lineThickness, rect.Height), color);
        // Right
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X + rect.Width - lineThickness, rect.Y, lineThickness, rect.Height), color);
    }

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _pixelTexture?.Dispose();
                _gridTiles.Clear();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}