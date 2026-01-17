namespace ConquerMapViewer.Infrastructure.Graphics;
public static class TGAHelper
{
    public static Texture2D LoadFromStream(Stream stream, GraphicsDevice device)
    {
        return Load(device, stream);
    }
    public static Texture2D Load(GraphicsDevice device, string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            return Load(device, fs);
    }
    public static Texture2D Load(GraphicsDevice device, Stream fs)
    {
        using (BinaryReader reader = new BinaryReader(fs))
            return Load(device, reader);
    }

    public static Texture2D Load(GraphicsDevice device, BinaryReader reader)
    {
        // Read TGA Header
        byte idLength = reader.ReadByte();
        byte colorMapType = reader.ReadByte();
        byte imageType = reader.ReadByte();

        // Color map specification
        ushort colorMapStart = reader.ReadUInt16();
        ushort colorMapLength = reader.ReadUInt16();
        byte colorMapDepth = reader.ReadByte();

        // Image specification
        ushort xOrigin = reader.ReadUInt16();
        ushort yOrigin = reader.ReadUInt16();
        ushort width = reader.ReadUInt16();
        ushort height = reader.ReadUInt16();
        byte bitsPerPixel = reader.ReadByte();
        byte imageDescriptor = reader.ReadByte();

        // Skip image ID
        if (idLength > 0)
            reader.ReadBytes(idLength);

        // Only support uncompressed true-color images (type 2) and uncompressed grayscale (type 3)
        if (imageType != 2 && imageType != 3 && imageType != 10)
            throw new NotSupportedException($"TGA image type {imageType} not supported");

        // Read pixel data
        int pixelCount = width * height;
        Color[] pixels = new Color[pixelCount];

        if (imageType == 2) // Uncompressed RGB/RGBA
        {
            int bytesPerPixel = bitsPerPixel / 8;

            for (int i = 0; i < pixelCount; i++)
            {
                if (bytesPerPixel == 3)
                {
                    byte b = reader.ReadByte();
                    byte g = reader.ReadByte();
                    byte r = reader.ReadByte();
                    pixels[i] = new Color(r, g, b, (byte)255);
                }
                else if (bytesPerPixel == 4)
                {
                    byte b = reader.ReadByte();
                    byte g = reader.ReadByte();
                    byte r = reader.ReadByte();
                    byte a = reader.ReadByte();
                    pixels[i] = new Color(r, g, b, a);
                }
            }
        }
        else if (imageType == 10) // RLE compressed RGB/RGBA
        {
            int bytesPerPixel = bitsPerPixel / 8;
            int pixelIndex = 0;

            while (pixelIndex < pixelCount)
            {
                byte packetHeader = reader.ReadByte();
                int packetSize = (packetHeader & 0x7F) + 1;
                bool isRLE = (packetHeader & 0x80) != 0;

                if (isRLE)
                {
                    // RLE packet
                    Color pixel;
                    if (bytesPerPixel == 3)
                    {
                        byte b = reader.ReadByte();
                        byte g = reader.ReadByte();
                        byte r = reader.ReadByte();
                        pixel = new Color(r, g, b, (byte)255);
                    }
                    else
                    {
                        byte b = reader.ReadByte();
                        byte g = reader.ReadByte();
                        byte r = reader.ReadByte();
                        byte a = reader.ReadByte();
                        pixel = new Color(r, g, b, a);
                    }

                    for (int i = 0; i < packetSize && pixelIndex < pixelCount; i++)
                    {
                        pixels[pixelIndex++] = pixel;
                    }
                }
                else
                {
                    // Raw packet
                    for (int i = 0; i < packetSize && pixelIndex < pixelCount; i++)
                    {
                        if (bytesPerPixel == 3)
                        {
                            byte b = reader.ReadByte();
                            byte g = reader.ReadByte();
                            byte r = reader.ReadByte();
                            pixels[pixelIndex++] = new Color(r, g, b, (byte)255);
                        }
                        else
                        {
                            byte b = reader.ReadByte();
                            byte g = reader.ReadByte();
                            byte r = reader.ReadByte();
                            byte a = reader.ReadByte();
                            pixels[pixelIndex++] = new Color(r, g, b, a);
                        }
                    }
                }
            }
        }

        // Check if image needs to be flipped
        bool flipVertically = (imageDescriptor & 0x20) == 0;

        if (flipVertically)
        {
            FlipVertical(pixels, width, height);
        }

        // Create and populate texture
        Texture2D texture = new Texture2D(device, width, height);
        texture.SetData(pixels);

        return texture;
    }

    private static void FlipVertical(Color[] pixels, int width, int height)
    {
        Color[] temp = new Color[width];

        for (int y = 0; y < height / 2; y++)
        {
            int topRow = y * width;
            int bottomRow = (height - 1 - y) * width;

            // Copy top row to temp
            Array.Copy(pixels, topRow, temp, 0, width);

            // Copy bottom row to top
            Array.Copy(pixels, bottomRow, pixels, topRow, width);

            // Copy temp to bottom
            Array.Copy(temp, 0, pixels, bottomRow, width);
        }
    }
}
