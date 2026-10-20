using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.U2D.Sprites;
using RoadOfTheOldKing.World;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RoadOfTheOldKing.EditorTools
{
    public static class TerrainKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/Shared/Terrain";

        // Imports a reusable tileset. This never reads or rebuilds a scene.
        public static void BuildCellCliffs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/CliffTilesManifest.json")).entries;
            if (entries.Any(e => e.width != 16 || e.height != 16))
                throw new InvalidDataException("Cliff tiles must occupy one 16px cell.");
            var sprites = ImportSprites(Root + "/CliffTiles16.png", entries);
            // Native Sprite physics shapes: 4px side strips / 2px rear rims.
            // Visual terrain remains noncolliding; these assets are painted on Collision.
            var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/CliffTiles16.png");
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var outlines = provider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            foreach (var rect in provider.GetSpriteRects().Where(r => r.name.StartsWith("CliffCollision_", StringComparison.Ordinal)))
            {
                int mask = int.Parse(rect.name.Substring("CliffCollision_".Length));
                var shapes = new List<Vector2[]>();
                Action<float,float,float,float> box = (x0,y0,x1,y1) => shapes.Add(new[] {
                    new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1) });
                if ((mask & 1) != 0) box(-8,6,8,8);
                if ((mask & 2) != 0) box(4,-8,8,8);
                if ((mask & 4) != 0) box(-8,-8,-4,8);
                outlines.SetOutlines(rect.spriteID, shapes);
            }
            provider.Apply(); importer.SaveAndReimport();
            sprites = AssetDatabase.LoadAllAssetsAtPath(Root + "/CliffTiles16.png").OfType<Sprite>().ToDictionary(s => s.name);
            foreach (var entry in entries.Where(e => e.material == "Piece" || e.material == "Collision"))
            {
                var folder = entry.material == "Collision" ? "Assets/Art/Tiles/Shared/Collision/" : Root + "/Tiles/";
                var tile = GetOrCreate<Tile>(folder + entry.name + ".asset");
                tile.sprite = sprites[entry.name];
                tile.colliderType = entry.material == "Collision" ? Tile.ColliderType.Sprite : Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
            }
            var banks = new List<Tile>();
            foreach (var entry in entries.Where(e => e.material == "Bank"))
            {
                var tile = GetOrCreate<Tile>(Root + "/Tiles/" + entry.name + ".asset");
                tile.sprite = sprites[entry.name];
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                banks.Add(tile);
            }
            var brushes = new List<RuleTile>();
            foreach (var material in new[] { "EarthTop", "EarthFace", "StoneTop", "StoneFace" })
            {
                var brush = GetOrCreate<TerrainRuleTile>(Root + "/Rules/Paint_Cliff" + material + ".asset");
                brush.part = material.EndsWith("Top", StringComparison.Ordinal)
                    ? TerrainRuleTile.Part.Top : TerrainRuleTile.Part.Face;
                brush.m_TilingRules.Clear();
                brush.m_DefaultColliderType = Tile.ColliderType.None;
                brush.m_DefaultGameObject = null;
                brush.m_DefaultSprite = sprites["Cliff" + material + "_255_0"];
                foreach (var group in entries.Where(e => e.material == material).GroupBy(e => e.mask).OrderByDescending(g => g.Key))
                {
                    int mask = group.Key;
                    var variants = group.OrderBy(e => e.variant).Select(e => sprites[e.name]).ToArray();
                    if (material == "StoneTop" && mask == 255 && variants.Length > 2)
                    {
                        // Bedrock should read as a quiet plane. Weight the existing
                        // plain sprite instead of adding duplicate atlas images.
                        var weighted = Enumerable.Repeat(variants[0], 12).ToArray();
                        weighted[3] = variants[1];
                        weighted[8] = variants[2];
                        variants = weighted;
                    }
                    var rule = MakeRule(variants);
                    for (int i = 0; i < 8; i++)
                    {
                        if (i >= 4 && ((mask & (1 << (i - 4))) == 0 || (mask & (1 << ((i - 3) % 4))) == 0)) continue;
                        rule.m_NeighborPositions.Add(Neighbors[i]);
                        rule.m_Neighbors.Add((mask & (1 << i)) != 0 ? RuleTile.TilingRule.Neighbor.This : RuleTile.TilingRule.Neighbor.NotThis);
                    }
                    brush.m_TilingRules.Add(rule);
                }
                brush.UpdateNeighborPositions();
                EditorUtility.SetDirty(brush);
                brushes.Add(brush);
            }
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Cliffs - tops and faces", typeof(Grid));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
                for (int material = 0; material < 2; material++)
                {
                    var top = AddMap(root.transform, material == 0 ? "Earth tops" : "Stone tops", 1);
                    var face = AddMap(root.transform, material == 0 ? "Earth faces" : "Stone faces", 0);
                    int x0 = material * 10;
                    for (int x = 0; x < 7; x++) for (int y = 1; y < 5; y++)
                        top.SetTile(new Vector3Int(x0+x, y, 0), brushes[material*2]);
                    for (int x = 0; x < 7; x++)
                        face.SetTile(new Vector3Int(x0+x, 0, 0), brushes[material*2+1]);
                }
                var joinedTop = AddMap(root.transform, "Same-height materials - tops", 1);
                var joinedFace = AddMap(root.transform, "Same-height materials - faces", 0);
                for (int x = 20; x < 28; x++) for (int y = 0; y < 6; y++)
                    (y < 1 ? joinedFace : joinedTop).SetTile(new Vector3Int(x,y,0), brushes[(x < 24 ? 0 : 2) + (y < 1 ? 1 : 0)]);
                var higherTop = AddMap(root.transform, "Higher rock - tops", 3);
                var higherFace = AddMap(root.transform, "Higher rock - faces", 2);
                for (int x = 30; x < 38; x++) for (int y = 0; y < 9; y++)
                    (y < 1 ? joinedFace : joinedTop).SetTile(new Vector3Int(x,y,0), brushes[y < 1 ? 1 : 0]);
                for (int x = 34; x < 38; x++) for (int y = 3; y < 9; y++)
                    (y < 4 ? higherFace : higherTop).SetTile(new Vector3Int(x,y,0), brushes[y < 4 ? 3 : 2]);
                var shoreline = AddMap(root.transform, "Directional submerged banks", 0);
                for (int i=0; i<banks.Count; i++) shoreline.SetTile(new Vector3Int(i*2,-3,0), banks[i]);
                var steps = AddMap(root.transform, "One level - one stair row", 0);
                for (int x=0;x<3;x++) steps.SetTile(new Vector3Int(20+x,-3,0),
                    AssetDatabase.LoadAssetAtPath<Tile>(Root+"/Tiles/StoneStairs_Single_"+new[]{"Left","Center","Right"}[x]+".asset"));
                SavePalette(root, Root + "/Palettes/Terrain_Cliffs.prefab");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Shared/Refresh Terrain Assets")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            // Use the shared blank zone template without rebuilding Tutorial assets.
            BuildKit(Root, "Terrain");
            // Banks and reusable cliff cells belong to one shared terrain set.
            // Basin water is last because it extends the base terrain palette.
            BuildDepthEdges();
            BuildCellCliffs();
            BuildBasinWater();
        }

        public static void BuildDepthEdges()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/DepthManifest.json")).entries;
            var sprites = ImportSprites(Root + "/BankJoins16.png", entries);
            foreach (var entry in entries)
            {
                // Joined crests supersede the old independently painted face rows.
                if (entry.name.StartsWith("ShoreFace_", StringComparison.Ordinal)) continue;
                var tile = GetOrCreate<Tile>(Root + "/Tiles/" + entry.name + ".asset");
                tile.sprite = sprites[entry.name];
                tile.colliderType = Tile.ColliderType.None;
                tile.color = Color.white;
                tile.transform = Matrix4x4.identity;
                EditorUtility.SetDirty(tile);
            }
            var scratch = EditorSceneManager.NewPreviewScene();
            try
            {
                BuildDepthPalette(scratch);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scratch);
            }
            AssetDatabase.SaveAssets();
        }

        public static void BuildBasinWater()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/BasinWaterManifest.json")).entries;
            var sprites = ImportSprites(Root + "/BasinWater16.png", entries);
            var source = AssetDatabase.LoadAssetAtPath<RuleTile>(Root + "/Rules/Paint_MeadowWater.asset");
            var target = GetOrCreate<RuleTile>(Root + "/Rules/Paint_BasinWater.asset");
            EditorUtility.CopySerialized(source, target);
            target.name = "Paint_BasinWater";
            target.m_DefaultSprite = sprites[source.m_DefaultSprite.name.Replace("MeadowWater", "BasinWater")];
            foreach (var rule in target.m_TilingRules)
                rule.m_Sprites = rule.m_Sprites.Select(s => sprites[s.name.Replace("MeadowWater", "BasinWater")]).ToArray();
            target.UpdateNeighborPositions(); EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            string palettePath = Root + "/Palettes/Terrain_Paint.prefab";
            var palette = PrefabUtility.LoadPrefabContents(palettePath);
            try
            {
                palette.GetComponentInChildren<Tilemap>().SetTile(new Vector3Int(12, 0, 0), target);
                PrefabUtility.SaveAsPrefabAsset(palette, palettePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(palette); }
            BuildRiverWater();
        }

        public static void BuildRiverWater()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/DirtWaterManifest.json")).entries;
            var sprites = ImportSprites(Root + "/DirtWater16.png", entries);
            var source = AssetDatabase.LoadAssetAtPath<RuleTile>(Root + "/Rules/Paint_MeadowWater.asset");
            var target = GetOrCreate<RuleTile>(Root + "/Rules/Paint_DirtWater.asset");
            EditorUtility.CopySerialized(source, target); target.name = "Paint_DirtWater";
            target.m_DefaultSprite = sprites[source.m_DefaultSprite.name.Replace("MeadowWater", "DirtWater")];
            foreach (var rule in target.m_TilingRules)
                rule.m_Sprites = rule.m_Sprites.Select(s => sprites[s.name.Replace("MeadowWater", "DirtWater")]).ToArray();
            target.UpdateNeighborPositions(); EditorUtility.SetDirty(target); AssetDatabase.SaveAssetIfDirty(target);
            string path = Root + "/Palettes/Terrain_Paint.prefab";
            var palette = PrefabUtility.LoadPrefabContents(path);
            try
            {
                palette.GetComponentInChildren<Tilemap>().SetTile(new Vector3Int(14, 0, 0), target);
                PrefabUtility.SaveAsPrefabAsset(palette, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(palette); }
        }

        static void BuildDepthPalette(UnityEngine.SceneManagement.Scene scratch)
        {
            var root = new GameObject("Banks, wet joins and doorway", typeof(Grid));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scratch);
            var map = AddMap(root.transform, "Directional banks and wet corner assemblies", 0);
            Func<string, Tile> tile = name => AssetDatabase.LoadAssetAtPath<Tile>(Root + "/Tiles/" + name + ".asset");
            var edges = new[] {"Bank_Back", "Bank_West", "Bank_East", "Bank_Foot", "Bank_WestBack", "Bank_EastBack", "Bank_WestBackOuter", "Bank_EastBackOuter",
                "Stone_Back", "Stone_West", "Stone_East", "Stone_Foot", "Stone_WestBack", "Stone_EastBack", "Stone_WestBackOuter", "Stone_EastBackOuter"};
            for (int i = 0; i < edges.Length; i++) map.SetTile(new Vector3Int(i%8*2, -i/8*3, 0), tile(edges[i]));
            var lips = new[] { "ShoreLip_W", "ShoreLip_E", "ShoreLip_Inside_E" };
            for (int i = 0; i < lips.Length; i++) map.SetTile(new Vector3Int(17+i*2, -6, 0), tile(lips[i]));
            for (int y = 0; y < 3; y++)
            {
                for (int side = 0; side < 2; side++)
                {
                    map.SetTile(new Vector3Int(7+side*2, y-9, 0), tile("ShoreReturn_"+(side==0?"W":"E")+"_"+y));
                    map.SetTile(new Vector3Int(12+side*2, y-9, 0), tile("ShoreInside_"+(side==0?"W":"E")+"_"+y));
                }
            }
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++)
                map.SetTile(new Vector3Int(x, y-15, 0), tile("Door_"+x+"_"+y));
            SavePalette(root, Root + "/Palettes/Terrain_SpecialJoins.prefab");
        }


        public static void BakeBasinDrainage()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="GreenLowlands")
                throw new InvalidOperationException("Stopped GreenLowlands required.");
            var water=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Tilemap>(true))
                .Single(m=>m.name=="Terrain - changing pond water");
            if(water.transform.position!=Vector3.zero||water.transform.lossyScale!=Vector3.one)
                throw new InvalidOperationException("Expected aligned authored water footprint.");
            var cells=new HashSet<Vector3Int>();
            foreach(var p in water.cellBounds.allPositionsWithin)if(water.HasTile(p))cells.Add(p);
            if(cells.Count==0)throw new InvalidOperationException("No pond coverage to bake.");
            int x0=cells.Min(p=>p.x),y0=cells.Min(p=>p.y);
            int width=cells.Max(p=>p.x)-x0+1,height=cells.Max(p=>p.y)-y0+1;
            var maps = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true)).ToArray();
            var shore = maps.FirstOrDefault(m => m.name == "Terrain - basin banks");
            // A vertical wall/stair reveals from its top as water falls. It is
            // not a shallow floor whose height follows distance from shore.
            var submerged = new HashSet<Vector3Int>();
            foreach (var map in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true))
                .Where(m => m.name == "Terrain - Farm faces" || m.name == "Terrain - submerged height connectors"))
                foreach (var c in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(c) && (map.name.EndsWith("faces", StringComparison.Ordinal) || map.GetTile(c).name.StartsWith("StoneStairs_", StringComparison.Ordinal)))
                        submerged.Add(c);
            var wallFeet = new Dictionary<Vector3Int, int>();
            foreach (var c in submerged)
            {
                int foot = c.y;
                while (submerged.Contains(new Vector3Int(c.x, foot - 1, 0))) foot--;
                wallFeet[c] = foot;
            }
            var pixels=new Color32[width*16*height*16];
            for(int y=0;y<height*16;y++)for(int x=0;x<width*16;x++){
                var cell=new Vector3Int(x0+x/16,y0+y/16,0);
                if(!cells.Contains(cell))continue;
                var p=new Vector2(x0+(x+.5f)/16f,y0+(y+.5f)/16f);
                // Basin floor is one low surface. Only authored banks slope
                // up to Farm; distance from a wall does not raise the floor.
                float elevation = .10f + .015f * (Mathf.Sin(p.x * .73f + p.y * .31f) + Mathf.Sin(p.y * .91f));
                if (shore != null && shore.HasTile(cell))
                {
                    float u = (x % 16 + .5f) / 16f, v = (y % 16 + .5f) / 16f;
                    string edge = shore.GetTile(cell).name;
                    float side = Mathf.Clamp01((edge.Contains("West") ? 1-u : u) / .75f);
                    float back = Mathf.Clamp01((1-v) / .75f);
                    elevation = edge.EndsWith("_Back",StringComparison.Ordinal) ? back :
                        !edge.Contains("Back") ? side : edge.EndsWith("Outer",StringComparison.Ordinal) ? Mathf.Min(side,back) : Mathf.Max(side,back);
                }
                if (wallFeet.TryGetValue(cell, out int footY))
                    elevation = Mathf.Clamp01((p.y - footY) / LowlandsElevationAuthoring.RiseCells);
                pixels[y*width*16+x]=new Color32((byte)Mathf.RoundToInt(Mathf.Clamp(elevation,.01f,.99f)*255),0,0,255);
            }
            var texture=new Texture2D(width*16,height*16,TextureFormat.RGBA32,false,true);
            texture.SetPixels32(pixels);texture.Apply();string path="Assets/Art/Generated/GreenLowlands/BasinDepth.png";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=false;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(width,height)*16),2048,8192);
            importer.SaveAndReimport();
            var reveal=water.GetComponentInParent<RoadOfTheOldKing.World.SluiceWaterReveal>(true);
            if(reveal==null)throw new InvalidOperationException("Changing water must belong to the sluice reveal.");
            Undo.RecordObject(reveal,"Use basin elevation instead of a directional wipe");var data=new SerializedObject(reveal);
            data.FindProperty("basinDepth").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            data.FindProperty("basinBounds").vector4Value=new Vector4(x0,y0,width,height);
            data.FindProperty("duration").floatValue=8f;data.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Baked native bed-height data for "+cells.Count+" pond cells. Eight-second reveal; collision remains sluice-owned until completion.");
        }
        private static readonly string[] Materials = { "MeadowWater", "StoneWater", "DeepWater", "GrassLedge", "StoneLedge", "StoneWall" };

        internal static void BuildKit(string assetRoot, string prefix)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
            if (!AssetDatabase.LoadAssetAtPath<Tile>(GroundKitBuilder.CollisionPath))
                throw new InvalidOperationException("Missing shared collision brush: " + GroundKitBuilder.CollisionPath);
            var previous = SceneManager.GetActiveScene();
            var scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scratch);
                var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(assetRoot + "/TileManifest.json")).entries;
                foreach (string folder in new[] { "Tiles", "Rules", "Animations", "Palettes" }) Directory.CreateDirectory(assetRoot + "/" + folder);
                AssetDatabase.Refresh();
                var sprites = ImportSprites(assetRoot + "/" + prefix + "16.png", entries);
                var tiles = new Dictionary<string, Tile>();
                foreach (var entry in entries)
                {
                    var tile = GetOrCreate<Tile>(assetRoot + "/Tiles/" + entry.name + ".asset");
                    tile.sprite = sprites[entry.name];
                    tile.colliderType = Tile.ColliderType.None;
                    tile.color = Color.white; tile.transform = Matrix4x4.identity;
                    EditorUtility.SetDirty(tile); tiles.Add(entry.name, tile);
                }
                var rules = CreateRules(assetRoot, entries, sprites);
                var animations = CreateAnimations(assetRoot, entries, sprites);
                var collision = AssetDatabase.LoadAssetAtPath<Tile>(GroundKitBuilder.CollisionPath);
                CreatePalettes(assetRoot, prefix, entries, tiles, rules, animations, collision);
                AssetDatabase.SaveAssets();
                Debug.Log(prefix + " kit: 315 visual tiles, 6 RuleTiles, 3 AnimatedTiles, 3 palettes.");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scratch, true);
            }
        }

        private static Dictionary<string, RuleTile> CreateRules(string assetRoot, Entry[] entries, Dictionary<string, Sprite> sprites)
        {
            var result = new Dictionary<string, RuleTile>();
            foreach (string material in Materials)
            {
                var tile = GetOrCreate<RuleTile>(assetRoot + "/Rules/Paint_" + material + ".asset");
                tile.m_TilingRules.Clear();
                var pieces = entries.Where(e => e.material == material && e.variant == 0).ToArray();
                tile.m_DefaultSprite = sprites[pieces[0].name];
                tile.m_DefaultColliderType = Tile.ColliderType.None;
                foreach (var entry in pieces)
                {
                    var fills = entry.mask == 255
                        ? entries.Where(e => e.material == material && e.mask == 255).Select(e => sprites[e.name]).ToArray()
                        : new[] { sprites[entry.name] };
                    // Keep broad surfaces calm: center texture is an occasional accent.
                    if (entry.mask == 255) fills = new[] { fills[0], fills[0], fills[1], fills[0], fills[0], fills[2], fills[0], fills[0], fills[3], fills[0], fills[0], fills[0] };
                    var rule = MakeRule(fills);
                    rule.m_Id = tile.m_TilingRules.Count;
                    for (int bit = 0; bit < (material == "StoneWall" ? 4 : 8); bit++)
                    {
                        if (bit >= 4 && ((entry.mask & (1 << (bit - 4))) == 0 || (entry.mask & (1 << ((bit - 3) % 4))) == 0)) continue;
                        rule.m_NeighborPositions.Add(Neighbors[bit]);
                        rule.m_Neighbors.Add((entry.mask & (1 << bit)) != 0
                            ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis);
                    }
                    tile.m_TilingRules.Add(rule);
                }
                tile.UpdateNeighborPositions(); EditorUtility.SetDirty(tile); result.Add(material, tile);
            }
            return result;
        }

        private static Dictionary<string, AnimatedTile> CreateAnimations(string assetRoot, Entry[] entries, Dictionary<string, Sprite> sprites)
        {
            var result = new Dictionary<string, AnimatedTile>();
            foreach (string material in new[] { "Ripple", "FallBody", "FallFoam" })
            {
                var tile = GetOrCreate<AnimatedTile>(assetRoot + "/Animations/Animated_" + material + ".asset");
                tile.m_AnimatedSprites = entries.Where(e => e.material == material).OrderBy(e => e.variant).Select(e => sprites[e.name]).ToArray();
                tile.m_MinSpeed = tile.m_MaxSpeed = material == "Ripple" ? 1f : 1.25f;
                tile.m_AnimationStartTime = 0; tile.m_AnimationStartFrame = 0;
                tile.m_TileColliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile); result.Add(material, tile);
            }
            return result;
        }

        private static void CreatePalettes(string assetRoot, string prefix, Entry[] entries, Dictionary<string, Tile> tiles, Dictionary<string, RuleTile> rules, Dictionary<string, AnimatedTile> animations, Tile collision)
        {
            var root = new GameObject(prefix + " - Paint", typeof(Grid));
            var map = AddMap(root.transform, "Automatic brushes", 0);
            for (int i = 0; i < Materials.Length; i++) map.SetTile(new Vector3Int(i * 2, 0, 0), rules[Materials[i]]);
            var basinWater = AssetDatabase.LoadAssetAtPath<RuleTile>(assetRoot + "/Rules/Paint_BasinWater.asset");
            if (basinWater != null) map.SetTile(new Vector3Int(12, 0, 0), basinWater);
            int x = 0;
            foreach (var animation in animations.Values) { map.SetTile(new Vector3Int(x, -3, 0), animation); x += 2; }
            map.SetTile(new Vector3Int(6, -3, 0), tiles["FallLip_00"]);
            map.SetTile(new Vector3Int(0, -6, 0), collision);
            map.animationFrameRate = 4;
            SavePalette(root, assetRoot + "/Palettes/" + prefix + "_Paint.prefab");

            root = new GameObject(prefix + " - Structures", typeof(Grid));
            map = AddMap(root.transform, "Cliffs stairs ramps and walls", 0);
            var structures = new[] { "EarthCliff", "StoneCliff", "StoneStairs", "EarthRamp" };
            var rows = new[] { "Top", "Middle", "Foot" }; var columns = new[] { "Left", "Center", "Right" };
            for (int s = 0; s < structures.Length; s++) for (int r = 0; r < 3; r++) for (int c = 0; c < 3; c++)
                map.SetTile(new Vector3Int(s * 5 + c, -r, 0), tiles[structures[s] + "_" + rows[r] + "_" + columns[c]]);
            var walls = entries.Where(e => e.material == "StoneWall").ToArray();
            for (int i = 0; i < walls.Length; i++) map.SetTile(new Vector3Int(i % 8, -5 - i / 8, 0), tiles[walls[i].name]);
            map.SetTile(new Vector3Int(11, -5, 0), tiles["FallLip_00"]);
            map.SetTile(new Vector3Int(11, -6, 0), animations["FallBody"]);
            map.SetTile(new Vector3Int(11, -7, 0), animations["FallFoam"]);
            map.animationFrameRate = 4;
            SavePalette(root, assetRoot + "/Palettes/" + prefix + "_Structures.prefab");

            root = new GameObject(prefix + " - Individual Pieces", typeof(Grid));
            map = AddMap(root.transform, "All individual pieces", 0);
            int row = 0;
            foreach (string material in entries.Select(e => e.material).Distinct())
            {
                var pieces = entries.Where(e => e.material == material).ToArray();
                for (int i = 0; i < pieces.Length; i++) map.SetTile(new Vector3Int(i % 12, -row - i / 12, 0), tiles[pieces[i].name]);
                row += (pieces.Length + 11) / 12 + 1;
            }
            SavePalette(root, assetRoot + "/Palettes/" + prefix + "_IndividualPieces.prefab");
        }

    }
}
