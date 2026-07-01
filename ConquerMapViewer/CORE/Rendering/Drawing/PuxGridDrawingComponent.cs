namespace ConquerMapViewer.Rendering.Drawing;

/// <summary>
/// Renders a grid overlay aligned to PUX tile boundaries.
/// Mirrors PuxDrawingComponent's UpdateScreen logic exactly.
/// </summary>
public sealed class PuxGridDrawingComponent : BaseDrawingComponent, IDisposable
{
    private record struct GridTile(Rectangle Bounds);

    private readonly Pux _pux;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<GridTile> _gridTiles = new();
    private Texture2D? _pixelTexture;
    private float _currentZoom = 1f;

    private const int EXTRA = 2;
    private const float MIN_LINE_THICKNESS = 1f;

    public Color GridColor { get; set; } = new Color(0, 200, 255, 128); // Cyan

    public PuxGridDrawingComponent(Pux pux, GraphicsDevice graphicsDevice)
    {
        _pux = pux;
        _graphicsDevice = graphicsDevice;

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public override void UpdateScreen(Rectangle sr)
    {
        _gridTiles.Clear();
        if (!Enabled || _pux.TileSize == 0) return;

        int numX = Math.Min(sr.Width / _pux.TileSize + EXTRA, _pux.Width);
        int numY = Math.Min(sr.Height / _pux.TileSize + EXTRA, _pux.Height);
        int sx = sr.X / _pux.TileSize;
        int sy = sr.Y / _pux.TileSize;
        int offX = -(sr.X % _pux.TileSize);
        int offY = -(sr.Y % _pux.TileSize);

        for (int x = sx; x < sx + numX; x++)
            for (int y = sy; y < sy + numY; y++)
            {
                if (x < 0 || x >= _pux.Width ||
                    y < 0 || y >= _pux.Height) continue;

                var screenX = offX + (x - sx) * _pux.TileSize;
                var screenY = offY + (y - sy) * _pux.TileSize;

                _gridTiles.Add(new GridTile(
                    new Rectangle(screenX, screenY, _pux.TileSize, _pux.TileSize)));
            }
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (_pixelTexture == null || !Enabled) return;

        _currentZoom = transformMatrix.M11;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                          null, null, null, null, transformMatrix);

        foreach (var tile in _gridTiles)
            DrawRectangleOutline(spriteBatch, tile.Bounds, GridColor);

        spriteBatch.End();
    }

    private void DrawRectangleOutline(SpriteBatch sb, Rectangle rect, Color color)
    {
        if (_pixelTexture == null) return;
        int t = (int)Math.Ceiling(MIN_LINE_THICKNESS / _currentZoom);
        sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, t), color);
        sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - t, rect.Width, t), color);
        sb.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, t, rect.Height), color);
        sb.Draw(_pixelTexture, new Rectangle(rect.X + rect.Width - t, rect.Y, t, rect.Height), color);
    }

    private bool _disposed;
    public void Dispose()
    {
        if (!_disposed)
        {
            _pixelTexture?.Dispose();
            _gridTiles.Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}