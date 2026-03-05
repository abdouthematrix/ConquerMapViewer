namespace ConquerMapViewer.Rendering.Drawing;

public class PuzzleDrawingComponent : BaseDrawingComponent
{
    private record struct ScreenTile(Vector2 Location, Texture2D Texture);

    protected readonly Puzzle _puzzle;
    protected readonly IAniDictionary _aniDictionary;
    protected readonly TextureCache _textureCache;
    private readonly List<ScreenTile> _visibleTiles = new();

    private const int EXTRA_TILES = 2;

    public PuzzleDrawingComponent(
        Puzzle puzzle,
        IAniDictionary aniDictionary,
        TextureCache textureCache)
    {
        _puzzle = puzzle;
        _aniDictionary = aniDictionary;
        _textureCache = textureCache;
        _aniDictionary.Add(_puzzle.AniPath);
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        _visibleTiles.Clear();

        if (!Enabled)
            return;

        var numPiecesX = Math.Min(screenRect.Width / _puzzle.TileSize + EXTRA_TILES, _puzzle.HorizontalTiles);
        var numPiecesY = Math.Min(screenRect.Height / _puzzle.TileSize + EXTRA_TILES, _puzzle.VerticalTiles);
        var startPieceX = screenRect.X / _puzzle.TileSize;
        var startPieceY = screenRect.Y / _puzzle.TileSize;
        var offsetX = -screenRect.X % _puzzle.TileSize;
        var offsetY = -screenRect.Y % _puzzle.TileSize;

        for (var x = startPieceX; x < startPieceX + numPiecesX; x++)
        {
            var drawX = offsetX + (x - startPieceX) * _puzzle.TileSize;
            
            for (var y = startPieceY; y < startPieceY + numPiecesY; y++)
            {
                if (x >= 0 && x < _puzzle.HorizontalTiles && y >= 0 && y < _puzzle.VerticalTiles)
                {
                    var drawY = offsetY + (y - startPieceY) * _puzzle.TileSize;
                    LoadTile(_puzzle.Tiles[x, y], new Vector2(drawX, drawY));
                }
            }
        }
    }

    protected virtual void LoadTile(short tileId, Vector2 location)
    {
        if (tileId == -1)
            return;

        var key = $"Puzzle{tileId}";
        if (!_aniDictionary.TryGetFrames(_puzzle.AniPath, key, out var frames) || frames.Count == 0)
            return;

        var texture = _textureCache.GetOrLoad(frames[0]);
        _visibleTiles.Add(new ScreenTile(location, texture));
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        if (!Enabled)
            return;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);
        
        foreach (var tile in _visibleTiles)
        {
            spriteBatch.Draw(tile.Texture, tile.Location, Color.White);
        }
        
        spriteBatch.End();
    }

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _visibleTiles.Clear();
                // Note: Don't dispose textures as they're managed by TextureCache
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
