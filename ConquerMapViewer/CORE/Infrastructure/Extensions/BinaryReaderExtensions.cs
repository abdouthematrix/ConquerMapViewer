

namespace ConquerMapViewer.Infrastructure.Extensions;

public static class BinaryReaderExtensions
{
    public static string ReadASCIIString(this BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length);
        var nullIndex = Array.IndexOf(bytes, (byte)0);
        return Encoding.ASCII.GetString(bytes, 0, nullIndex >= 0 ? nullIndex : length);
    }

    public static MapPoint ReadPoint(this BinaryReader reader) =>
        new(reader.ReadInt32(), reader.ReadInt32());

    public static MapSize ReadSize(this BinaryReader reader) =>
        new(reader.ReadInt32(), reader.ReadInt32());
}
