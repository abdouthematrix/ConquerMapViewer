using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.Rendering.Coordinates;
using ConquerMapViewer.Infrastructure.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.Rendering.Drawing;

public sealed class TerrainObjectDrawingComponent : BaseDrawingComponent
{
    private record struct ScreenTexture(Vector2 Location, List<Texture2D> Textures, int Interval);

    private readonly IList<MapTerrainObject> _terrainObjects;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly IAniDictionary _aniDictionary;
    private readonly IPackageReader _packageReader;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<ScreenTexture> _textures = new();
    private readonly int _startTick = Environment.TickCount;

    public TerrainObjectDrawingComponent(
        IList<MapTerrainObject> terrainObjects,
        IsometricCoordinateSystem coordinateSystem,
        IAniDictionary aniDictionary,
        IPackageReader packageReader,
        GraphicsDevice graphicsDevice)
    {
        _terrainObjects = terrainObjects;
        _coordinateSystem = coordinateSystem;
        _aniDictionary = aniDictionary;
        _packageReader = packageReader;
        _graphicsDevice = graphicsDevice;

        // Preload all ANI files
        var uniqueAniPaths = terrainObjects.Select(t => t.AniPath).Distinct();
        foreach (var aniPath in uniqueAniPaths)
        {
            _aniDictionary.Add(aniPath);
        }
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        DisposeTextures();

        foreach (var terrain in _terrainObjects)
        {
            var point = _coordinateSystem.MapToScreen(new Vector2(terrain.Location.X, terrain.Location.Y));

            if (point.X <= screenRect.Location.X - terrain.ImageOffset.X - 64 ||
                point.X > screenRect.Location.X + screenRect.Size.X + terrain.ImageOffset.X + 64 ||
                point.Y <= screenRect.Location.Y - terrain.ImageOffset.Y - 32 ||
                point.Y > screenRect.Location.Y + screenRect.Size.Y + terrain.ImageOffset.Y + 32)
                continue;

            var location = new Vector2(
                point.X - screenRect.X - terrain.ImageOffset.X,
                point.Y - screenRect.Y - terrain.ImageOffset.Y
            );

            var textureList = new List<Texture2D>();
            var frames = _aniDictionary[terrain.AniPath, terrain.AniName];

            foreach (var frame in frames)
            {
                using var stream = _packageReader.LoadFile(frame);
                var extension = Path.GetExtension(frame).ToLowerInvariant();

                Texture2D texture = extension == ".dds"
                    ? DDSHelper.LoadFromStream(stream, _graphicsDevice)
                    : Texture2D.FromStream(_graphicsDevice, stream);

                textureList.Add(texture);
            }

            if (textureList.Count > 0)
            {
                _textures.Add(new ScreenTexture(location, textureList, terrain.Interval));
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        var currentTick = Environment.TickCount - _startTick;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);
        foreach (var texture in _textures)
        {
            if (texture.Textures.Count == 0)
                continue;

            var frameIndex = (currentTick / Math.Max(1, texture.Interval)) % texture.Textures.Count;
            var currentTexture = texture.Textures[frameIndex];
            spriteBatch.Draw(currentTexture, texture.Location, new Color(240, 255, 255, 255));
        }
        spriteBatch.End();
    }

    private void DisposeTextures()
    {
        foreach (var texture in _textures)
        {
            foreach (var tex in texture.Textures)
            {
                tex?.Dispose();
            }
        }
        _textures.Clear();
    }

    public void Dispose()
    {
        DisposeTextures();
    }
}
