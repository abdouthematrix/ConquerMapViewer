namespace ConquerMapViewer.WPF.Controls;

public sealed class MapViewerControl : WpfGame
{
    private const float ZOOM_SPEED = 0.15f;
    private const float PAN_SPEED = 5.0f;
    private const float PAN_SPEED_FAST = 50.0f;

    private IGraphicsDeviceService? _graphicsDeviceManager;
    private WpfKeyboard? _keyboard;
    private WpfMouse? _mouse;
    private SpriteBatch? _spriteBatch;
    private MapViewerService? _mapViewerService;
    private System.Windows.Point _lastMousePosition;
    private MouseState _lastMouseState;
    
    private int _frameCount;
    private double _elapsedTime;
    private int _currentFPS;

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

        HandleZoom(mouseState);
        HandlePan(keyboardState, mouseState);
        HandleKeyboardShortcuts(keyboardState);
        UpdateStatus(mouseState);

        _lastMousePosition = new System.Windows.Point(mouseState.X, mouseState.Y);
        _lastMouseState = mouseState;

        // Calculate FPS
        _frameCount++;
        _elapsedTime += gameTime.ElapsedGameTime.TotalSeconds;
        if (_elapsedTime >= 1.0)
        {
            _currentFPS = _frameCount;
            _frameCount = 0;
            _elapsedTime = 0;
            OnFPSChanged(_currentFPS);
        }
    }

    private void HandleZoom(MouseState mouseState)
    {
        if (_mapViewerService == null)
            return;

        if (mouseState.ScrollWheelValue != _lastMouseState.ScrollWheelValue)
        {
            float zoomDelta = (mouseState.ScrollWheelValue - _lastMouseState.ScrollWheelValue) / 120f;
            
            // Zoom towards mouse position
            var mousePos = new Vector2(mouseState.X, mouseState.Y);
            var worldPos = mousePos / _mapViewerService.Zoom + _mapViewerService.Position;
            
            _mapViewerService.Zoom *= (1 + zoomDelta * ZOOM_SPEED);
            
            // Adjust position to keep mouse point steady
            var newWorldPos = mousePos / _mapViewerService.Zoom + _mapViewerService.Position;
            _mapViewerService.Position += worldPos - newWorldPos;
        }
    }

    private void HandlePan(KeyboardState keyboardState, MouseState mouseState)
    {
        if (_mapViewerService == null)
            return;

        // Mouse panning with right button
        if (mouseState.RightButton == ButtonState.Pressed)
        {
            var currentPos = new Vector2(mouseState.X, mouseState.Y);
            var delta = currentPos - new Vector2((float)_lastMousePosition.X, (float)_lastMousePosition.Y);
            _mapViewerService.Position -= delta / _mapViewerService.Zoom;
        }

        // Keyboard panning
        var panSpeed = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift)
            ? PAN_SPEED_FAST
            : PAN_SPEED;

        var panDelta = Vector2.Zero;
        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            panDelta.Y -= panSpeed;
        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            panDelta.Y += panSpeed;
        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            panDelta.X -= panSpeed;
        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            panDelta.X += panSpeed;

        if (panDelta != Vector2.Zero)
        {
            _mapViewerService.Position += panDelta;
        }
    }

    private KeyboardState _lastKeyboardState;

    private void HandleKeyboardShortcuts(KeyboardState keyboardState)
    {
        if (_mapViewerService == null)
            return;

        // Check for key press (not held)
        bool IsKeyPressed(Keys key) => 
            keyboardState.IsKeyDown(key) && _lastKeyboardState.IsKeyUp(key);

        // Zoom shortcuts
        if (IsKeyPressed(Keys.OemPlus) || IsKeyPressed(Keys.Add))
            _mapViewerService.Zoom *= 1.2f;
        if (IsKeyPressed(Keys.OemMinus) || IsKeyPressed(Keys.Subtract))
            _mapViewerService.Zoom /= 1.2f;

        // Reset view
        if (IsKeyPressed(Keys.Home) || IsKeyPressed(Keys.H))
            _mapViewerService.ResetView();

        // Fit to window
        if (IsKeyPressed(Keys.F))
            _mapViewerService.FitToWindow();

        _lastKeyboardState = keyboardState;
    }

    private void UpdateStatus(MouseState mouseState)
    {
        if (_mapViewerService?.CoordinateSystem == null)
            return;

        var screenPoint = new Point(
            mouseState.X + _mapViewerService.DrawWindow.X,
            mouseState.Y + _mapViewerService.DrawWindow.Y
        );

        var mapCoord = _mapViewerService.CoordinateSystem.ScreenToMap(screenPoint);
        OnStatusChanged($"Screen: ({mouseState.X}, {mouseState.Y}) | Map: ({mapCoord.X:F1}, {mapCoord.Y:F1}) | Zoom: {_mapViewerService.Zoom * 100:F0}% | FPS: {_currentFPS}");
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_graphicsDeviceManager == null || _spriteBatch == null || _mapViewerService == null)
            return;

        _graphicsDeviceManager.GraphicsDevice.Clear(Color.DarkGray);
        _mapViewerService.Draw(_spriteBatch);
    }

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<int>? FPSChanged;

    private void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(this, status);
    }

    private void OnFPSChanged(int fps)
    {
        FPSChanged?.Invoke(this, fps);
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
