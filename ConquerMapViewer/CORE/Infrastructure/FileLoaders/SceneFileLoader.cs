namespace ConquerMapViewer.Infrastructure.FileLoaders;

public sealed class SceneFileLoader : ISceneFileLoader
{
    public Scene Load(Stream stream)
    {
        using var reader = new BinaryReader(stream);

        var scene = new Scene
        {
            SceneParts = new List<ScenePart>()
        };

        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            ScenePart scenePart = new ScenePart();
            scenePart.AniPath = reader.ReadASCIIString(256);
            scenePart.AniName = reader.ReadASCIIString(64);

            // FIXED: First ReadPoint is m_posOffset (ImageOffset in pixels)
            scenePart.ImageOffset = reader.ReadPoint();

            scenePart.Interval = reader.ReadInt32();
            scenePart.Size = reader.ReadSize();
            scenePart.Thick = reader.ReadInt32();

            // FIXED: Second ReadPoint is m_posSceneOffset (Location in cells)
            scenePart.Location = reader.ReadPoint();

            scenePart.Height = reader.ReadInt32();
            scenePart.Cells = new MapCell[scenePart.Size.Width, scenePart.Size.Height];

            for (var j = 0; j < scenePart.Size.Height; j++)
            {
                for (var k = 0; k < scenePart.Size.Width; k++)
                {
                    scenePart.Cells[k, j] = new MapCell
                    {
                        Access = (MapCellAccessType)reader.ReadInt32(),
                        Surface = (short)reader.ReadInt32(),
                        Height = (short)reader.ReadInt32()
                    };
                }
            }

            scene.SceneParts.Add(scenePart);
        }

        return scene;
    }
}