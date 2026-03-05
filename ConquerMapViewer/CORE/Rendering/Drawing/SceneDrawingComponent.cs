namespace ConquerMapViewer.Rendering.Drawing;

public sealed class SceneDrawingComponent : BaseDrawingComponent
{
    private record struct AnimatedScenePart(Vector2 Location, List<Texture2D> Frames, int Interval);

    private readonly IList<MapScene> _scenes;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly IAniDictionary _aniDictionary;
    private readonly TextureCache _textureCache;
    private readonly IPackageReader _packageReader;
    private readonly ISceneFileLoader _sceneFileLoader;
    private readonly Dictionary<string, Scene> _loadedScenes = new();
    private readonly List<AnimatedScenePart> _visibleSceneParts = new();
    private readonly int _startTick = Environment.TickCount;

    private const int SCREEN_BUFFER_X = 64;
    private const int SCREEN_BUFFER_Y = 32;
    private const int MIN_INTERVAL = 1;
    private static readonly Color TINT_COLOR = new(240, 255, 255, 255);

    public SceneDrawingComponent(
        IList<MapScene> scenes,
        IsometricCoordinateSystem coordinateSystem,
        IAniDictionary aniDictionary,
        TextureCache textureCache,
        IPackageReader packageReader,
        ISceneFileLoader sceneFileLoader)
    {
        _scenes = scenes;
        _coordinateSystem = coordinateSystem;
        _aniDictionary = aniDictionary;
        _textureCache = textureCache;
        _packageReader = packageReader;
        _sceneFileLoader = sceneFileLoader;

        // Preload all scene files
        foreach (var mapScene in _scenes)
        {
            LoadScene(mapScene);
        }
    }

    private void LoadScene(MapScene mapScene)
    {
        if (_loadedScenes.ContainsKey(mapScene.ScenePath))
            return;

        try
        {
            var sceneStream = _packageReader.LoadFile(mapScene.ScenePath);
            var scene = _sceneFileLoader.Load(sceneStream);
            _loadedScenes[mapScene.ScenePath] = scene;

            // Preload all ANI files for this scene
            var uniqueAniPaths = scene.SceneParts.Select(p => p.AniPath).Distinct();
            foreach (var aniPath in uniqueAniPaths)
            {
                _aniDictionary.Add(aniPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load scene: {mapScene.ScenePath}, Error: {ex.Message}");
        }
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        _visibleSceneParts.Clear();

        if (!Enabled)
            return;

        foreach (var mapScene in _scenes)
        {
            if (!_loadedScenes.TryGetValue(mapScene.ScenePath, out var scene))
                continue;

            foreach (var scenePart in scene.SceneParts)
            {
                // Location (m_posSceneOffset) is added to cell position
                var partCellPos = new Vector2(
                    mapScene.Location.X + scenePart.Location.X,
                    mapScene.Location.Y + scenePart.Location.Y);

                // Transform cell to screen coordinates
                var partScreenPos = _coordinateSystem.MapToScreen(partCellPos);

                // Check if visible
                if (!IsInScreenBounds(partScreenPos, screenRect, scenePart.ImageOffset))
                    continue;

                // Calculate final drawing location
                // ImageOffset (m_posOffset) is added to screen position in pixels
                var location = new Vector2(
                    partScreenPos.X - screenRect.X + scenePart.ImageOffset.X,
                    partScreenPos.Y - screenRect.Y + scenePart.ImageOffset.Y
                );

                // Load animation frames
                if (!_aniDictionary.TryGetFrames(scenePart.AniPath, scenePart.AniName, out var framePaths)
                    || framePaths.Count == 0)
                    continue;

                var frames = new List<Texture2D>(framePaths.Count);
                foreach (var framePath in framePaths)
                {
                    var texture = _textureCache.GetOrLoad(framePath);
                    frames.Add(texture);
                }

                if (frames.Count > 0)
                {
                    _visibleSceneParts.Add(new AnimatedScenePart(
                        location,
                        frames,
                        Math.Max(MIN_INTERVAL, scenePart.Interval)));
                }
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (!Enabled)
            return;

        var currentTick = Environment.TickCount - _startTick;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);

        foreach (var scenePart in _visibleSceneParts)
        {
            if (scenePart.Frames.Count == 0)
                continue;

            var frameIndex = (currentTick / scenePart.Interval) % scenePart.Frames.Count;
            var currentTexture = scenePart.Frames[frameIndex];
            spriteBatch.Draw(currentTexture, scenePart.Location, TINT_COLOR);
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
                _visibleSceneParts.Clear();
                _loadedScenes.Clear();
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