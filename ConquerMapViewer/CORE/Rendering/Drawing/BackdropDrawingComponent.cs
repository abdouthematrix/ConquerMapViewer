using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.Rendering.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.Rendering.Drawing;

public sealed class BackdropDrawingComponent : PuzzleDrawingComponent
{
    private const int HORIZONTAL_RATE_DIVISOR = 3;
    private const int VERTICAL_RATE_DIVISOR = 8;

    private readonly Puzzle _mainPuzzle;
    private readonly Matrix _scaleMatrix;

    public BackdropDrawingComponent(
        Puzzle backdropPuzzle,
        Puzzle mainPuzzle,
        IAniDictionary aniDictionary,
        TextureCache textureCache)
        : base(backdropPuzzle, aniDictionary, textureCache)
    {
        _mainPuzzle = mainPuzzle;

        // Calculate scale to stretch backdrop to fit main puzzle
        var scaleX = (float)_mainPuzzle.Width / _puzzle.Width;
        var scaleY = (float)_mainPuzzle.Height / _puzzle.Height;
        _scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1f);
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        // Calculate parallax offset
        var offsetX = screenRect.X;
        var offsetY = screenRect.Y;

        if (_puzzle.HorizontalRate.HasValue && _puzzle.HorizontalRate.Value != 0)
        {
            var divisor = _puzzle.HorizontalRate.Value / HORIZONTAL_RATE_DIVISOR;
            if (divisor != 0)
            {
                offsetX /= divisor;
            }
        }

        if (_puzzle.VerticalRate.HasValue && _puzzle.VerticalRate.Value != 0)
        {
            var divisor = _puzzle.VerticalRate.Value / VERTICAL_RATE_DIVISOR;
            if (divisor != 0)
            {
                offsetY /= divisor;
            }
        }

        // Transform screen rect back to backdrop space for tile loading
        var scaleX = (float)_puzzle.Width / _mainPuzzle.Width;
        var scaleY = (float)_puzzle.Height / _mainPuzzle.Height;

        var backdropRect = new Rectangle(
            (int)(offsetX * scaleX),
            (int)(offsetY * scaleY),
            (int)(screenRect.Width * scaleX),
            (int)(screenRect.Height * scaleY)
        );

        base.UpdateScreen(backdropRect);
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (!Enabled)
            return;

        // Combine the zoom transform with the backdrop scale transform
        var combinedTransform = _scaleMatrix * transformMatrix;

        base.Draw(spriteBatch, combinedTransform);
    }
}