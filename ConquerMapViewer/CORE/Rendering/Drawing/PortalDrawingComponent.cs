namespace ConquerMapViewer.Rendering.Drawing;

public sealed class PortalDrawingComponent : BaseDrawingComponent
{
    private record struct ScreenPortal(Vector2 Location);

    private readonly IList<MapPortal> _portals;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly TextureCache _textureCache;
    private readonly List<ScreenPortal> _visiblePortals = new();
    private Texture2D? _portalTexture;

    // Animation state
    private float _animationTime;
    private const float ROTATION_SPEED = 2f; // Full rotation every 2 seconds
    private const float FADE_CYCLE_SPEED = 3f; // Fade cycle every 3 seconds

    private const string PORTAL_DDS = @"c3/effect/exit.dds";
    private const int IMAGE_OFFSET_X = 128;
    private const int IMAGE_OFFSET_Y = 128;
    private const int SCREEN_BUFFER_X = 64;
    private const int SCREEN_BUFFER_Y = 32;
    private const float DRAW_SCALE = 2f; // 128x128 texture drawn at 256x256

    public PortalDrawingComponent(
        IList<MapPortal> portals,
        IsometricCoordinateSystem coordinateSystem,
        TextureCache textureCache)
    {
        _portals = portals;
        _coordinateSystem = coordinateSystem;
        _textureCache = textureCache;
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        _visiblePortals.Clear();

        // Load portal texture once
        _portalTexture ??= _textureCache.GetOrLoad(PORTAL_DDS);

        foreach (var portal in _portals)
        {
            var point = _coordinateSystem.MapToScreen(new Vector2(portal.Location.X, portal.Location.Y));

            if (!IsInScreenBounds(point, screenRect))
                continue;

            var location = new Vector2(
                point.X - screenRect.X - (IMAGE_OFFSET_X / 2),
                point.Y - screenRect.Y - (IMAGE_OFFSET_Y / 2)
            );

            _visiblePortals.Add(new ScreenPortal(location));
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (_portalTexture == null || !Enabled)
            return;

        // Update animation time (assuming 60fps, increment by deltaTime if available)
        _animationTime += 0.016f; // ~60fps frame time

        // Calculate rotation (full 360 degree rotation)
        float rotation = (_animationTime / ROTATION_SPEED) * MathHelper.TwoPi;

        // Calculate fade (smooth sine wave between 0.3 and 1.0 for visibility)
        float fade = 0.65f + (float)Math.Sin(_animationTime / FADE_CYCLE_SPEED * MathHelper.TwoPi) * 0.35f;

        // Create color with fade applied
        Color portalColor = Color.White * fade;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);

        foreach (var portal in _visiblePortals)
        {
            // Calculate origin (center of texture for rotation)
            Vector2 origin = new Vector2(_portalTexture.Width / 2f, _portalTexture.Height / 2f);

            // Adjust position to account for origin offset
            Vector2 drawPosition = portal.Location + origin;

            spriteBatch.Draw(
                _portalTexture,
                drawPosition,
                null,
                portalColor,
                rotation,
                origin,
                DRAW_SCALE, // scale to 256x256
                SpriteEffects.None,
                0f);
        }

        spriteBatch.End();
    }

    private bool IsInScreenBounds(Vector2 point, Rectangle screenRect)
    {
        return point.X > screenRect.X - IMAGE_OFFSET_X - SCREEN_BUFFER_X &&
               point.X < screenRect.Right + IMAGE_OFFSET_X + SCREEN_BUFFER_X &&
               point.Y > screenRect.Y - IMAGE_OFFSET_Y - SCREEN_BUFFER_Y &&
               point.Y < screenRect.Bottom + IMAGE_OFFSET_Y + SCREEN_BUFFER_Y;
    }

    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _visiblePortals.Clear();
                // Note: Don't dispose _portalTexture as it's managed by TextureCache
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