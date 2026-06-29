namespace ConquerMapViewer.Rendering.Drawing;

public sealed class TerrainObjectDrawingComponent : BaseDrawingComponent
{
    private record struct AnimatedObject(Vector2 Location, List<Texture2D> Frames, int Interval, MapPoint CellLocation, int Width, int Height);

    private readonly IList<MapTerrainObject> _terrainObjects;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly IAniDictionary _aniDictionary;
    private readonly TextureCache _textureCache;
    private readonly List<AnimatedObject> _visibleObjects = new();
    private readonly int _startTick = Environment.TickCount;

    private const int SCREEN_BUFFER_X = 64;
    private const int SCREEN_BUFFER_Y = 32;
    private const int MIN_INTERVAL = 1;
    private static readonly Color TINT_COLOR = new(240, 255, 255, 255);

    public TerrainObjectDrawingComponent(
        IList<MapTerrainObject> terrainObjects,
        IsometricCoordinateSystem coordinateSystem,
        IAniDictionary aniDictionary,
        TextureCache textureCache)
    {
        _terrainObjects = terrainObjects;
        _coordinateSystem = coordinateSystem;
        _aniDictionary = aniDictionary;
        _textureCache = textureCache;

        // Preload all ANI files
        var uniqueAniPaths = terrainObjects.Select(t => t.AniPath).Distinct();
        foreach (var aniPath in uniqueAniPaths)
        {
            _aniDictionary.Add(aniPath);
        }
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        _visibleObjects.Clear();

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

            if (!_aniDictionary.TryGetFrames(terrain.AniPath, terrain.AniName, out var framePaths) || framePaths.Count == 0)
                continue;

            var frames = new List<Texture2D>(framePaths.Count);
            foreach (var framePath in framePaths)
            {
                var texture = _textureCache.GetOrLoad(framePath);
                frames.Add(texture);
            }

            if (frames.Count > 0)
            {
                var width = terrain.PicWidth > 0 ? terrain.PicWidth : frames[0].Width;
                var height = terrain.PicHeight > 0 ? terrain.PicHeight : frames[0].Height;
                _visibleObjects.Add(new AnimatedObject(location, frames, Math.Max(MIN_INTERVAL, terrain.Interval), terrain.Location, width, height));
            }
        }

        // Sort by isometric depth (cell X + cell Y) so objects further into the
        // scene are drawn first, matching the original engine's painter algorithm.
        _visibleObjects.Sort((a, b) =>
        {
            int depthA = a.CellLocation.X + a.CellLocation.Y;
            int depthB = b.CellLocation.X + b.CellLocation.Y;
            return depthA.CompareTo(depthB);
        });
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (!Enabled)
            return;

        var currentTick = Environment.TickCount - _startTick;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);

        foreach (var obj in _visibleObjects)
        {
            if (obj.Frames.Count == 0)
                continue;

            var frameIndex = (currentTick / obj.Interval) % obj.Frames.Count;
            var currentTexture = obj.Frames[frameIndex];
            var destRect = new Rectangle((int)obj.Location.X, (int)obj.Location.Y, obj.Width, obj.Height);
            spriteBatch.Draw(currentTexture, destRect, TINT_COLOR);
        }

        spriteBatch.End();
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
                _visibleObjects.Clear();
                // Note: Don't dispose textures as they're managed by TextureCache
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