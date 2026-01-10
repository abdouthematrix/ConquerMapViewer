using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Core.Domain.Enums;
using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.Core.Services;
using ConquerMapViewer.Rendering.Coordinates;
using ConquerMapViewer.Rendering.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.WPF.ViewModels;

public sealed class MapViewerService : IDisposable
{
    private readonly MapLoadingService _mapLoadingService;
    private readonly IAniDictionary _aniDictionary;
    private readonly IPackageReader _packageReader;
    private GraphicsDevice? _graphicsDevice;

    private MapData? _mapData;
    private Puzzle? _puzzle;
    private IsometricCoordinateSystem? _coordinateSystem;
    private readonly Dictionary<DrawingAspect, List<IDrawingComponent>> _drawingComponents = new();

    private Vector2 _position = Vector2.Zero;
    private float _zoom = 0.5f;
    private Rectangle _drawWindow;
    private Vector2? _defaultPosition;
    private float _defaultZoom = 0.5f;

    public Vector2 Position
    {
        get => _position;
        set
        {
            if (_puzzle != null)
            {
                var maxX = Math.Max(0, _puzzle.Width - _drawWindow.Width);
                var maxY = Math.Max(0, _puzzle.Height - _drawWindow.Height);
                _position = new Vector2(
                    Math.Clamp(value.X, 0, maxX),
                    Math.Clamp(value.Y, 0, maxY)
                );
            }
            else
            {
                _position = value;
            }
            UpdateDrawWindow();
        }
    }

    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Clamp(value, 0.1f, 5.0f);
            UpdateDrawWindow();
            ZoomChanged?.Invoke(this, _zoom);
        }
    }

    public Rectangle DrawWindow => _drawWindow;
    public IsometricCoordinateSystem? CoordinateSystem => _coordinateSystem;
    public Puzzle? Puzzle => _puzzle;

    public event EventHandler<float>? ZoomChanged;

    public MapViewerService(
        MapLoadingService mapLoadingService,
        IAniDictionary aniDictionary,
        IPackageReader packageReader)
    {
        _mapLoadingService = mapLoadingService;
        _aniDictionary = aniDictionary;
        _packageReader = packageReader;
    }

    public void SetGraphicsDevice(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadMap(string path, int tileSize)
    {
        if (_graphicsDevice == null)
            throw new InvalidOperationException("GraphicsDevice must be set before loading maps");

        DisposeDrawingComponents();

        (_mapData, _puzzle) = _mapLoadingService.LoadMap(path, tileSize);
        _coordinateSystem = new IsometricCoordinateSystem(_puzzle, _mapData);

        InitializeDrawingComponents();
        
        _defaultPosition = new Vector2(_puzzle.Width / 4f, _puzzle.Height / 4f);
        _defaultZoom = 0.5f;
        
        UpdateDrawWindow();
    }

    private void InitializeDrawingComponents()
    {
        if (_puzzle == null || _mapData == null || _coordinateSystem == null || _graphicsDevice == null)
            return;

        _drawingComponents[DrawingAspect.Puzzle] = new List<IDrawingComponent>
        {
            new PuzzleDrawingComponent(_puzzle, _aniDictionary, _packageReader, _graphicsDevice)
        };

        _drawingComponents[DrawingAspect.MapCell] = new List<IDrawingComponent>
        {
            new MapCellDrawingComponent(_mapData.Cells, _coordinateSystem, _graphicsDevice)
        };

        _drawingComponents[DrawingAspect.Portals] = new List<IDrawingComponent>
        {
            new PortalDrawingComponent(_mapData.Portals, _coordinateSystem, _packageReader, _graphicsDevice)
        };

        _drawingComponents[DrawingAspect.TerrainObject] = new List<IDrawingComponent>
        {
            new TerrainObjectDrawingComponent(_mapData.TerrainObjects, _coordinateSystem, _aniDictionary, _packageReader, _graphicsDevice)
        };

        _drawingComponents[DrawingAspect.Grid] = new List<IDrawingComponent>
        {
            new GridDrawingComponent(_coordinateSystem, _graphicsDevice, _puzzle.TileSize)
        };
    }

    public void SetLayerEnabled(DrawingAspect aspect, bool enabled)
    {
        if (_drawingComponents.TryGetValue(aspect, out var components))
        {
            foreach (var component in components)
            {
                component.Enabled = enabled;
            }
        }
    }

    public void ResetView()
    {
        if (_defaultPosition.HasValue)
        {
            _zoom = _defaultZoom;
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
        
        Zoom = Math.Min(zoomX, zoomY) * 0.9f;
        Position = Vector2.Zero;
    }

    public void JumpToMapCoordinate(Vector2 mapCoord)
    {
        if (_coordinateSystem == null)
            return;

        var screenPoint = _coordinateSystem.MapToScreen(mapCoord);
        Position = new Vector2(
            screenPoint.X - _drawWindow.Width / 2,
            screenPoint.Y - _drawWindow.Height / 2
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

        var effectiveWindow = Vector2.Transform(realWindow, Matrix.CreateScale(1 / _zoom));
        _drawWindow = new Rectangle(
            new Point((int)_position.X, (int)_position.Y),
            new Point((int)effectiveWindow.X, (int)effectiveWindow.Y)
        );

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
        var transformMatrix = Matrix.CreateScale(_zoom);
       
        foreach (var components in _drawingComponents.Values)
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

    public void Dispose()
    {
        DisposeDrawingComponents();
    }
}
