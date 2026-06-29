namespace ConquerMono.Map.FileLoaders;

using IniSections = Dictionary<string, Dictionary<string, string>>;

// ─────────────────────────────────────────────────────────────────────────────
// MapOtherDataLoader
//
// Parses the ".OtherData" named sub-stream from a .dmap container.
// Format: INI-style plain text (sections [Name], key=value pairs).
// ─────────────────────────────────────────────────────────────────────────────
public class OtherDataFileLoader : IOtherDataFileLoader
{
    public MapOtherData Load(Stream stream, int layerCount)
    {
        var result = new MapOtherData();
        var sections = ParseIni(stream);

        for (int i = 0; i < layerCount; i++)
        {
            TryLoadSceneLayer(sections, i, result);
            TryLoadTerrainLayer(sections, i, result);
            TryLoadInteractiveLayer(sections, i, result);

            // Stop when no section of any type exists for this index
            if (!sections.ContainsKey($"SceneLayer{i}") &&
                !sections.ContainsKey($"TerrainLayer{i}") &&
                !sections.ContainsKey($"InteractiveLayer{i}") &&
                i > 0)
                break;
        }

        return result;
    }

    // ── SceneLayer  (FUN_00d223b9) ────────────────────────────────────────────
    private void TryLoadSceneLayer(IniSections s, int i, MapOtherData r)
    {
        if (!s.TryGetValue($"SceneLayer{i}", out var kv)) return;
        r.SceneLayers[i] = new MapOtherData.SceneLayerData
        {
            Alpha = Get(kv, "Alpha", 0xFF),
            Light = Get(kv, "Light", 0x80),
            Red = Get(kv, "Red", 0xFF),
            Green = Get(kv, "Green", 0xFF),
            Blue = Get(kv, "Blue", 0xFF),
            PuzzleAlpha = Get(kv, "PuzzleAlpha", 0xFF),
            PuzzleLight = Get(kv, "PuzzleLight", 0x80),
            PuzzleRed = Get(kv, "PuzzleRed", 0xFF),
            PuzzleGreen = Get(kv, "PuzzleGreen", 0xFF),
            PuzzleBlue = Get(kv, "PuzzleBlue", 0xFF),
        };
    }

    // ── TerrainLayer  (FUN_00d22686) ─────────────────────────────────────────
    private void TryLoadTerrainLayer(IniSections s, int i, MapOtherData r)
    {
        if (!s.TryGetValue($"TerrainLayer{i}", out var kv)) return;
        var d = new MapOtherData.TerrainLayerData
        {
            Alpha = Get(kv, "Alpha", 0xFF),
            Light = Get(kv, "Light", 0x80),
            Red = Get(kv, "Red", 0xFF),
            Green = Get(kv, "Green", 0xFF),
            Blue = Get(kv, "Blue", 0xFF),
        };
        for (int j = 0; kv.ContainsKey($"MapObjIndex{j}"); j++)
            d.ObjRefs.Add(Get(kv, $"MapObjIndex{j}", -1));

        if (s.TryGetValue($"TerrainLayerPicSize{i}", out var pv))
            ReadPicSizes(pv, d.PicSizes);

        r.TerrainLayers[i] = d;
    }

    // ── InteractiveLayer  (FUN_00d21e87) ─────────────────────────────────────
    private void TryLoadInteractiveLayer(IniSections s, int i, MapOtherData r)
    {
        if (!s.TryGetValue("Header", out var hdr)) return;
        if (!hdr.ContainsKey($"InteractiveLayer{i}")) return;

        if (!s.TryGetValue($"InteractiveLayer{i}", out var kv)) return;
        var d = new MapOtherData.InteractiveLayerData
        {
            Width = Get(kv, "Width", 0),
            Height = Get(kv, "Height", 0),
        };
        for (int j = 0; kv.ContainsKey($"MapObjIndex{j}"); j++)
            d.ObjRefs.Add(Get(kv, $"MapObjIndex{j}", -1));

        if (s.TryGetValue($"InteractiveLayerPicSize{i}", out var pv))
            ReadPicSizes(pv, d.PicSizes);

        r.InteractiveLayers[i] = d;
    }

    private void ReadPicSizes(Dictionary<string, string> kv,
        List<(int, int, int)> list)
    {
        for (int j = 0; kv.ContainsKey($"MapObjIndex{j}"); j++)
        {
            int idx = Get(kv, $"MapObjIndex{j}", -1);
            int w = Get(kv, $"Width{j}", 0);
            int h = Get(kv, $"Height{j}", 0);
            if (idx >= 0) list.Add((idx, w, h));
        }
    }

    // ── INI parser ────────────────────────────────────────────────────────────
    private static IniSections ParseIni(Stream stream)
    {
        var result = new IniSections(StringComparer.OrdinalIgnoreCase);
        var current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string currentName = string.Empty;

        using var reader = new StreamReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;

            if (line[0] == '[' && line[^1] == ']')
            {
                if (currentName.Length > 0) result[currentName] = current;
                currentName = line[1..^1].Trim();
                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq > 0) current[line[..eq].Trim()] = line[(eq + 1)..].Trim();
        }
        if (currentName.Length > 0) result[currentName] = current;
        return result;
    }

    private static int Get(Dictionary<string, string> kv, string key, int def)
        => kv.TryGetValue(key, out var v) && int.TryParse(v, out int n) ? n : def;
}