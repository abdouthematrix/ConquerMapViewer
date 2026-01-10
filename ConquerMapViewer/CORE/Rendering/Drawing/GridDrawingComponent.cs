using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ConquerMapViewer.Rendering.Coordinates;

namespace ConquerMapViewer.Rendering.Drawing;

public sealed class GridDrawingComponent : IDrawingComponent, IDisposable
{
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _tileSize;
    private Texture2D? _pixelTexture;
    private Rectangle _screenRect;

    public bool Enabled { get; set; } = false;

    public GridDrawingComponent(
        IsometricCoordinateSystem coordinateSystem,
        GraphicsDevice graphicsDevice,
        int tileSize)
    {
        _coordinateSystem = coordinateSystem;
        _graphicsDevice = graphicsDevice;
        _tileSize = tileSize;

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void UpdateScreen(Rectangle screenRect)
    {
        _screenRect = screenRect;
    }

    public void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (_pixelTexture == null || !Enabled)
            return;

        spriteBatch.Begin(transformMatrix: transformMatrix);

        var gridColor = new Color(0, 255, 0, 128); // Semi-transparent green
        var startX = (_screenRect.X / _tileSize) * _tileSize;
        var startY = (_screenRect.Y / _tileSize) * _tileSize;
        var endX = _screenRect.Right + _tileSize;
        var endY = _screenRect.Bottom + _tileSize;

        // Draw vertical lines
        for (int x = startX; x <= endX; x += _tileSize)
        {
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(x, _screenRect.Y, 1, _screenRect.Height),
                gridColor
            );
        }

        // Draw horizontal lines
        for (int y = startY; y <= endY; y += _tileSize)
        {
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(_screenRect.X, y, _screenRect.Width, 1),
                gridColor
            );
        }

        spriteBatch.End();
    }

    public void Dispose()
    {
        _pixelTexture?.Dispose();
    }
}
