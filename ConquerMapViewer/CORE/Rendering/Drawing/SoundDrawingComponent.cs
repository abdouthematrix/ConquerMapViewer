namespace ConquerMapViewer.Rendering.Drawing;

/// <summary>
/// Renders MapSound positions as small cyan squares with circular range indicators
/// </summary>
public sealed class SoundDrawingComponent : BaseDrawingComponent, IDisposable
{
    private record struct ScreenSound(Rectangle Bounds, int ScreenRange);

    private readonly IList<MapSound> _sounds;
    private readonly MapCellCollection _cells;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<ScreenSound> _visibleSounds = new();
    private Texture2D? _pixelTexture;
    private float _currentZoom = 1f;

    private const int MARKER_SIZE = 32;
    private const int SCREEN_BUFFER = 64;
    private const float MIN_LINE_THICKNESS = 1f;

    /// <summary>Color used to fill the sound marker square.</summary>
    public Color SoundColor { get; set; } = new Color(0, 230, 255, 180); // Semi-transparent cyan

    /// <summary>Color used for the range radius outline.</summary>
    public Color RangeColor { get; set; } = new Color(0, 230, 255, 80); // Faint cyan

    /// <summary>When true, a range circle outline is drawn around each sound marker.</summary>
    public bool ShowRange { get; set; } = true;

    public SoundDrawingComponent(
        IList<MapSound> sounds,
        MapCellCollection cells,
        IsometricCoordinateSystem coordinateSystem,
        GraphicsDevice graphicsDevice)
    {
        _sounds = sounds;
        _cells = cells;
        _coordinateSystem = coordinateSystem;
        _graphicsDevice = graphicsDevice;

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        _visibleSounds.Clear();

        if (!Enabled)
            return;

        foreach (var sound in _sounds)
        {
            // Sounds store world coordinates — convert to cell first
            var cellPos = _cells.World2Cell((int)sound.Location.X, (int)sound.Location.Y);
            var screenPos = _coordinateSystem.MapToScreen(new Vector2(cellPos.X, cellPos.Y));

            // Approximate pixels-per-cell to convert Range (in cells) to screen pixels
            var rangeInPixels = (int)(sound.Range * _cells.CellWidth / 2f);

            if (!IsInScreenBounds(screenPos, screenRect, rangeInPixels))
                continue;

            var drawX = (int)(screenPos.X - screenRect.X) - MARKER_SIZE / 2;
            var drawY = (int)(screenPos.Y - screenRect.Y) - MARKER_SIZE / 2;

            _visibleSounds.Add(new ScreenSound(
                new Rectangle(drawX, drawY, MARKER_SIZE, MARKER_SIZE),
                rangeInPixels));
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (_pixelTexture == null || !Enabled)
            return;

        _currentZoom = transformMatrix.M11;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);

        foreach (var sound in _visibleSounds)
        {
            // Optionally draw the range radius as a circle
            if (ShowRange && sound.ScreenRange > 0)
                DrawCircleOutline(spriteBatch, sound.Bounds, sound.ScreenRange, RangeColor);

            // Filled square marker
            spriteBatch.Draw(_pixelTexture, sound.Bounds, SoundColor);

            // Solid border at full opacity
            DrawRectangleOutline(spriteBatch, sound.Bounds, new Color(0, 230, 255, 255));
        }

        spriteBatch.End();
    }

    /// <summary>
    /// Draws a hollow circle outline centred on <paramref name="markerBounds"/>
    /// with the given <paramref name="radius"/> in screen pixels,
    /// approximated as <paramref name="segments"/> line segments.
    /// </summary>
    private void DrawCircleOutline(SpriteBatch spriteBatch, Rectangle markerBounds, int radius, Color color, int segments = 64)
    {
        if (_pixelTexture == null || radius <= 0)
            return;

        var cx = markerBounds.Center.X;
        var cy = markerBounds.Center.Y;
        var thickness = (int)Math.Max(1, Math.Ceiling(MIN_LINE_THICKNESS / _currentZoom));
        var step = MathF.PI * 2f / segments;

        for (var i = 0; i < segments; i++)
        {
            var a0 = step * i;
            var a1 = step * (i + 1);
            var from = new Point(cx + (int)(MathF.Cos(a0) * radius), cy + (int)(MathF.Sin(a0) * radius));
            var to = new Point(cx + (int)(MathF.Cos(a1) * radius), cy + (int)(MathF.Sin(a1) * radius));
            DrawLine(spriteBatch, from, to, thickness, color);
        }
    }

    /// <summary>Draws a 1-pixel-wide (scaled) line between two points using the pixel texture.</summary>
    private void DrawLine(SpriteBatch spriteBatch, Point from, Point to, int thickness, Color color)
    {
        if (_pixelTexture == null)
            return;

        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = (float)Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.5f)
            return;

        var angle = (float)Math.Atan2(dy, dx);
        spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(from.X, from.Y, (int)length, thickness),
            null,
            color,
            angle,
            Vector2.Zero,
            SpriteEffects.None,
            0f);
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        if (_pixelTexture == null)
            return;

        var t = (int)Math.Ceiling(MIN_LINE_THICKNESS / _currentZoom);

        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, t), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, t, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), color);
    }

    private bool IsInScreenBounds(Vector2 point, Rectangle screenRect, int extraBuffer = 0)
    {
        var buf = MARKER_SIZE + SCREEN_BUFFER + extraBuffer;
        return point.X > screenRect.X - buf &&
               point.X < screenRect.Right + buf &&
               point.Y > screenRect.Y - buf &&
               point.Y < screenRect.Bottom + buf;
    }

    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _pixelTexture?.Dispose();
                _visibleSounds.Clear();
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