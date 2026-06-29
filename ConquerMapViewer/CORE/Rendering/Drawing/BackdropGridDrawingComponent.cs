namespace ConquerMapViewer.Rendering.Drawing;

/// <summary>
/// Renders a grid overlay for the backdrop puzzle with proper scaling and parallax
/// </summary>
public class BackdropGridDrawingComponent : PuzzleGridDrawingComponent
{
    private const int HORIZONTAL_RATE_DIVISOR = 3;
    private const int VERTICAL_RATE_DIVISOR = 8;

    private readonly Puzzle _mainPuzzle;
    private readonly Matrix _scaleMatrix;

    public BackdropGridDrawingComponent(
        Puzzle backdropPuzzle,
        Puzzle mainPuzzle,
        GraphicsDevice graphicsDevice)
        : base(backdropPuzzle, graphicsDevice)
    {
        if (mainPuzzle == null)
            return;
        _mainPuzzle = mainPuzzle;

        // Calculate scale to match backdrop stretching
        var scaleX = (float)_mainPuzzle.Width / _puzzle.Width;
        var scaleY = (float)_mainPuzzle.Height / _puzzle.Height;
        _scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1f);

        // Use a different color to distinguish from main puzzle grid
        GridColor = new Color(255, 0, 255, 128); // Semi-transparent magenta
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        // Calculate parallax offset (same as BackdropDrawingComponent)
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

        // Transform screen rect to backdrop space
        var scaleX = (float)_puzzle.Width / _mainPuzzle.Width;
        var scaleY = (float)_puzzle.Height / _mainPuzzle.Height;

        var backdropRect = new Rectangle(
            (int)(offsetX * scaleX),
            (int)(offsetY * scaleY),
            (int)(screenRect.Width * scaleX),
            (int)(screenRect.Height * scaleY)
        );

        // Let the base class handle grid generation in backdrop space
        base.UpdateScreen(backdropRect);
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (!Enabled)
            return;

        // Combine scale matrix with transform (same as BackdropDrawingComponent)
        var combinedTransform = _scaleMatrix * transformMatrix;

        // Let the base class handle the actual drawing with the combined transform
        base.Draw(spriteBatch, combinedTransform);
    }
}