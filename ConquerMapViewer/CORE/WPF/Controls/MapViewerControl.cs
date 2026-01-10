using Microsoft.Xna.Framework.Input;

namespace ConquerMapViewer.WPF.Controls;

public sealed class MapViewerControl : WpfGame
{
    private IGraphicsDeviceService? _graphicsDeviceManager;
    private WpfKeyboard? _keyboard;
    private WpfMouse? _mouse;
    private SpriteBatch? _spriteBatch;
    private MapViewerService? _mapViewerService;
    private System.Windows.Point _lastMousePosition;
    private MouseState _lastmouseState;

    public MapViewerService? MapViewerService
    {
        get => _mapViewerService;
        set
        {
            _mapViewerService = value;
            if (_mapViewerService != null && _graphicsDeviceManager != null)
            {
                _mapViewerService.SetGraphicsDevice(_graphicsDeviceManager.GraphicsDevice);
            }
        }
    }

    protected override void Initialize()
    {
        _graphicsDeviceManager = new WpfGraphicsDeviceService(this);
        _keyboard = new WpfKeyboard(this);
        _mouse = new WpfMouse(this);

        base.Initialize();

        _spriteBatch = new SpriteBatch(_graphicsDeviceManager.GraphicsDevice);

        if (_mapViewerService != null)
        {
            _mapViewerService.SetGraphicsDevice(_graphicsDeviceManager.GraphicsDevice);
        }
    }    
    protected override void Update(GameTime gameTime)
    {
        if (_keyboard == null || _mouse == null || _mapViewerService == null)
            return;

        var keyboardState = _keyboard.GetState();
        var mouseState = _mouse.GetState();

        // Handle zoom with mouse wheel        
        if (mouseState.ScrollWheelValue < _lastmouseState.ScrollWheelValue)
        {
            _mapViewerService.Zoom *= 1.01f;
        }
        else if (mouseState.ScrollWheelValue > _lastmouseState.ScrollWheelValue)
        {
            _mapViewerService.Zoom *= 1.0f / 1.01f;
        }

        // Handle pan with middle mouse button
        if (mouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
        {
            var currentPos = new Vector2(mouseState.X, mouseState.Y);
            var delta = currentPos - new Vector2((float)_lastMousePosition.X, (float)_lastMousePosition.Y);
            _mapViewerService.Position -= delta;
        }

        _lastMousePosition = new System.Windows.Point(mouseState.X, mouseState.Y);

        // Update status with mouse position
        if (_mapViewerService.CoordinateSystem != null)
        {
            var screenPoint = new Microsoft.Xna.Framework.Point(
                mouseState.X + _mapViewerService.DrawWindow.X,
                mouseState.Y + _mapViewerService.DrawWindow.Y
            );

            var mapCoord = _mapViewerService.CoordinateSystem.ScreenToMap(screenPoint);
            OnStatusChanged($"Mouse: ({mouseState.X}, {mouseState.Y}) | Map: ({mapCoord.X:F2}, {mapCoord.Y:F2})");
        }
        _lastmouseState = mouseState;
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_graphicsDeviceManager == null || _spriteBatch == null || _mapViewerService == null)
            return;

        _graphicsDeviceManager.GraphicsDevice.Clear(Color.LightGray);
        _mapViewerService.Draw(_spriteBatch);

    }

    public event EventHandler<string>? StatusChanged;

    private void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(this, status);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _spriteBatch?.Dispose();
            _mapViewerService?.Dispose();
        }
        base.Dispose(disposing);
    }
}
