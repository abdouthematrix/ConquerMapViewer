using ConquerMapViewer.Core.Domain.Entities;

namespace ConquerMapViewer.Core.Interfaces;

public interface IGameMapRepository
{
    IReadOnlyDictionary<int, GameMap> GetAllMaps();
    GameMap? GetMapById(int mapId);
    GameMap? GetMapByName(string name);
}
