using ConquerMapViewer.Core.Interfaces;
using ConquerMapViewer.Infrastructure.Animation;
using Microsoft.Xna.Framework.Graphics;

namespace ConquerMapViewer.Rendering.Shared;

/// <summary>
/// Caches loaded textures to avoid repeated disk I/O and texture creation
/// </summary>
public sealed class TextureCache : IDisposable
{
    private readonly Dictionary<string, Texture2D> _cache = new();
    private readonly IPackageReader _packageReader;
    private readonly GraphicsDevice _graphicsDevice;

    public TextureCache(IPackageReader packageReader, GraphicsDevice graphicsDevice)
    {
        _packageReader = packageReader;
        _graphicsDevice = graphicsDevice;
    }

    public Texture2D GetOrLoad(string path)
    {
        if (_cache.TryGetValue(path, out var cached))
            return cached;

        using var stream = _packageReader.LoadFile(path);
        var extension = Path.GetExtension(path).ToLowerInvariant();

        Texture2D texture = extension == ".dds"
            ? DDSHelper.LoadFromStream(stream, _graphicsDevice)
            : Texture2D.FromStream(_graphicsDevice, stream);

        _cache[path] = texture;
        return texture;
    }

    public void Clear()
    {
        foreach (var texture in _cache.Values)
            texture?.Dispose();
        _cache.Clear();
    }

    public void Dispose()
    {
        Clear();
    }
}
