namespace ConquerMapViewer.WPF.ViewModels;

public sealed class MapViewerService : IDisposable
{
    private readonly MapLoadingService _mapLoadingService;
    private readonly IAniDictionary _aniDictionary;
    private readonly IPackageReader _packageReader;
    private readonly ISceneFileLoader _sceneFileLoader;
    private GraphicsDevice? _graphicsDevice;
    private TextureCache? _textureCache;

    private MapData? _mapData;
    private Puzzle? _puzzle;
    private IsometricCoordinateSystem? _coordinateSystem;
    private readonly Dictionary<DrawingAspect, List<IDrawingComponent>> _drawingComponents = new();

    private Vector2 _position = Vector2.Zero;
    private float _zoom = 0.5f;
    private Rectangle _drawWindow;
    private Vector2? _defaultPosition;

    private const float DEFAULT_ZOOM = 0.5f;

    // Minimum zoom is the level at which the puzzle exactly fills the viewport —
    // zooming out further would only show empty space beyond the puzzle edge.
    private float MinZoom
    {
        get
        {
            if (_puzzle == null || _graphicsDevice == null)
                return 0.01f;

            var viewport = _graphicsDevice.Viewport;
            return Math.Max(
                viewport.Width / (float)_puzzle.Width,
                viewport.Height / (float)_puzzle.Height
            );
        }
    }

    private const float MAX_ZOOM = 5.0f;
    private const float FIT_ZOOM_PADDING = 0.9f;
    private const float DEFAULT_POSITION_DIVISOR = 4f;

