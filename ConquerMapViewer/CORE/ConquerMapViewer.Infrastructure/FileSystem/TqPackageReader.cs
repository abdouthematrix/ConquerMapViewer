namespace ConquerMapViewer.Infrastructure.FileSystem;

public sealed class TqPackageReader : IPackageReader
{
    private readonly Dictionary<string, IPackageReader> _packages = new();
    private readonly string _conquerDirectory;

    public TqPackageReader(string conquerDirectory)
    {
        _conquerDirectory = conquerDirectory;
        AddPackage("c3.wdf");
        AddPackage("data.wdf");
    }

    public void AddPackage(string fileName)
    {
        var fullPath = Path.Combine(_conquerDirectory, fileName);
        if (!File.Exists(fullPath))
            return;

        var parts = fileName.Split('.');
        var extension = parts[1].ToLowerInvariant();
        var key = parts[0].ToLowerInvariant();

        switch (extension)
        {
            case "wdf":
                _packages.Add(key, new WdfPackageReader(fullPath));
                break;
        }
    }

    public Stream LoadFile(string fileName)
    {
        if (File.Exists(fileName))
        {
            return LoadFromFileSystem(fileName);
        }

        var fullPath = Path.Combine(_conquerDirectory, fileName);
        if (File.Exists(fullPath))
        {
            return LoadFromFileSystem(fullPath);
        }

        var packageKey = fileName.Split('/', '\\')[0];
        if (_packages.TryGetValue(packageKey, out var package))
        {
            return package.LoadFile(fileName);
        }

        throw new FileNotFoundException($"File not found: {fileName}");
    }

    private static Stream LoadFromFileSystem(string path)
    {
        if (Path.GetExtension(path).ToLowerInvariant() == ".7z")
        {
            using var archive = new ArchiveFile(path);
            var dmapEntry = archive.Entries.FirstOrDefault(e =>
                Path.GetExtension(e.FileName).ToLowerInvariant() == ".dmap");

            if (dmapEntry != null)
            {
                var ms = new MemoryStream();
                dmapEntry.Extract(ms);
                ms.Position = 0;
                return ms;
            }
        }

        using var fs = new FileStream(path, FileMode.Open);
        var buffer = new byte[fs.Length];
        fs.Read(buffer, 0, buffer.Length);
        return new MemoryStream(buffer);
    }

    public void Dispose()
    {
        foreach (var package in _packages.Values)
        {
            package.Dispose();
        }
        _packages.Clear();
    }
}
