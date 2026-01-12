namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class MapCell
{
    public MapCellAccessType Access { get; set; }
    public short Surface { get; set; }
    public short Height { get; set; }

    public Color AccessColor => Access switch
    {
        MapCellAccessType.Accessible => Color.Green,
        MapCellAccessType.Inaccessible => Color.Black,
        MapCellAccessType.Portal => Color.Blue,
        MapCellAccessType.Terrain => Color.Yellow,
        _ => Color.White
    };
}
