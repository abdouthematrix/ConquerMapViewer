using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Rendering.Coordinates;
using ConquerMapViewer.Rendering.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Windows;

namespace ConquerMapViewer.Rendering.Drawing;

public sealed class MapCellDrawingComponent : BaseDrawingComponent
{
    private readonly MapCellCollection _cells;
    private readonly IsometricCoordinateSystem _coordinateSystem;
    private readonly CellVertexBuilder _vertexBuilder;

    public MapCellDrawingComponent(
        MapCellCollection cells,
        IsometricCoordinateSystem coordinateSystem,
        GraphicsDevice graphicsDevice)
    {
        _cells = cells;
        _coordinateSystem = coordinateSystem;
        _vertexBuilder = new CellVertexBuilder(coordinateSystem.CellPoints, graphicsDevice);
    }

    public override void UpdateScreen(Rectangle screenRect)
    {
        _vertexBuilder.Begin();

        var numCellsWidth = screenRect.Size.X / 64 + 2;
        var numCellHeight = screenRect.Size.X / 32 + 2;
        var xOffset = screenRect.X % 64 + 32;
        var yOffset = screenRect.Y % 32;
        var drawX = -xOffset;
        var drawY = -yOffset;

        for (var x = 0; x < numCellsWidth * 2; x++)
        {
            for (var y = 0; y < numCellHeight; y++)
            {
                var screenPos = new Point(
                    (x * 32 + drawX) + screenRect.Location.X + 32,
                    (y * 32 + (drawY - (x % 2) * 16)) + screenRect.Location.Y + 16
                );

                var mapCoord = _coordinateSystem.ScreenToMap(screenPos);

                if (mapCoord.X >= 0 && mapCoord.X < _cells.CollectionSize.Width &&
                    mapCoord.Y >= 0 && mapCoord.Y < _cells.CollectionSize.Height)
                {
                    var cell = _cells[(int)mapCoord.X, (int)mapCoord.Y];
                    var cellPos = new Vector2(x * 32 + drawX, y * 32 + (drawY - (x % 2) * 16));
                    _vertexBuilder.AddCell(cellPos, cell.AccessColor);
                }
            }
        }

        _vertexBuilder.End();
    }

    public override void Draw(SpriteBatch spriteBatch, Matrix transformMatrix)
    {
        _vertexBuilder.Draw(transformMatrix);
    }

    public void Dispose()
    {
        _vertexBuilder?.Dispose();
    }
}
