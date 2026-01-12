using ConquerMapViewer.Core.Domain.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.Rendering.Drawing;

/// <summary>
/// Renders a grid overlay aligned to puzzle tile boundaries with optional debug information
/// </summary>
public class PuzzleGridDrawingComponent : BaseDrawingComponent
{
    private record struct GridTile(Rectangle Bounds);

    protected readonly Puzzle _puzzle;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<GridTile> _gridTiles = new();
    private Texture2D? _pixelTexture;
    public Color GridColor { get; set; } = new Color(0, 255, 0, 128); // Semi-transparent green

    public PuzzleGridDrawingComponent(
        Puzzle puzzle,
        GraphicsDevice graphicsDevice)
    {
        _puzzle = puzzle;
        _graphicsDevice = graphicsDevice;

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        _gridTiles.Clear();

        if (!Enabled)
            return;

        var numPiecesX = Math.Min(screenRect.Width / _puzzle.TileSize + 2, _puzzle.HorizontalTiles);
        var numPiecesY = Math.Min(screenRect.Height / _puzzle.TileSize + 2, _puzzle.VerticalTiles);
        var startPieceX = screenRect.X / _puzzle.TileSize;
        var startPieceY = screenRect.Y / _puzzle.TileSize;
        var offsetX = -screenRect.X % _puzzle.TileSize;
        var offsetY = -screenRect.Y % _puzzle.TileSize;

        var drawX = offsetX;
        var drawY = offsetY;

        for (var x = startPieceX; x < startPieceX + numPiecesX; x++)
        {
            for (var y = startPieceY; y < startPieceY + numPiecesY; y++)
            {
                if (x < _puzzle.HorizontalTiles && y < _puzzle.VerticalTiles && x >= 0 && y >= 0)
                {
                    var bounds = new Rectangle(drawX, drawY, _puzzle.TileSize, _puzzle.TileSize);
                    _gridTiles.Add(new GridTile(bounds));
                    drawY += _puzzle.TileSize;
                }
            }
            drawX += _puzzle.TileSize;
            drawY = offsetY;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (_pixelTexture == null || !Enabled)
            return;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);

        foreach (var tile in _gridTiles)
        {
            DrawRectangleOutline(spriteBatch, tile.Bounds, GridColor);
        }

        spriteBatch.End();
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        if (_pixelTexture == null)
            return;

        // Top
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        // Bottom
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - 1, rect.Width, 1), color);
        // Left
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        // Right
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X + rect.Width - 1, rect.Y, 1, rect.Height), color);
    }

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _pixelTexture?.Dispose();
                _gridTiles.Clear();
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