    public Vector2 Position
    {
        get => _position;
        set
        {
            _position = ClampPosition(value);
            UpdateDrawWindow();
        }
    }

    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Clamp(value, MinZoom, MAX_ZOOM);
            UpdateDrawWindow();
            ZoomChanged?.Invoke(this, _zoom);
        }
    }

    public Rectangle DrawWindow => _drawWindow;
    public IsometricCoordinateSystem? CoordinateSystem => _coordinateSystem;
    public Puzzle? Puzzle => _puzzle;
    public bool IsMapLoaded => _puzzle != null && _mapData != null;

    public event EventHandler<float>? ZoomChanged;

    public MapViewerService(
        MapLoadingService mapLoadingService,
        IAniDictionary aniDictionary,
        IPackageReader packageReader,
        ISceneFileLoader sceneFileLoader)
    {
        _mapLoadingService = mapLoadingService;
        _aniDictionary = aniDictionary;
        _packageReader = packageReader;
        _sceneFileLoader = sceneFileLoader;
    }

    public void SetGraphicsDevice(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _textureCache = new TextureCache(_packageReader, _graphicsDevice);
    }

    public void LoadMap(string path, int tileSize)
    {
        if (_graphicsDevice == null)
            throw new InvalidOperationException("GraphicsDevice must be set before loading maps");

        if (_textureCache == null)
            throw new InvalidOperationException("TextureCache not initialized");

        DisposeDrawingComponents();

        // Clear texture cache when loading a new map
        _textureCache.Clear();

        (_mapData, _puzzle) = _mapLoadingService.LoadMap(path, tileSize);
        _coordinateSystem = new IsometricCoordinateSystem(_puzzle, _mapData);

        InitializeDrawingComponents();

        _defaultPosition = new Vector2(
            _puzzle.Width / DEFAULT_POSITION_DIVISOR,
            _puzzle.Height / DEFAULT_POSITION_DIVISOR
        );

        ResetView();
    }

    private void InitializeDrawingComponents()
    {
        if (_puzzle == null || _mapData == null || _coordinateSystem == null || _textureCache == null)
            return;

        // Load backdrops first (they render behind everything)
        _drawingComponents[DrawingAspect.Backdrop] = new List<IDrawingComponent>();
        _drawingComponents[DrawingAspect.BackdropGrid] = new List<IDrawingComponent>();

        if (_mapData.Layers.Count > 0)
        {
            foreach (var layer in _mapData.Layers)
            {
                foreach (var backdrop in layer.Backdrops)
                {
                    // The puzzle is already loaded by MapLoadingService
                    if (backdrop.Puzzle != null)
                    {
                        var component = new BackdropDrawingComponent(
                            backdrop.Puzzle,
                            _puzzle,
                            _aniDictionary,
                            _textureCache
                        );

                        _drawingComponents[DrawingAspect.Backdrop].Add(component);

                        var component2 = new BackdropGridDrawingComponent(
                            backdrop.Puzzle,
                            _puzzle,
                            _graphicsDevice!
                        );

                        _drawingComponents[DrawingAspect.BackdropGrid].Add(component2);
                    }
                }
            }
        }

        _drawingComponents[DrawingAspect.Puzzle] = new List<IDrawingComponent>
        {
            new PuzzleDrawingComponent(_puzzle, _aniDictionary, _textureCache)
        };

        _drawingComponents[DrawingAspect.MapCell] = new List<IDrawingComponent>
        {
            new MapCellDrawingComponent(_mapData.Cells, _coordinateSystem, _graphicsDevice!)
        };

        _drawingComponents[DrawingAspect.Portals] = new List<IDrawingComponent>
        {
            new PortalDrawingComponent(_mapData.Portals, _coordinateSystem, _textureCache)
        };

        _drawingComponents[DrawingAspect.Scene] = new List<IDrawingComponent>
        {
            new SceneDrawingComponent(
                _mapData.Scenes,
                _coordinateSystem,
                _aniDictionary,
                _textureCache,
                _packageReader,
                _sceneFileLoader)
        };

        _drawingComponents[DrawingAspect.TerrainObject] = new List<IDrawingComponent>
        {
            new TerrainObjectDrawingComponent(_mapData.TerrainObjects, _coordinateSystem, _aniDictionary, _textureCache)
        };

        _drawingComponents[DrawingAspect.PuzzleGrid] = new List<IDrawingComponent>
        {
            new PuzzleGridDrawingComponent(_puzzle, _graphicsDevice!)
        };

        _drawingComponents[DrawingAspect.TerrainObjectGrid] = new List<IDrawingComponent>
        {
            new TerrainObjectGridDrawingComponent(_mapData.TerrainObjects, _coordinateSystem, _aniDictionary, _textureCache, _graphicsDevice)
        };

        _drawingComponents[DrawingAspect.SceneGrid] = new List<IDrawingComponent>
        {
            new SceneGridDrawingComponent(
                _mapData.Scenes,
                _coordinateSystem,
                _aniDictionary,
                _textureCache,
                _packageReader,
                _sceneFileLoader,
                _graphicsDevice!)
        };
        _drawingComponents[DrawingAspect.Effect] = new List<IDrawingComponent>
        {
            new EffectDrawingComponent(_mapData.Effects, _mapData.Cells, _coordinateSystem, _graphicsDevice!)
        };

        _drawingComponents[DrawingAspect.Sound] = new List<IDrawingComponent>
        {
           new SoundDrawingComponent(_mapData.Sounds, _mapData.Cells, _coordinateSystem, _graphicsDevice!)
        };
    }

    public void SetLayerEnabled(DrawingAspect aspect, bool enabled)
    {
        if (_drawingComponents.TryGetValue(aspect, out var components))
        {
            foreach (var component in components)
            {
                component.Enabled = enabled;

                // Update component when enabling it
                if (enabled)
                {
                    component.UpdateScreen(_drawWindow);
                }
            }
        }
    }

    public bool IsLayerEnabled(DrawingAspect aspect)
    {
        if (_drawingComponents.TryGetValue(aspect, out var components))
        {
            return components.Any(c => c.Enabled);
        }
        return false;
    }

    public void ResetView()
    {
        if (_defaultPosition.HasValue)
        {
            _zoom = DEFAULT_ZOOM;
            Position = _defaultPosition.Value;
            ZoomChanged?.Invoke(this, _zoom);
        }
    }

    public void FitToWindow()
    {
        if (_puzzle == null || _graphicsDevice == null)
            return;

        var viewport = _graphicsDevice.Viewport;
        var zoomX = viewport.Width / (float)_puzzle.Width;
        var zoomY = viewport.Height / (float)_puzzle.Height;

        Zoom = Math.Min(zoomX, zoomY) * FIT_ZOOM_PADDING;
        Position = Vector2.Zero;
    }

    public void JumpToMapCoordinate(Vector2 mapCoord)
    {
        if (_coordinateSystem == null)
            return;

        var screenPoint = _coordinateSystem.MapToScreen(mapCoord);
        Position = new Vector2(
            screenPoint.X - _drawWindow.Width / 2f,
            screenPoint.Y - _drawWindow.Height / 2f
        );
    }

    public Vector2? ScreenToMapCoordinate(Vector2 screenPoint)
    {
        if (_coordinateSystem == null)
            return null;

        // Adjust for zoom and position
        var worldPoint = new Vector2(
            (screenPoint.X / _zoom) + _position.X,
            (screenPoint.Y / _zoom) + _position.Y
        );

        return _coordinateSystem.ScreenToMap(worldPoint);
    }

    private Vector2 ClampPosition(Vector2 value)
    {
        if (_puzzle == null)
            return value;

        var maxX = Math.Max(0, _puzzle.Width - _drawWindow.Width);
        var maxY = Math.Max(0, _puzzle.Height - _drawWindow.Height);

        return new Vector2(
            Math.Clamp(value.X, 0, maxX),
            Math.Clamp(value.Y, 0, maxY)
        );
    }

    private void UpdateDrawWindow()
    {
        if (_puzzle == null || _coordinateSystem == null || _graphicsDevice == null)
            return;

        var viewport = _graphicsDevice.Viewport;
        var realWindow = new Vector2(
            Math.Min(viewport.Width, _puzzle.Width),
            Math.Min(viewport.Height, _puzzle.Height)
        );

        var effectiveWindow = Vector2.Transform(realWindow, Matrix.CreateScale(1f / _zoom));

        // Cap at puzzle bounds: when zoomed out far enough to "see" more world-space pixels
        // than the puzzle has, clamp the window so ClampPosition and all drawing components
        // never reference coordinates outside the puzzle.
        var windowW = (int)Math.Min(effectiveWindow.X, _puzzle.Width);
        var windowH = (int)Math.Min(effectiveWindow.Y, _puzzle.Height);

        // Build the window with the new dimensions first so ClampPosition can use them.
        _drawWindow = new Rectangle(
            (int)_position.X,
            (int)_position.Y,
            windowW,
            windowH
        );

        // Re-clamp: zoom changes alter the visible window size, which changes the valid
        // scroll range, so the current position may now be out of bounds.
        _position = ClampPosition(_position);
        _drawWindow.X = (int)_position.X;
        _drawWindow.Y = (int)_position.Y;

        UpdateVisibleComponents();
    }

    private void UpdateVisibleComponents()
    {
        foreach (var components in _drawingComponents.Values)
        {
            foreach (var component in components.Where(c => c.Enabled))
            {
                component.UpdateScreen(_drawWindow);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsMapLoaded)
            return;

        var transformMatrix = Matrix.CreateScale(_zoom);

        // Draw in layer order
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.Backdrop);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.Puzzle);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.MapCell);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.Portals);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.Scene);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.TerrainObject);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.Effect);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.Sound);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.BackdropGrid);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.PuzzleGrid);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.TerrainObjectGrid);
        DrawLayer(spriteBatch, transformMatrix, DrawingAspect.SceneGrid);
    }

    private void DrawLayer(SpriteBatch spriteBatch, Matrix transformMatrix, DrawingAspect aspect)
    {
        if (_drawingComponents.TryGetValue(aspect, out var components))
        {
            foreach (var component in components.Where(c => c.Enabled))
            {
                component.Draw(spriteBatch, transformMatrix);
            }
        }
    }

    private void DisposeDrawingComponents()
    {
        foreach (var components in _drawingComponents.Values)
        {
            foreach (var component in components)
            {
                (component as IDisposable)?.Dispose();
            }
        }
        _drawingComponents.Clear();
    }

    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DisposeDrawingComponents();
                _textureCache?.Dispose();
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