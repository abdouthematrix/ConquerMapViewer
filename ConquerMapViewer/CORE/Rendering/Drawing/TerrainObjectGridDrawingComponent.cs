using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.Rendering.Coordinates;
using ConquerMapViewer.Rendering.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.Rendering.Drawing;

/// <summary>
/// Renders a grid overlay around terrain objects with zoom-aware borders
/// </summary>
public sealed class TerrainObjectGridDrawingComponent : BaseDrawingComponent
{
    private record struct GridCell(Rectangle Bounds);

    private readonly IList<MapTerrainObject> _terrainObjects;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly IAniDictionary _aniDictionary;
    private readonly TextureCache _textureCache;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<GridCell> _visibleCells = new();
    private Texture2D? _pixelTexture;
    private float _currentZoom = 1f;

    private const int SCREEN_BUFFER_X = 64;
    private const int SCREEN_BUFFER_Y = 32;
    private const float MIN_LINE_THICKNESS = 1f;

    public Color GridColor { get; set; } = new Color(255, 0, 0, 180); // Semi-transparent red

    public TerrainObjectGridDrawingComponent(
        IList<MapTerrainObject> terrainObjects,
        IsometricCoordinateSystem coordinateSystem,
        IAniDictionary aniDictionary,
        TextureCache textureCache,
        GraphicsDevice graphicsDevice)
    {
        _terrainObjects = terrainObjects;
        _coordinateSystem = coordinateSystem;
        _aniDictionary = aniDictionary;
        _textureCache = textureCache;
        _graphicsDevice = graphicsDevice;

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // Preload all ANI files
        var uniqueAniPaths = terrainObjects.Select(t => t.AniPath).Distinct();
        foreach (var aniPath in uniqueAniPaths)
        {
            _aniDictionary.Add(aniPath);
        }
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        _visibleCells.Clear();

        if (!Enabled)
            return;

        foreach (var terrain in _terrainObjects)
        {
            var point = _coordinateSystem.MapToScreen(new Vector2(terrain.Location.X, terrain.Location.Y));

            if (!IsInScreenBounds(point, screenRect, terrain.ImageOffset))
                continue;

            var location = new Vector2(
                point.X - screenRect.X - terrain.ImageOffset.X,
                point.Y - screenRect.Y - terrain.ImageOffset.Y
            );

            // Get the first frame to determine actual dimensions
            if (!_aniDictionary.TryGetFrames(terrain.AniPath, terrain.AniName, out var framePaths) || framePaths.Count == 0)
                continue;

            // Load first frame to get actual texture dimensions
            var firstFrameTexture = _textureCache.GetOrLoad(framePaths[0]);
            var bounds = new Rectangle(
                (int)location.X,
                (int)location.Y,
                firstFrameTexture.Width,
                firstFrameTexture.Height
            );

            _visibleCells.Add(new GridCell(bounds));
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (_pixelTexture == null || !Enabled)
            return;

        // Extract zoom from transform matrix
        _currentZoom = transformMatrix.M11; // Assumes uniform scale

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);

        foreach (var cell in _visibleCells)
        {
            DrawRectangleOutline(spriteBatch, cell.Bounds, GridColor);
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

    private bool IsInScreenBounds(Vector2 point, Rectangle screenRect, MapPoint imageOffset)
    {
        return point.X > screenRect.X - imageOffset.X - SCREEN_BUFFER_X &&
               point.X < screenRect.Right + imageOffset.X + SCREEN_BUFFER_X &&
               point.Y > screenRect.Y - imageOffset.Y - SCREEN_BUFFER_Y &&
               point.Y < screenRect.Bottom + imageOffset.Y + SCREEN_BUFFER_Y;
    }

    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _pixelTexture?.Dispose();
                _visibleCells.Clear();
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