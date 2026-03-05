namespace ConquerMapViewer.Rendering.Drawing;

/// <summary>
/// Renders a grid overlay around scene parts with zoom-aware borders
/// </summary>
public sealed class SceneGridDrawingComponent : BaseDrawingComponent
{
    private record struct GridCell(Rectangle Bounds);

    private readonly IList<MapScene> _scenes;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly IAniDictionary _aniDictionary;
    private readonly TextureCache _textureCache;
    private readonly IPackageReader _packageReader;
    private readonly ISceneFileLoader _sceneFileLoader;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<string, Scene> _loadedScenes = new();
    private readonly List<GridCell> _visibleCells = new();
    private Texture2D? _pixelTexture;
    private float _currentZoom = 1f;

    private const int SCREEN_BUFFER_X = 64;
    private const int SCREEN_BUFFER_Y = 32;
    private const float MIN_LINE_THICKNESS = 1f;

    public Color GridColor { get; set; } = new Color(0, 255, 255, 180); // Semi-transparent cyan

    public SceneGridDrawingComponent(
        IList<MapScene> scenes,
        IsometricCoordinateSystem coordinateSystem,
        IAniDictionary aniDictionary,
        TextureCache textureCache,
        IPackageReader packageReader,
        ISceneFileLoader sceneFileLoader,
        GraphicsDevice graphicsDevice)
    {
        _scenes = scenes;
        _coordinateSystem = coordinateSystem;
        _aniDictionary = aniDictionary;
        _textureCache = textureCache;
        _packageReader = packageReader;
        _sceneFileLoader = sceneFileLoader;
        _graphicsDevice = graphicsDevice;

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

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
        _visibleCells.Clear();

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

                // Get the first frame to determine actual dimensions
                if (!_aniDictionary.TryGetFrames(scenePart.AniPath, scenePart.AniName, out var framePaths)
                    || framePaths.Count == 0)
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
                _loadedScenes.Clear();
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