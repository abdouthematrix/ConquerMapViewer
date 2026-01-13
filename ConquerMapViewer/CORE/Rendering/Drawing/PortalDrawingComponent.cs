using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.Rendering.Coordinates;
using ConquerMapViewer.Rendering.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.Rendering.Drawing;

public sealed class PortalDrawingComponent : BaseDrawingComponent
{
    private record struct ScreenPortal(Vector2 Location);

    private readonly IList<MapPortal> _portals;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly TextureCache _textureCache;
    private readonly List<ScreenPortal> _visiblePortals = new();
    private Texture2D? _portalTexture;

    private const string PORTAL_DDS = @"c3/effect/exit.dds";
    private const int IMAGE_OFFSET_X = 128;
    private const int IMAGE_OFFSET_Y = 128;
    private const int SCREEN_BUFFER_X = 64;
    private const int SCREEN_BUFFER_Y = 32;

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

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);
        
        foreach (var portal in _visiblePortals)
        {
            spriteBatch.Draw(_portalTexture, portal.Location, Color.White);
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
