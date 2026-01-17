namespace ConquerMapViewer.Core.Domain.Entities;

public sealed class MapCell
{
    public MapCellAccessType Access { get; set; }
    public short Surface { get; set; }
    public short Height { get; set; }

    public Color AccessColor => Access switch
    {
        MapCellAccessType.Accessible => Color.Transparent,
        MapCellAccessType.Inaccessible => Color.Red,
        MapCellAccessType.Portal => Color.Blue,
        MapCellAccessType.Terrain => Color.Yellow,
        MapCellAccessType.Scene => Color.Purple,
        MapCellAccessType.Backdrop => Color.Orange,
        MapCellAccessType.Effect => Color.Cyan,
        MapCellAccessType.Sound => Color.Magenta,
        _ => Color.White
    };
}
