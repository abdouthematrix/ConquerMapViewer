namespace ConquerMono.Map.FileLoaders;

using IniSections = Dictionary<string, Dictionary<string, string>>;

// ─────────────────────────────────────────────────────────────────────────────
// OtherDataFileLoader
//
// Parses the ".OtherData" named sub-stream from a .dmap container.
// Format: INI-style plain text (sections [Name], key=value pairs).
// ─────────────────────────────────────────────────────────────────────────────
public class OtherDataFileLoader : IOtherDataFileLoader
{
    public MapOtherData Load(Stream stream)
    {
        var result = new MapOtherData();
        var sections = ParseIni(stream);

        if (!sections.TryGetValue("Header", out var hdr)) return result;

        int sceneLayers = Get(hdr, "SenceLayerAmount", 0);
        int terrainLayers = Get(hdr, "TerrainLayerAmount", 0);
        int interactiveLayers = Get(hdr, "InteractiveLayerAmount", 0);

        for (int i = 0; i < sceneLayers; i++) TryLoadSceneLayer(sections, i, result);
        for (int i = 0; i < terrainLayers; i++) TryLoadTerrainLayer(sections, i, result);
        for (int i = 0; i < interactiveLayers; i++) TryLoadInteractiveLayer(sections, i, result);

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
    // FIX: PicSizes are loaded independently — [TerrainLayer{i}] need not exist.
    // Many maps ship only [TerrainLayerPicSize{i}] with no main section.
    private void TryLoadTerrainLayer(IniSections s, int i, MapOtherData r)
    {
        bool hasMain = s.TryGetValue($"TerrainLayer{i}", out var kv);
        bool hasPicSize = s.TryGetValue($"TerrainLayerPicSize{i}", out var pv);
        if (!hasMain && !hasPicSize) return;

        var d = hasMain
            ? new MapOtherData.TerrainLayerData
            {
                Alpha = Get(kv, "Alpha", 0xFF),
                Light = Get(kv, "Light", 0x80),
                Red = Get(kv, "Red", 0xFF),
                Green = Get(kv, "Green", 0xFF),
                Blue = Get(kv, "Blue", 0xFF),
            }
            : new MapOtherData.TerrainLayerData();

        if (hasMain)
            for (int j = 0; kv.ContainsKey($"MapObjIndex{j}"); j++)
                d.ObjRefs.Add(Get(kv, $"MapObjIndex{j}", -1));

        if (hasPicSize)
            ReadPicSizes(pv, d.PicSizes);

        r.TerrainLayers[i] = d;
    }

    // ── InteractiveLayer  (FUN_00d21e87) ─────────────────────────────────────
    // FIX 1: gate used hdr.ContainsKey("InteractiveLayer{i}") but the Header
    //        section stores "InteractiveLayerAmount=N", not per-index keys.
    //        Now reads the count and checks i against it.
    // FIX 2: PicSizes are loaded independently — [InteractiveLayer{i}] need
    //        not exist; [InteractiveLayerPicSize{i}] alone is sufficient.
    private void TryLoadInteractiveLayer(IniSections s, int i, MapOtherData r)
    {
        if (!s.TryGetValue("Header", out var hdr)) return;
        if (Get(hdr, "InteractiveLayerAmount", 0) <= i) return;

        bool hasMain = s.TryGetValue($"InteractiveLayer{i}", out var kv);
        bool hasPicSize = s.TryGetValue($"InteractiveLayerPicSize{i}", out var pv);
        if (!hasMain && !hasPicSize) return;

        var d = hasMain
            ? new MapOtherData.InteractiveLayerData
            {
                Width = Get(kv, "Width", 0),
                Height = Get(kv, "Height", 0),
            }
            : new MapOtherData.InteractiveLayerData();

        if (hasMain)
            for (int j = 0; kv.ContainsKey($"MapObjIndex{j}"); j++)
                d.ObjRefs.Add(Get(kv, $"MapObjIndex{j}", -1));

        if (hasPicSize)
            ReadPicSizes(pv, d.PicSizes);

        r.InteractiveLayers[i] = d;
    }

    private void ReadPicSizes(Dictionary<string, string> kv, List<(int, int, int)> list)
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