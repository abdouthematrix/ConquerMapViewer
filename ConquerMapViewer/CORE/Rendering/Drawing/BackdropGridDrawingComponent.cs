namespace ConquerMapViewer.Rendering.Drawing;

/// <summary>
/// Renders a grid overlay for the backdrop puzzle with proper scaling and parallax.
/// Parallax matches the formula used by CSceneLayer::Show() in the original C++ engine.
/// </summary>
public class BackdropGridDrawingComponent : PuzzleGridDrawingComponent
{
    private readonly MapSize _mainPuzzle;
    private readonly Matrix _scaleMatrix;

    // Layer-level parallax rates (0–100), matching CSceneLayer::GetMoveRateX/Y().
    // Must be identical to those given to the paired BackdropDrawingComponent so
    // that the grid overlay stays aligned with the rendered backdrop tiles.
    private readonly int _moveRateX;
    private readonly int _moveRateY;

    public BackdropGridDrawingComponent(
        Puzzle backdropPuzzle,
        MapSize mainPuzzle,
        GraphicsDevice graphicsDevice,
        int moveRateX = 100,
        int moveRateY = 100)
        : base(backdropPuzzle, graphicsDevice)
    {
        _mainPuzzle = mainPuzzle;
        _moveRateX = moveRateX;
        _moveRateY = moveRateY;

        // Same scale as BackdropDrawingComponent so the grid lines sit on top of
        // the stretched backdrop tiles at exactly the right positions.
        var scaleX = (float)_mainPuzzle.Width / _puzzle.Width;
        var scaleY = (float)_mainPuzzle.Height / _puzzle.Height;
        _scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1f);

        GridColor = new Color(255, 0, 255, 128); // Semi-transparent magenta
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        // Identical parallax calculation to BackdropDrawingComponent.UpdateScreen().
        // Both components must receive the same moveRateX/Y values so the grid
        // overlay is always pixel-perfect over the backdrop tiles.

        float mainMapCenterX = _mainPuzzle.Width / 2f;
        float mainMapCenterY = _mainPuzzle.Height / 2f;

        float viewCenterX = screenRect.X + screenRect.Width / 2f;
        float viewCenterY = screenRect.Y + screenRect.Height / 2f;

        float offsetX = viewCenterX - mainMapCenterX;
        float offsetY = viewCenterY - mainMapCenterY;

        float parallaxCenterX = mainMapCenterX + offsetX * _moveRateX / 100f;
        float parallaxCenterY = mainMapCenterY + offsetY * _moveRateY / 100f;

        float parallaxViewX = parallaxCenterX - screenRect.Width / 2f;
        float parallaxViewY = parallaxCenterY - screenRect.Height / 2f;

        float invScaleX = (float)_puzzle.Width / _mainPuzzle.Width;
        float invScaleY = (float)_puzzle.Height / _mainPuzzle.Height;

        var backdropRect = new Rectangle(
            (int)(parallaxViewX * invScaleX),
            (int)(parallaxViewY * invScaleY),
            (int)(screenRect.Width * invScaleX),
            (int)(screenRect.Height * invScaleY)
        );

        base.UpdateScreen(backdropRect);
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (!Enabled)
            return;

        var combinedTransform = _scaleMatrix * transformMatrix;

        base.Draw(spriteBatch, combinedTransform);
    }
}