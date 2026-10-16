using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    public static class GreenLowlandsTerrainKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/GreenLowlands/Terrain";

        [MenuItem("Tools/Road of the Old King/Build Green Lowlands Terrain Kit")]
        public static void Build()
        {
            // Use the shared blank zone template without rebuilding Tutorial assets.
            TutorialTerrainKitBuilder.BuildKit(Root, "GreenLowlandsTerrain");
        }

        [MenuItem("Tools/Road of the Old King/Build Lowlands Depth Edges")]
        public static void BuildDepthEdges()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/DepthManifest.json")).entries;
            var sprites = ImportSprites(Root + "/GreenLowlandsDepth16.png", entries);
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

        [MenuItem("Tools/Road of the Old King/Build Lowlands Automatic Cliff Kit")]
        public static void BuildCliffKit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/CliffManifest.json")).entries;
            if (entries.Length != 188 || entries.Any(e => e.width != 16 || e.height != 64))
                throw new InvalidDataException("Expected 47 complete cliff topologies, four spatial phases each.");
            var sprites = ImportSprites(Root + "/GreenLowlandsCliffs16.png", entries, anchorTopCell: true);
            var brush = GetOrCreate<RoadOfTheOldKing.World.RaisedCliffTile>(Root + "/Rules/Paint_RaisedEarth.asset");
            brush.crestLine = false;
            brush.sprites = new Sprite[256*4];
            foreach (var entry in entries) brush.sprites[entry.mask*4+entry.variant] = sprites[entry.name];
            EditorUtility.SetDirty(brush);
            var crest = GetOrCreate<RoadOfTheOldKing.World.RaisedCliffTile>(Root + "/Rules/Paint_CliffCrest.asset");
            crest.sprites = brush.sprites;
            crest.crestLine = true;
            EditorUtility.SetDirty(crest);
            var scratch = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = CreateCliffRoot("Lowlands cliffs - paint upper footprint", scratch);
                var map = AddCliffMap(root.transform);
                // Picking any cell of this assembled example picks the SAME automatic tile.
                // The whole front follows painting/erasing the top footprint.
                PaintCliffSample(map, brush);
                map.SetTile(new Vector3Int(-9, 6, 0), brush);
                var shore = AddCliffMap(root.transform);
                shore.name = "Retaining cliffs - fill the upper rim band";
                for (int x = -6; x <= 5; x++)
                    for (int y = -7 + Mathf.Max(0, x / 2); y <= -7 + Mathf.Max(0, (x + 1) / 2); y++)
                        shore.SetTile(new Vector3Int(x, y, 0), crest);
                SavePalette(root, Root + "/Palettes/GreenLowlandsTerrain_Cliffs.prefab");

                Directory.CreateDirectory(Root + "/Templates");
                root = CreateCliffRoot("Lowlands cliff surface", scratch);
                AddCliffMap(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, Root + "/Templates/LowlandsCliffSurface.prefab");
                UnityEngine.Object.DestroyImmediate(root);
                root = CreateCliffRoot("Lowlands cliff sample", scratch);
                PaintCliffSample(AddCliffMap(root.transform), brush);
                PrefabUtility.SaveAsPrefabAsset(root, Root + "/Templates/LowlandsCliffSample.prefab");
                UnityEngine.Object.DestroyImmediate(root);
                BuildDepthPalette(scratch);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scratch);
            }
        }

        [MenuItem("Tools/Road of the Old King/Build Lowlands Basin Water")]
        public static void BuildBasinWater()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use stopped Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/BasinWaterManifest.json")).entries;
            var sprites = ImportSprites(Root + "/GreenLowlandsBasinWater16.png", entries);
            var source = AssetDatabase.LoadAssetAtPath<RuleTile>(Root + "/Rules/Paint_MeadowWater.asset");
            var target = GetOrCreate<RuleTile>(Root + "/Rules/Paint_BasinWater.asset");
            EditorUtility.CopySerialized(source, target);
            target.name = "Paint_BasinWater";
            target.m_DefaultSprite = sprites[source.m_DefaultSprite.name.Replace("MeadowWater", "BasinWater")];
            foreach (var rule in target.m_TilingRules)
                rule.m_Sprites = rule.m_Sprites.Select(s => sprites[s.name.Replace("MeadowWater", "BasinWater")]).ToArray();
            target.UpdateNeighborPositions(); EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            string palettePath = Root + "/Palettes/GreenLowlandsTerrain_Paint.prefab";
            var palette = PrefabUtility.LoadPrefabContents(palettePath);
            try
            {
                palette.GetComponentInChildren<Tilemap>().SetTile(new Vector3Int(12, 0, 0), target);
                PrefabUtility.SaveAsPrefabAsset(palette, palettePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(palette); }
        }

        static GameObject CreateCliffRoot(string name, UnityEngine.SceneManagement.Scene scratch)
        {
            var root = new GameObject(name, typeof(Grid));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scratch);
            return root;
        }

        static UnityEngine.Tilemaps.Tilemap AddCliffMap(Transform parent)
        {
            var map = AddMap(parent, "Cliffs - paint upper footprint", 42);
            var renderer = map.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            // Tall sprites overlap three cells south. TopRight orders northern
            // cells before southern caps; individual mode avoids chunk splits.
            renderer.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Individual;
            renderer.sortOrder = UnityEngine.Tilemaps.TilemapRenderer.SortOrder.TopRight;
            return map;
        }

        static void PaintCliffSample(UnityEngine.Tilemaps.Tilemap map, RoadOfTheOldKing.World.RaisedCliffTile brush)
        {
            for (int y = 0; y <= 6; y++) for (int x = -6; x <= 5; x++)
            {
                bool footprint = x <= -2 || y >= 3;
                // A north notch also exercises the opposite pair of inner turns.
                if (x >= 2 && x <= 3 && y >= 5) footprint = false;
                if (footprint) map.SetTile(new Vector3Int(x, y, 0), brush);
            }
        }

        static void BuildDepthPalette(UnityEngine.SceneManagement.Scene scratch)
        {
            var root = CreateCliffRoot("Lowlands banks wet joins and doorway", scratch);
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
            SavePalette(root, Root + "/Palettes/GreenLowlandsTerrain_DepthManual.prefab");
        }

        [MenuItem("Tools/Road of the Old King/Bake Lowlands Basin Drainage")]
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
            // Quarter-unit chamfer distances are sufficient for bed slopes; the
            // exported height field is sampled at the world's native 16 PPU.
            int w=width*4,h=height*4;var distance=new float[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                distance[y*w+x]=cells.Contains(new Vector3Int(x0+x/4,y0+y/4,0))?10000:0;
            Func<int,int,float> at=(x,y)=>x<0||x>=w||y<0||y>=h?0:distance[y*w+x];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)distance[y*w+x]=Mathf.Min(distance[y*w+x],
                Mathf.Min(Mathf.Min(at(x-1,y),at(x,y-1))+.25f,Mathf.Min(at(x-1,y-1),at(x+1,y-1))+.353553f));
            for(int y=h-1;y>=0;y--)for(int x=w-1;x>=0;x--)distance[y*w+x]=Mathf.Min(distance[y*w+x],
                Mathf.Min(Mathf.Min(at(x+1,y),at(x,y+1))+.25f,Mathf.Min(at(x+1,y+1),at(x-1,y+1))+.353553f));
            var channel=new[]{new Vector2(10,74),new Vector2(27,71),new Vector2(40,60),new Vector2(44,48),new Vector2(51,36)};
            var pixels=new Color32[width*16*height*16];
            for(int y=0;y<height*16;y++)for(int x=0;x<width*16;x++){
                var cell=new Vector3Int(x0+x/16,y0+y/16,0);
                if(!cells.Contains(cell))continue;
                var p=new Vector2(x0+(x+.5f)/16f,y0+(y+.5f)/16f);
                float sx=x/4f-.375f,sy=y/4f-.375f;int ix=Mathf.FloorToInt(sx),iy=Mathf.FloorToInt(sy);
                float d=Mathf.Lerp(Mathf.Lerp(at(ix,iy),at(ix+1,iy),sx-ix),Mathf.Lerp(at(ix,iy+1),at(ix+1,iy+1),sx-ix),sy-iy);
                float channelDistance=float.PositiveInfinity;
                for(int k=0;k<channel.Length-1;k++){
                    var segment=channel[k+1]-channel[k];float t=Mathf.Clamp01(Vector2.Dot(p-channel[k],segment)/segment.sqrMagnitude);
                    channelDistance=Mathf.Min(channelDistance,Vector2.Distance(p,channel[k]+segment*t));
                }
                float far=Mathf.Clamp01(Vector2.Distance(p,channel[0])/65f);
                float elevation=Mathf.Max(.10f+.10f*far,.88f-d*.075f);
                float channelWeight=(1f-Mathf.SmoothStep(0,1,channelDistance/2.5f))*Mathf.Clamp01(d/1.2f);
                elevation=Mathf.Lerp(elevation,.07f+.13f*far,channelWeight);
                elevation+=.013f*(Mathf.Sin(p.x*.73f+p.y*.31f)+Mathf.Sin(p.y*.91f));
                pixels[y*width*16+x]=new Color32((byte)Mathf.RoundToInt(Mathf.Clamp(elevation,.045f,.94f)*255),0,0,255);
            }
            var texture=new Texture2D(width*16,height*16,TextureFormat.RGBA32,false,true);
            texture.SetPixels32(pixels);texture.Apply();string path=Root+"/LowlandsBasinDepth.png";
            File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=false;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.SaveAndReimport();
            var reveal=water.GetComponentInParent<RoadOfTheOldKing.World.SluiceWaterReveal>(true);
            if(reveal==null)throw new InvalidOperationException("Changing water must belong to the sluice reveal.");
            Undo.RecordObject(reveal,"Use basin elevation instead of a directional wipe");var data=new SerializedObject(reveal);
            data.FindProperty("basinDepth").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            data.FindProperty("basinBounds").vector4Value=new Vector4(x0,y0,width,height);
            data.FindProperty("duration").floatValue=8f;data.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Baked native bed-height data for "+cells.Count+" pond cells. Eight-second reveal; collision remains sluice-owned until completion.");
        }
    }
}
