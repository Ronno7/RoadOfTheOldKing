using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using RoadOfTheOldKing.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.EditorTools
{
    public enum ElevationClass { Unclassified, Lower, Normal, Upper, Structure, Void }

    /// <summary>Editor guide data lives in ordinary Tilemaps, never in a runtime height system.</summary>
    public static class LowlandsElevationAuthoring
    {
        public const string AssetRoot = "Assets/Editor/SceneAuthoring/LowlandsElevation";
        public const string ElevationName = "Elevation - Lower Normal Upper";
        public const string TransitionsName = "Transitions - arrows point uphill";
        public const int RiseCells = 1;
        static readonly ElevationClass[] Classes = { ElevationClass.Lower, ElevationClass.Normal,
            ElevationClass.Upper, ElevationClass.Structure, ElevationClass.Void };
        static readonly Vector3Int[] Directions = { Vector3Int.up, Vector3Int.right,
            Vector3Int.down, Vector3Int.left };
        static readonly string[] DirectionNames = { "North", "East", "South", "West" };
        static readonly Dictionary<ElevationClass, Tile> ClassTiles = new Dictionary<ElevationClass, Tile>();
        static readonly Dictionary<Vector3Int, Tile> ArrowTiles = new Dictionary<Vector3Int, Tile>();

        public sealed class Guides { public Tilemap elevation, transitions; }
        public sealed class Report
        {
            public int cells, surfaces, structures, voids, unclassified, errors, warnings;
            public readonly List<string> messages = new List<string>();
            public bool IsValid { get { return errors == 0; } }
            public void Error(string message) { errors++; if (messages.Count < 80) messages.Add("ERROR: " + message); }
            public void Warning(string message) { warnings++; if (messages.Count < 80) messages.Add("NOTE: " + message); }
            public override string ToString() { return cells + " cells; " + surfaces + " surfaces; " + structures +
                " structure; " + voids + " void; " + errors + " errors; " + warnings + " notes.\n" + string.Join("\n", messages); }
        }

        public static bool TryHeight(ElevationClass value, out int height)
        {
            height = value == ElevationClass.Upper ? 1 : value == ElevationClass.Lower ? -1 : 0;
            return value == ElevationClass.Lower || value == ElevationClass.Normal || value == ElevationClass.Upper;
        }

        public static Tile TileFor(ElevationClass value)
        {
            if (value == ElevationClass.Unclassified) return null;
            Tile tile;
            if (!ClassTiles.TryGetValue(value, out tile) || tile == null)
                ClassTiles[value] = tile = AssetDatabase.LoadAssetAtPath<Tile>(AssetRoot + "/" + value + ".asset");
            return tile;
        }

        public static Tile TransitionTile(Vector3Int uphill)
        {
            int i = Array.IndexOf(Directions, uphill);
            if (i < 0) throw new ArgumentException("Use a cardinal, one-cell uphill direction.");
            Tile tile;
            if (!ArrowTiles.TryGetValue(uphill, out tile) || tile == null)
                ArrowTiles[uphill] = tile = AssetDatabase.LoadAssetAtPath<Tile>(AssetRoot + "/Up" + DirectionNames[i] + ".asset");
            return tile;
        }

        public static ElevationClass ReadClassification(Tilemap map, Vector3Int cell)
        {
            var tile = map.GetTile(cell);
            if (tile != null) foreach (var value in Classes) if (tile == TileFor(value)) return value;
            return ElevationClass.Unclassified;
        }

        public static bool ReadTransition(Tilemap map, Vector3Int cell, out Vector3Int uphill)
        {
            uphill = Vector3Int.zero;
            if (map == null) return false;
            var tile = map.GetTile(cell);
            if (tile == null) return false;
            for (int i = 0; i < Directions.Length; i++) if (tile == TransitionTile(Directions[i]))
            { uphill = Directions[i]; return true; }
            return false;
        }

        public static void SetClass(Tilemap map, Vector3Int cell, ElevationClass value)
        {
            RequireStopped();
            var tile = TileFor(value);
            if (value != ElevationClass.Unclassified && tile == null)
                throw new InvalidOperationException("Create elevation assets before painting guides.");
            map.SetTile(cell, tile);
        }

        public static Color GuideColor(ElevationClass value)
        {
            switch (value)
            {
                case ElevationClass.Lower: return new Color(.39f, .61f, .74f, .5f);
                case ElevationClass.Normal: return new Color(.50f, .70f, .43f, .5f);
                case ElevationClass.Upper: return new Color(.85f, .65f, .31f, .5f);
                case ElevationClass.Structure: return new Color(.63f, .38f, .40f, .5f);
                case ElevationClass.Void: return new Color(.23f, .25f, .27f, .55f);
                default: return Color.clear;
            }
        }

        public static string GuideLabel(ElevationClass value)
        {
            switch (value)
            {
                case ElevationClass.Lower: return "-1";
                case ElevationClass.Normal: return "0";
                case ElevationClass.Upper: return "+1";
                case ElevationClass.Structure: return "X";
                case ElevationClass.Void: return "V";
                default: return "?";
            }
        }

        public static Guides CreateGuides(Grid grid)
        {
            RequireStopped();
            if (grid == null) throw new ArgumentNullException("grid");
            EnsureAssets();
            var parent = grid.transform.Find("Authoring - elevation guides");
            if (parent == null)
            {
                var go = new GameObject("Authoring - elevation guides");
                Undo.RegisterCreatedObjectUndo(go, "Create elevation guides");
                go.transform.SetParent(grid.transform, false); go.tag = "EditorOnly"; parent = go.transform;
            }
            return new Guides { elevation = GuideMap(parent, ElevationName), transitions = GuideMap(parent, TransitionsName) };
        }

        static Tilemap GuideMap(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var map = existing.GetComponent<Tilemap>();
                if (map == null) throw new InvalidOperationException(name + " exists but is not a Tilemap.");
                return map;
            }
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            Undo.RegisterCreatedObjectUndo(go, "Create elevation guide");
            go.transform.SetParent(parent, false); go.tag = "EditorOnly";
            // The window draws Scene View-only guides. They can never cover gameplay.
            go.GetComponent<TilemapRenderer>().enabled = false;
            return go.GetComponent<Tilemap>();
        }

        public static Report Validate(Tilemap elevation, Tilemap transitions, BoundsInt area)
        {
            var report = new Report();
            if (!CheckMaps(elevation, transitions, area, report)) return report;
            var assets = Classes.ToDictionary(v => TileFor(v), v => v);
            var boundary = elevation.gameObject.scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Tilemap>(true))
                .FirstOrDefault(m => m.name == "Height boundaries");
            foreach (var p in area.allPositionsWithin)
            {
                report.cells++;
                ElevationClass kind;
                var tile = elevation.GetTile(p);
                if (!(tile is Tile) || !assets.TryGetValue((Tile)tile, out kind))
                { report.unclassified++; report.Error("Unclassified or foreign tile at " + p + "."); continue; }
                int height;
                if (TryHeight(kind, out height))
                {
                    report.surfaces++;
                    foreach (var direction in new[] { Vector3Int.right, Vector3Int.up })
                    {
                        int other;
                        if (TryHeight(ReadClassification(elevation, p + direction), out other) && Math.Abs(height - other) > 1 &&
                            !HasThinBoundary(boundary, p, height, p + direction, other))
                            report.Error("Touching Upper/Lower floors at " + p + "; classify their two-cell front face as Structure or use an explicit side/rear boundary.");
                    }
                }
                else if (kind == ElevationClass.Structure) report.structures++;
                else report.voids++;
                if (transitions == null || !transitions.HasTile(p)) continue;
                Vector3Int uphill;
                if (!ReadTransition(transitions, p, out uphill))
                { report.Error("Foreign transition tile at " + p + "."); continue; }
                if (kind != ElevationClass.Structure)
                    report.Error("Transition treads at " + p + " must replace Structure, not a horizontal floor.");
                Vector3Int previous;
                if (ReadTransition(transitions, p - uphill, out previous) && previous == uphill) continue;
                var end = p;
                int length = 1;
                Vector3Int next;
                while (length <= 32 && ReadTransition(transitions, end + uphill, out next) && next == uphill)
                { end += uphill; length++; }
                int low, high;
                if (length > 32 || !TryHeight(ReadClassification(elevation, p - uphill), out low) ||
                    !TryHeight(ReadClassification(elevation, end + uphill), out high) || high != low + 1)
                    report.Error("Uphill transition at " + p + " must join horizontal landings exactly one tier apart.");
                if (length != RiseCells) report.Warning("Transition at " + p + " is " + length + " cells; standard projected rise is " + RiseCells + ".");
            }
            return report;
        }

        static bool HasThinBoundary(Tilemap map, Vector3Int a, int ah, Vector3Int b, int bh)
        {
            if (map == null) return false;
            var high = ah > bh ? a : b;
            var downhill = (ah > bh ? b : a) - high;
            var tile = map.GetTile(high);
            int mask;
            if (tile == null || !tile.name.StartsWith("CliffCollision_", StringComparison.Ordinal) ||
                !int.TryParse(tile.name.Substring("CliffCollision_".Length), out mask)) return false;
            int bit = downhill == Vector3Int.up ? 1 : downhill == Vector3Int.right ? 2 :
                downhill == Vector3Int.left ? 4 : 0;
            return bit != 0 && (mask & bit) != 0;
        }

        static bool CheckMaps(Tilemap elevation, Tilemap transitions, BoundsInt area, Report report)
        {
            if (elevation == null) { report.Error("Assign the elevation guide."); return false; }
            if (Classes.Any(v => TileFor(v) == null)) { report.Error("Create elevation assets first."); return false; }
            if (area.size.z != 1 || area.position.z != 0 || area.size.x <= 0 || area.size.y <= 0 ||
                (long)area.size.x * area.size.y > 100000)
            { report.Error("Choose a non-empty XY region of at most 100,000 cells at Z=0."); return false; }
            if (!Aligned(elevation) || (transitions != null && (!Aligned(transitions) || !SameGrid(elevation, transitions))))
            { report.Error("Guides need the same unrotated unit grid and cell alignment."); return false; }
            return true;
        }

        static bool Aligned(Tilemap map)
        {
            return map.layoutGrid != null && Mathf.Approximately(map.layoutGrid.cellSize.x, 1) &&
                Mathf.Approximately(map.layoutGrid.cellSize.y, 1) &&
                Mathf.Approximately(map.layoutGrid.cellGap.x, 0) && Mathf.Approximately(map.layoutGrid.cellGap.y, 0) &&
                map.transform.rotation == Quaternion.identity &&
                map.transform.lossyScale == Vector3.one && map.orientation == Tilemap.Orientation.XY;
        }

        static bool SameGrid(Tilemap a, Tilemap b)
        { return a.layoutGrid == b.layoutGrid && a.CellToWorld(Vector3Int.zero) == b.CellToWorld(Vector3Int.zero); }

        public static void Export(Tilemap elevation, Tilemap transitions, string path)
        {
            RequireStopped();
            if (elevation == null) throw new ArgumentNullException("elevation");
            var preflight = new Report();
            if (!CheckMaps(elevation, transitions, elevation.cellBounds, preflight))
                throw new InvalidOperationException(preflight.ToString());
            if (elevation.CellToWorld(Vector3Int.zero) != Vector3.zero)
                throw new InvalidOperationException("The existing world-coordinate contract requires guide cell (0,0) at world (0,0).");
            var json = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
            var surfaces = new JArray(); var structures = new JArray(); var voids = new JArray();
            var bounds = elevation.cellBounds;
            int unclassified = 0;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                int x = bounds.xMin;
                while (x < bounds.xMax)
                {
                    var kind = ReadClassification(elevation, new Vector3Int(x, y));
                    int start = x++;
                    while (x < bounds.xMax && ReadClassification(elevation, new Vector3Int(x, y)) == kind) x++;
                    if (kind == ElevationClass.Unclassified) { unclassified += x - start; continue; }
                    var run = new JObject { ["y"] = y, ["xMin"] = start, ["xMax"] = x - 1 };
                    int height;
                    if (TryHeight(kind, out height)) { run["height"] = height; surfaces.Add(run); }
                    else if (kind == ElevationClass.Structure) structures.Add(run);
                    else voids.Add(run);
                }
            }
            var transitionRuns = new JArray();
            if (transitions != null) foreach (var p in transitions.cellBounds.allPositionsWithin)
            {
                Vector3Int direction, previous;
                if (!ReadTransition(transitions, p, out direction) ||
                    (ReadTransition(transitions, p - direction, out previous) && previous == direction)) continue;
                var end = p; Vector3Int next; int length = 1;
                while (ReadTransition(transitions, end + direction, out next) && next == direction && length < 1024)
                { end += direction; length++; }
                transitionRuns.Add(new JObject { ["x"] = p.x, ["y"] = p.y, ["dx"] = direction.x,
                    ["dy"] = direction.y, ["length"] = length });
            }
            json["schema"] = 2;
            json["geometrySource"] = "GreenLowlands scene editor-only Elevation and Transitions Tilemaps; exports are derived.";
            json.Remove("projectedRiseCells");
            json["projectedRisePerTierCells"] = RiseCells;
            json["guideBounds"] = new JObject { ["x"] = bounds.xMin, ["y"] = bounds.yMin,
                ["width"] = bounds.size.x, ["height"] = bounds.size.y };
            json["unclassifiedCells"] = unclassified;
            json["surfaceRuns"] = surfaces; json["structureRuns"] = structures;
            json["voidRuns"] = voids; json["transitionRuns"] = transitionRuns;
            File.WriteAllText(path, json.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
        }

        public static void EnsureAssets()
        {
            RequireStopped();
            Directory.CreateDirectory(AssetRoot);
            var assets = new List<Tile>();
            foreach (var kind in Classes) assets.Add(EnsureTile(kind.ToString(), GuideColor(kind), GuideLabel(kind)));
            for (int i = 0; i < Directions.Length; i++)
                assets.Add(EnsureTile("Up" + DirectionNames[i], new Color(.88f,.83f,.62f,1), new[] { "N", "E", "S", "W" }[i]));
            string palettePath = AssetRoot + "/LowlandsElevationPalette.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) == null)
            {
                var scratch = EditorSceneManager.NewPreviewScene();
                try
                {
                    var root = new GameObject("Lowlands elevation guide", typeof(Grid));
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scratch);
                    var map = TileKitAssets.AddMap(root.transform, "0 +1 -1 X structure V void; N E S W uphill", 0);
                    for (int i = 0; i < assets.Count; i++) map.SetTile(new Vector3Int(i * 2, 0), assets[i]);
                    TileKitAssets.SavePalette(root, palettePath);
                }
                finally { EditorSceneManager.ClosePreviewScene(scratch); }
            }
            AssetDatabase.SaveAssets();
        }

        static Tile EnsureTile(string name, Color color, string glyph)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(AssetRoot + "/" + name + ".asset");
            if (existing != null && existing.sprite != null) return existing;
            string path = AssetRoot + "/" + name + ".png";
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color32[256]; color.a = 1;
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                pixels[y * 16 + x] = x == 0 || y == 0 || x == 15 || y == 15 ? new Color(.15f,.17f,.18f,1) : color;
            var font = new Dictionary<char, string> { ['0']="111101101101111", ['1']="010110010010111",
                ['+']="000010111010000", ['-']="000000111000000", ['X']="101101010101101", ['V']="101101101101010",
                ['N']="101111111111101", ['E']="111100110100111", ['S']="111100111001111", ['W']="101101111111101" };
            int startX = (16 - (glyph.Length * 4 - 1)) / 2;
            for (int c = 0; c < glyph.Length; c++) for (int y = 0; y < 5; y++) for (int x = 0; x < 3; x++)
                if (font[glyph[c]][y * 3 + x] == '1') pixels[(10 - y) * 16 + startX + c * 4 + x] = new Color(.08f,.10f,.12f,1);
            texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var tile = existing != null ? existing : ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.None;
            if (existing == null) AssetDatabase.CreateAsset(tile, AssetRoot + "/" + name + ".asset");
            EditorUtility.SetDirty(tile); return tile;
        }

        static void RequireStopped()
        { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use stopped Edit Mode."); }
    }
}
