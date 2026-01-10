using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.Infrastructure.Animation;
using ConquerMapViewer.Rendering.Coordinates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.Rendering.Drawing;

public sealed class PortalDrawingComponent : BaseDrawingComponent
{
    private record struct ScreenTexture(Vector2 Location, Texture2D Texture);

    private readonly IList<MapPortal> _portals;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly IPackageReader _packageReader;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<ScreenTexture> _textures = new();

    private const string PortalDDS = @"c3/effect/exit.dds";

    public PortalDrawingComponent(
        IList<MapPortal> portals,
        IsometricCoordinateSystem coordinateSystem,
        IPackageReader packageReader,
        GraphicsDevice graphicsDevice)
    {
        _portals = portals;
        _coordinateSystem = coordinateSystem;
        _packageReader = packageReader;
        _graphicsDevice = graphicsDevice;
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        DisposeTextures();

        foreach (var portal in _portals)
        {
            var point = _coordinateSystem.MapToScreen(new Vector2(portal.Location.X, portal.Location.Y));
            var imageOffset = new Vector2(128, 128);

            if (point.X <= screenRect.Location.X - imageOffset.X - 64 ||
                point.X > screenRect.Location.X + screenRect.Size.X + imageOffset.X + 64 ||
                point.Y <= screenRect.Location.Y - imageOffset.Y - 32 ||
                point.Y > screenRect.Location.Y + screenRect.Size.Y + imageOffset.Y + 32)
                continue;

            var location = new Vector2(
                point.X - screenRect.X - (imageOffset.X / 2),
                point.Y - screenRect.Y - (imageOffset.Y / 2)
            );

            using var stream = _packageReader.LoadFile(PortalDDS);
            var texture = DDSHelper.LoadFromStream(stream, _graphicsDevice);
            _textures.Add(new ScreenTexture(location, texture));
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);
        foreach (var texture in _textures)
        {
            spriteBatch.Draw(texture.Texture, texture.Location, Color.White);
        }
        spriteBatch.End();
    }

    private void DisposeTextures()
    {
        foreach (var texture in _textures)
        {
            texture.Texture?.Dispose();
        }
        _textures.Clear();
    }

    public void Dispose()
    {
        DisposeTextures();
    }
}
