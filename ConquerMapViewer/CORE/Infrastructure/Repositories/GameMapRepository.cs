using System.Text;
using ConquerMapViewer.Core.Domain.Entities;
using ConquerMapViewer.Core.Interfaces;

namespace ConquerMapViewer.Infrastructure.Repositories;

public sealed class GameMapRepository : IGameMapRepository
{
    private readonly Dictionary<int, GameMap> _maps = new();

    public GameMapRepository(string gameMapFilePath)
    {
        if (!File.Exists(gameMapFilePath))
            return;

        using var stream = new FileStream(gameMapFilePath, FileMode.Open);
        using var reader = new BinaryReader(stream);

        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            var map = new GameMap
            {
                Id = reader.ReadInt32(),
                Path = ReadAsciiString(reader, reader.ReadInt32()),
                TileSize = reader.ReadInt32()
            };
            if (!_maps.ContainsKey(map.Id))
                _maps.Add(map.Id, map);
        }
    }

    public IReadOnlyDictionary<int, GameMap> GetAllMaps() => _maps;

    public GameMap? GetMapById(int mapId) =>
        _maps.TryGetValue(mapId, out var map) ? map : null;

    public GameMap? GetMapByName(string name)
    {
        foreach (var map in _maps.Values)
        {
            if (Path.GetFileNameWithoutExtension(map.Path).Equals(name, StringComparison.OrdinalIgnoreCase))
                return map;
        }
        return null;
    }

    private static string ReadAsciiString(BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length);
        return Encoding.ASCII.GetString(bytes);
    }
}
