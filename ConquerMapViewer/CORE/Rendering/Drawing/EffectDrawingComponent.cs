namespace ConquerMapViewer.Rendering.Drawing;

/// <summary>
/// Renders Map3DEffect positions as small yellow squares
/// </summary>
public sealed class EffectDrawingComponent : BaseDrawingComponent, IDisposable
{
    private record struct ScreenEffect(Rectangle Bounds);

    private readonly IList<Map3DEffect> _effects;
    private readonly MapCellCollection _cells;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<ScreenEffect> _visibleEffects = new();
    private Texture2D? _pixelTexture;
    private float _currentZoom = 1f;

    private const int MARKER_SIZE = 32;
    private const int SCREEN_BUFFER = 32;
    private const float MIN_LINE_THICKNESS = 1f;

    public Color EffectColor { get; set; } = new Color(255, 255, 0, 204); // Semi-transparent yellow

    public EffectDrawingComponent(
        IList<Map3DEffect> effects,
        MapCellCollection cells,
        IsometricCoordinateSystem coordinateSystem,
        GraphicsDevice graphicsDevice)
    {
        _effects = effects;
        _cells = cells;
        _coordinateSystem = coordinateSystem;
        _graphicsDevice = graphicsDevice;

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        _visibleEffects.Clear();

        if (!Enabled)
            return;

        foreach (var effect in _effects)
        {
            // Effects store world coordinates — convert to cell first
            var cellPos = _cells.World2Cell((int)effect.Location.X, (int)effect.Location.Y);
            var screenPos = _coordinateSystem.MapToScreen(new Vector2(cellPos.X, cellPos.Y));

            if (!IsInScreenBounds(screenPos, screenRect))
                continue;

            var drawX = (int)(screenPos.X - screenRect.X) - MARKER_SIZE / 2;
            var drawY = (int)(screenPos.Y - screenRect.Y) - MARKER_SIZE / 2;

            _visibleEffects.Add(new ScreenEffect(new Rectangle(drawX, drawY, MARKER_SIZE, MARKER_SIZE)));
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (_pixelTexture == null || !Enabled)
            return;

        _currentZoom = transformMatrix.M11;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);

        foreach (var effect in _visibleEffects)
        {
            // Filled square
            spriteBatch.Draw(_pixelTexture, effect.Bounds, EffectColor);

            // Outlined border at full opacity
            DrawRectangleOutline(spriteBatch, effect.Bounds, new Color(255, 255, 0, 255));
        }

        spriteBatch.End();
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        if (_pixelTexture == null)
            return;

        var lineThickness = (int)Math.Ceiling(MIN_LINE_THICKNESS / _currentZoom);

        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, lineThickness), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - lineThickness, rect.Width, lineThickness), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, lineThickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - lineThickness, rect.Y, lineThickness, rect.Height), color);
    }

    private bool IsInScreenBounds(Vector2 point, Rectangle screenRect)
    {
        return point.X > screenRect.X - MARKER_SIZE - SCREEN_BUFFER &&
               point.X < screenRect.Right + MARKER_SIZE + SCREEN_BUFFER &&
               point.Y > screenRect.Y - MARKER_SIZE - SCREEN_BUFFER &&
               point.Y < screenRect.Bottom + MARKER_SIZE + SCREEN_BUFFER;
    }

    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _pixelTexture?.Dispose();
                _visibleEffects.Clear();
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