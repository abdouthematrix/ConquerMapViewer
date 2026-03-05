namespace ConquerMapViewer.Rendering.Primitives;

/// <summary>
/// Efficiently builds and renders isometric cell outlines using vertex batching
/// </summary>
public sealed class CellVertexBuilder : IDisposable
{
    private const int VERTICES_PER_CELL = 8; // 4 lines * 2 vertices per line
    private const int INITIAL_CAPACITY = 1024;

    private readonly Vector2[] _cellPoints;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;

    private VertexPositionColor[] _vertexArray;
    private int _vertexCount;
    private DynamicVertexBuffer? _vertexBuffer;
    private int _vertexBufferCapacity;

    public int PrimitiveCount { get; private set; }

    public CellVertexBuilder(Vector2[] cellPoints, GraphicsDevice graphicsDevice)
    {
        _cellPoints = cellPoints;
        _graphicsDevice = graphicsDevice;
        _vertexArray = new VertexPositionColor[INITIAL_CAPACITY];
        _vertexBufferCapacity = 0;

        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            View = Matrix.Identity
        };

        UpdateProjection(graphicsDevice.Viewport);
    }

    public void UpdateProjection(Viewport viewport)
    {
        _effect.Projection = Matrix.CreateOrthographicOffCenter(
            0, viewport.Width, viewport.Height, 0, 0, 1);
    }

    public void Begin(int estimatedCellCount = 0)
    {
        _vertexCount = 0;
        PrimitiveCount = 0;

        // Ensure array capacity
        if (estimatedCellCount > 0)
        {
            var requiredCapacity = estimatedCellCount * VERTICES_PER_CELL;
            if (_vertexArray.Length < requiredCapacity)
            {
                var newCapacity = Math.Max(requiredCapacity, _vertexArray.Length * 2);
                Array.Resize(ref _vertexArray, newCapacity);
            }
        }
    }

    public void AddCell(Vector2 location, Color color)
    {
        // Ensure capacity for 8 more vertices
        if (_vertexCount + VERTICES_PER_CELL > _vertexArray.Length)
        {
            Array.Resize(ref _vertexArray, _vertexArray.Length * 2);
        }

        // Add 4 lines forming the cell outline
        AddLineInternal(location, _cellPoints[0], _cellPoints[1], color);
        AddLineInternal(location, _cellPoints[1], _cellPoints[2], color);
        AddLineInternal(location, _cellPoints[2], _cellPoints[3], color);
        AddLineInternal(location, _cellPoints[3], _cellPoints[0], color);

        PrimitiveCount += 4;
    }

    private void AddLineInternal(Vector2 location, Vector2 start, Vector2 end, Color color)
    {
        _vertexArray[_vertexCount++] = new VertexPositionColor(
            new Vector3(location.X + start.X, location.Y + start.Y, 0),
            color
        );
        _vertexArray[_vertexCount++] = new VertexPositionColor(
            new Vector3(location.X + end.X, location.Y + end.Y, 0),
            color
        );
    }

    public void End()
    {
        if (_vertexCount == 0)
            return;

        // Reuse or create dynamic vertex buffer
        if (_vertexBuffer == null || _vertexBufferCapacity < _vertexCount)
        {
            _vertexBuffer?.Dispose();

            // Allocate with some extra capacity to avoid frequent recreations
            _vertexBufferCapacity = Math.Max(_vertexCount, (_vertexBufferCapacity * 3) / 2);
            _vertexBuffer = new DynamicVertexBuffer(
                _graphicsDevice,
                typeof(VertexPositionColor),
                _vertexBufferCapacity,
                BufferUsage.WriteOnly
            );
        }

        // Upload vertex data with SetDataOptions.Discard for best performance
        _vertexBuffer.SetData(_vertexArray, 0, _vertexCount, SetDataOptions.Discard);
    }

    public void Draw(Matrix transformMatrix)
    {
        if (_vertexBuffer == null || PrimitiveCount == 0)
            return;

        _effect.World = transformMatrix;
        _effect.CurrentTechnique.Passes[0].Apply();
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, PrimitiveCount);
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _effect?.Dispose();
    }
}