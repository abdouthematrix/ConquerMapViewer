namespace ConquerMapViewer.Rendering.Drawing;

public sealed class BackdropDrawingComponent : PuzzleDrawingComponent
{
    private readonly MapSize _mainPuzzle;
    private readonly Matrix _scaleMatrix;

    // Layer-level parallax rates (0–100), matching CSceneLayer::GetMoveRateX/Y().
    // 100 = moves with the camera (no parallax), 50 = half speed, 0 = fixed.
    // Default 100 means no parallax effect (equivalent to a normal scene layer).
    private readonly int _moveRateX;
    private readonly int _moveRateY;

    public BackdropDrawingComponent(
        Puzzle backdropPuzzle,
        MapSize mainPuzzle,
        IAniDictionary aniDictionary,
        TextureCache textureCache,
        int moveRateX = 100,
        int moveRateY = 100)
        : base(backdropPuzzle, aniDictionary, textureCache)
    {
        _mainPuzzle = mainPuzzle;
        _moveRateX = moveRateX;
        _moveRateY = moveRateY;

        // Scale to stretch the backdrop to fill the main puzzle dimensions,
        // mirroring how C2DMapPuzzleObj centers the bitmap on the map.
        var scaleX = (float)_mainPuzzle.Width / _puzzle.Width;
        var scaleY = (float)_mainPuzzle.Height / _puzzle.Height;
        _scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1f);
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        // Mirrors the parallax calculation in CSceneLayer::Show():
        //
        //   posView (viewport center):
        //     posView = GetViewPos() + screenSize / 2
        //
        //   posCenter (map center):
        //     posCenter = mapSize * cellSize / 2
        //
        //   posOffset (viewport offset from map center):
        //     posOffset = posView - posCenter
        //
        //   posMyView (parallax-adjusted viewport center):
        //     posMyView = posCenter + posOffset * MoveRate / 100
        //
        //   infoShow.posViewPoint (parallax-adjusted viewport top-left):
        //     posMyView -= screenSize / 2

        // Map center in main world pixels.
        float mainMapCenterX = _mainPuzzle.Width / 2f;
        float mainMapCenterY = _mainPuzzle.Height / 2f;

        // Viewport center in main world pixels.
        float viewCenterX = screenRect.X + screenRect.Width / 2f;
        float viewCenterY = screenRect.Y + screenRect.Height / 2f;

        // Offset of the viewport center from the map center.
        float offsetX = viewCenterX - mainMapCenterX;
        float offsetY = viewCenterY - mainMapCenterY;

        // Parallax-adjusted viewport center.
        float parallaxCenterX = mainMapCenterX + offsetX * _moveRateX / 100f;
        float parallaxCenterY = mainMapCenterY + offsetY * _moveRateY / 100f;

        // Convert back to a top-left viewport position in main world pixels.
        float parallaxViewX = parallaxCenterX - screenRect.Width / 2f;
        float parallaxViewY = parallaxCenterY - screenRect.Height / 2f;

        // Map from main world pixels to backdrop-native pixels.
        // The _scaleMatrix stretches the backdrop to fill the main puzzle, so the
        // inverse scale converts the parallax viewport back to backdrop space for
        // tile selection (equivalent to how C2DMapPuzzleObj derives posShow from
        // posLU = posCenter - sizeBmp / 2 and then calls bmpPuzzle.Show(-posShow)).
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

        // Combine the backdrop scale with the camera transform, matching the
        // C++ approach where the bitmap is stretched to cover the full map area.
        var combinedTransform = _scaleMatrix * transformMatrix;

        base.Draw(spriteBatch, combinedTransform);
    }
}