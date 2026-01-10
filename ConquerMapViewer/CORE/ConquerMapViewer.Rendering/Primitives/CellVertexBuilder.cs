using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.Rendering.Primitives;

public sealed class CellVertexBuilder : IDisposable
{
    private readonly Vector2[] _cellPoints;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;
    private List<VertexPositionColor> _vertices = new();
    private VertexBuffer? _vertexBuffer;

    public int PrimitiveCount { get; private set; }

    public CellVertexBuilder(Vector2[] cellPoints, GraphicsDevice graphicsDevice)
    {
        _cellPoints = cellPoints;
        _graphicsDevice = graphicsDevice;
        _effect = new BasicEffect(graphicsDevice);
        // Set up orthographic projection for 2D rendering
        var viewport = graphicsDevice.Viewport;
        _effect.Projection = Matrix.CreateOrthographicOffCenter(
            0, viewport.Width, viewport.Height, 0, 0, 1);
        _effect.View = Matrix.Identity;
        _effect.VertexColorEnabled = true;
    }

    public void Begin()
    {
        _vertexBuffer?.Dispose();
        _vertices.Clear();
        PrimitiveCount = 0;
    }

    public void AddCell(Vector2 location, Color color)
    {
        AddLine(location, _cellPoints[0], _cellPoints[1], color);
        AddLine(location, _cellPoints[1], _cellPoints[2], color);
        AddLine(location, _cellPoints[2], _cellPoints[3], color);
        AddLine(location, _cellPoints[3], _cellPoints[0], color);

        AddLine(location, _cellPoints[4], _cellPoints[5], color);
        AddLine(location, _cellPoints[5], _cellPoints[6], color);
        AddLine(location, _cellPoints[6], _cellPoints[7], color);
        AddLine(location, _cellPoints[7], _cellPoints[4], color);

        AddLine(location, _cellPoints[8], _cellPoints[9], color);
        AddLine(location, _cellPoints[9], _cellPoints[10], color);
        AddLine(location, _cellPoints[10], _cellPoints[11], color);
        AddLine(location, _cellPoints[11], _cellPoints[8], color);

        PrimitiveCount += 12;
    }

    private void AddLine(Vector2 location, Vector2 start, Vector2 end, Color color)
    {
        _vertices.Add(new VertexPositionColor(
            new Vector3(location.X + start.X, location.Y + start.Y, 0),
            color
        ));
        _vertices.Add(new VertexPositionColor(
            new Vector3(location.X + end.X, location.Y + end.Y, 0),
            color
        ));
    }

    public void End()
    {
        if (_vertices.Count > 0)
        {
            _vertexBuffer = new VertexBuffer(
                _graphicsDevice,
                typeof(VertexPositionColor),
                _vertices.Count,
                BufferUsage.WriteOnly
            );
            _vertexBuffer.SetData(_vertices.ToArray());
        }
    }

    public void Draw(Matrix transformMatrix)
    {
        if (_vertexBuffer != null && PrimitiveCount > 0)
        {
            _effect.World = transformMatrix;
            _effect.CurrentTechnique.Passes[0].Apply();            
            _graphicsDevice.SetVertexBuffer(_vertexBuffer);
            _graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, PrimitiveCount);
        }
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _effect?.Dispose();
    }
}
