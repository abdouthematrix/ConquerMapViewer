using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.Infrastructure.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.Rendering.Drawing;

public class PuzzleDrawingComponent : BaseDrawingComponent
{
    private record struct ScreenTexture(Vector2 Location, Texture2D Texture);

    protected readonly Puzzle _puzzle;
    private readonly IAniDictionary _aniDictionary;
    private readonly IPackageReader _packageReader;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<ScreenTexture> _textures = new();

    public PuzzleDrawingComponent(
        Puzzle puzzle,
        IAniDictionary aniDictionary,
        IPackageReader packageReader,
        GraphicsDevice graphicsDevice)
    {
        _puzzle = puzzle;
        _aniDictionary = aniDictionary;
        _packageReader = packageReader;
        _graphicsDevice = graphicsDevice;
        _aniDictionary.Add(_puzzle.AniPath);
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        DisposeTextures();

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
                    LoadTile(_puzzle.Tiles[x, y], new Vector2(drawX, drawY));
                    drawY += _puzzle.TileSize;
                }
            }
            drawX += _puzzle.TileSize;
            drawY = offsetY;
        }
    }

    protected virtual void LoadTile(short tileId, Vector2 destRect)
    {
        if (tileId == -1)
            return;

        var frames = _aniDictionary[_puzzle.AniPath, $"Puzzle{tileId}"];
        if (frames.Count == 0)
            return;

        using var stream = _packageReader.LoadFile(frames[0]);
        var extension = Path.GetExtension(frames[0]).ToLowerInvariant();

        Texture2D texture = extension == ".dds"
            ? DDSHelper.LoadFromStream(stream, _graphicsDevice)
            : Texture2D.FromStream(_graphicsDevice, stream);

        _textures.Add(new ScreenTexture(destRect, texture));
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, transformMatrix);
        foreach (var texture in _textures)
        {
            spriteBatch.Draw(texture.Texture, texture.Location, Color.White);
        }
        spriteBatch.End();
    }

    protected void DisposeTextures()
    {
        foreach (var texture in _textures)
        {
            texture.Texture?.Dispose();
        }
        _textures.Clear();
    }

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DisposeTextures();
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
