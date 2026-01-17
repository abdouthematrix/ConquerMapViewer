namespace ConquerMapViewer.Infrastructure.Graphics;

public static class DDSHelper
{
    public static int GetWidth(Stream stream)
    {
        stream.Seek(16, SeekOrigin.Begin);
        using var reader = new BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true);
        return reader.ReadInt32();
    }

    public static Texture2D LoadFromStream(Stream stream, GraphicsDevice device)
    {
       return Load(device, stream);
    }

    private const uint DDS_MAGIC = 0x20534444; // "DDS "
    private const uint DDSD_MIPMAPCOUNT = 0x00020000;
    private const uint DDPF_FOURCC = 0x00000004;
    private const uint DDPF_RGB = 0x00000040;
    private const uint DDPF_RGBA = 0x00000041;

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
        // Read and verify magic number
        uint magic = reader.ReadUInt32();
        if (magic != DDS_MAGIC)
            throw new InvalidDataException("Invalid DDS file - magic number mismatch");

        // Read DDS_HEADER
        uint headerSize = reader.ReadUInt32(); // Should be 124
        uint flags = reader.ReadUInt32();
        uint height = reader.ReadUInt32();
        uint width = reader.ReadUInt32();
        uint pitchOrLinearSize = reader.ReadUInt32();
        uint depth = reader.ReadUInt32();
        uint mipMapCount = reader.ReadUInt32();

        // Skip reserved1[11]
        reader.ReadBytes(44);

        // Read DDS_PIXELFORMAT
        uint pfSize = reader.ReadUInt32();
        uint pfFlags = reader.ReadUInt32();
        uint fourCC = reader.ReadUInt32();
        uint rgbBitCount = reader.ReadUInt32();
        uint rBitMask = reader.ReadUInt32();
        uint gBitMask = reader.ReadUInt32();
        uint bBitMask = reader.ReadUInt32();
        uint aBitMask = reader.ReadUInt32();

        // Read caps
        uint caps = reader.ReadUInt32();
        uint caps2 = reader.ReadUInt32();
        uint caps3 = reader.ReadUInt32();
        uint caps4 = reader.ReadUInt32();

        // Skip reserved2
        reader.ReadUInt32();

        // Determine format
        SurfaceFormat format = DetermineSurfaceFormat(pfFlags, fourCC, rgbBitCount, rBitMask, gBitMask, bBitMask, aBitMask);

        // Check if mipmaps are present
        bool hasMipmaps = (flags & DDSD_MIPMAPCOUNT) != 0 && mipMapCount > 1;
        int mipLevels = hasMipmaps ? (int)mipMapCount : 1;

        // Create texture
        Texture2D texture = new Texture2D(device, (int)width, (int)height, hasMipmaps, format);

        // Read texture data for each mip level
        int w = (int)width;
        int h = (int)height;

        for (int mipLevel = 0; mipLevel < mipLevels; mipLevel++)
        {
            int mipSize = CalculateMipSize(w, h, format);
            byte[] mipData = reader.ReadBytes(mipSize);

            texture.SetData(mipLevel, null, mipData, 0, mipSize);

            // Calculate next mip dimensions
            w = Math.Max(1, w / 2);
            h = Math.Max(1, h / 2);
        }

        return texture;
    }

    private static SurfaceFormat DetermineSurfaceFormat(uint pfFlags, uint fourCC, uint bitCount,
        uint rMask, uint gMask, uint bMask, uint aMask)
    {
        // Check for compressed formats (FourCC)
        if ((pfFlags & DDPF_FOURCC) != 0)
        {
            string fourCCStr = FourCCToString(fourCC);

            switch (fourCCStr)
            {
                case "DXT1":
                    return SurfaceFormat.Dxt1;
                case "DXT3":
                    return SurfaceFormat.Dxt3;
                case "DXT5":
                    return SurfaceFormat.Dxt5;
                default:
                    throw new NotSupportedException($"Unsupported DDS format: {fourCCStr}");
            }
        }

        // Check for uncompressed formats
        if ((pfFlags & DDPF_RGB) != 0)
        {
            if (bitCount == 32)
            {
                // Check for BGRA
                if (bMask == 0x000000FF && gMask == 0x0000FF00 &&
                    rMask == 0x00FF0000 && aMask == 0xFF000000)
                    return SurfaceFormat.Color;

                // Check for RGBA
                if (rMask == 0x000000FF && gMask == 0x0000FF00 &&
                    bMask == 0x00FF0000 && aMask == 0xFF000000)
                    return SurfaceFormat.Color; // Will need swizzling
            }
            else if (bitCount == 24)
            {
                return SurfaceFormat.Color; // Will convert to 32-bit
            }
        }

        return SurfaceFormat.Color; // Default fallback
    }

    private static int CalculateMipSize(int width, int height, SurfaceFormat format)
    {
        switch (format)
        {
            case SurfaceFormat.Dxt1:
            case SurfaceFormat.Dxt1a:
            case SurfaceFormat.Dxt1SRgb:
                // 4x4 blocks, 8 bytes per block
                return Math.Max(1, ((width + 3) / 4)) * Math.Max(1, ((height + 3) / 4)) * 8;

            case SurfaceFormat.Dxt3:
            case SurfaceFormat.Dxt3SRgb:
            case SurfaceFormat.Dxt5:
            case SurfaceFormat.Dxt5SRgb:
                // 4x4 blocks, 16 bytes per block
                return Math.Max(1, ((width + 3) / 4)) * Math.Max(1, ((height + 3) / 4)) * 16;

            case SurfaceFormat.Color:
            case SurfaceFormat.ColorSRgb:
                return width * height * 4;

            default:
                return width * height * 4;
        }
    }

    private static string FourCCToString(uint fourCC)
    {
        return new string(new char[]
        {
                (char)(fourCC & 0xFF),
                (char)((fourCC >> 8) & 0xFF),
                (char)((fourCC >> 16) & 0xFF),
                (char)((fourCC >> 24) & 0xFF)
        });
    }
}
