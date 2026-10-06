using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using Prop = RoadOfTheOldKing.EditorTools.TutorialEnvironmentKitBuilder.Prop;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    /// <summary>Regional sources plus direct references to retained Tutorial objects.</summary>
    public static class GreenLowlandsEnvironmentKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/GreenLowlands/Environment";
        public const string PrefabRoot = "Assets/Prefabs/Environment/GreenLowlands";
        [Serializable] public class Catalog { public Prop[] entries; public string[] reusedPrefabs; }
        public static Catalog ReadCatalog() => JsonUtility.FromJson<Catalog>(File.ReadAllText(Root+"/EnvironmentManifest.json"));
        public static string TilePath(string name,int x,int y) => Root+"/Tiles/"+name+"_"+x+"_"+y+".asset";

        [MenuItem("Tools/Road of the Old King/Build Green Lowlands Environment Kit")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode.");
            var catalog=ReadCatalog();
            var collisionTile=AssetDatabase.LoadAssetAtPath<Tile>(TutorialGroundKitBuilder.CollisionPath);
            if(!collisionTile)throw new InvalidOperationException("Build Ground first.");
            var reused=catalog.reusedPrefabs.Select(name=>AssetDatabase.LoadAssetAtPath<GameObject>(TutorialEnvironmentKitBuilder.PrefabRoot+"/"+name+".prefab")).ToArray();
            if(reused.Any(p=>!p))throw new InvalidOperationException("A retained Tutorial prefab is missing.");
            var swatches=File.ReadAllLines("../Docs/Art/Palettes/green-lowlands-field-and-old-road-v1.gpl")
                .Select(line=>line.Split((char[])null,StringSplitOptions.RemoveEmptyEntries))
                .Where(parts=>parts.Length>=3&&byte.TryParse(parts[0],out _))
                .Select(parts=>new Color32(byte.Parse(parts[0]),byte.Parse(parts[1]),byte.Parse(parts[2]),255)).ToArray();
            if(swatches.Length!=16)throw new InvalidDataException("Expected Field and Old Road palette.");
            var previous=SceneManager.GetActiveScene();
            var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            Texture2D atlas=null;
            try
            {
                SceneManager.SetActiveScene(scratch);
                foreach(string folder in new[]{Root+"/Tiles",Root+"/Palettes",PrefabRoot})Directory.CreateDirectory(folder);
                atlas=new Texture2D(512,256,TextureFormat.RGBA32,false);
                atlas.SetPixels32(new Color32[512*256]);
                var entries=new List<Entry>();int ax=2,ay=2,shelfHeight=0;
                foreach(var prop in catalog.entries)
                {
                    if(ax+prop.width+2>atlas.width){ax=2;ay+=shelfHeight+4;shelfHeight=0;}
                    if(ax+prop.width+2>atlas.width||ay+prop.height+2>atlas.height)throw new InvalidDataException("Atlas capacity exceeded.");
                    if(prop.swatches.Length>15)throw new InvalidDataException("Per-object color budget exceeded: "+prop.name);
                    var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    Color32[] pixels;
                    try
                    {
                        if(!source.LoadImage(File.ReadAllBytes("../ArtSource/GreenLowlands/Environment/"+prop.source+".png")))throw new InvalidDataException(prop.source);
                        pixels=TutorialEnvironmentKitBuilder.PrepareSprite(source,prop,swatches);
                    }
                    finally{Object.DestroyImmediate(source);}
                    var opaque=pixels.Where(p=>p.a!=0).ToArray();
                    if(opaque.Any(p=>p.a!=255||!swatches.Contains(p))||opaque.Distinct().Count()>prop.swatches.Length)throw new InvalidDataException("Palette/alpha mismatch: "+prop.name);
                    atlas.SetPixels32(ax,ay,prop.width,prop.height,pixels);
                    entries.Add(new Entry{name=prop.name,x=ax,y=ay,width=prop.width,height=prop.height});
                    for(int y=0;y<prop.height/16;y++)for(int x=0;x<prop.width/16;x++)
                        entries.Add(new Entry{name=prop.name+"_"+x+"_"+y,x=ax+x*16,y=ay+y*16,width=16,height=16});
                    ax+=prop.width+4;shelfHeight=Math.Max(shelfHeight,prop.height);
                }
                atlas.Apply();File.WriteAllBytes(Root+"/GreenLowlandsEnvironment16.png",atlas.EncodeToPNG());
                AssetDatabase.Refresh();
                var sprites=ImportSprites(Root+"/GreenLowlandsEnvironment16.png",entries.ToArray());
                var palette=new GameObject("GreenLowlandsEnvironment",typeof(Grid));
                var paletteMap=AddMap(palette.transform,"Complete object stamps",0);
                int index=0;
                foreach(var prop in catalog.entries)
                {
                    var root=new GameObject(prop.name,typeof(Grid));
                    var bases=AddMap(root.transform,"Environment",0,"World");
                    var overhead=AddMap(root.transform,"Above Player",100,"Player");
                    var collision=AddMap(root.transform,"Collision",0);
                    collision.gameObject.layer=LayerMask.NameToLayer("Environment");
                    collision.GetComponent<TilemapRenderer>().enabled=false;
                    collision.gameObject.AddComponent<TilemapCollider2D>();
                    var origin=new Vector3Int(index%4*12,-index/4*9,0);
                    for(int y=0;y<prop.height/16;y++)for(int x=0;x<prop.width/16;x++)
                    {
                        string name=prop.name+"_"+x+"_"+y;
                        var tile=GetOrCreate<Tile>(TilePath(prop.name,x,y));
                        tile.sprite=sprites[name];tile.colliderType=Tile.ColliderType.None;
                        tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(tile);
                        // Near rail occludes; far rail and deck stay beneath the player.
                        bool above=prop.kind=="bridge"?y==0:y>=prop.overheadRow;
                        (above?overhead:bases).SetTile(new Vector3Int(x,y,0),tile);
                        paletteMap.SetTile(origin+new Vector3Int(x,y,0),tile);
                    }
                    foreach(var area in prop.collision)
                        for(int y=area.y;y<area.y+area.height;y++)for(int x=area.x;x<area.x+area.width;x++)
                            collision.SetTile(new Vector3Int(x,y,0),collisionTile);
                    PrefabUtility.SaveAsPrefabAsset(root,PrefabRoot+"/"+prop.name+".prefab");Object.DestroyImmediate(root);
                    index++;
                }
                // Palette cells point at existing assets. No cloned/recolored Tutorial art.
                foreach(var prefab in reused)
                {
                    var origin=new Vector3Int(index%4*12,-index/4*9,0);
                    foreach(string mapName in new[]{"Environment","Above Player"})
                    {
                        var map=prefab.transform.Find(mapName).GetComponent<Tilemap>();
                        foreach(var cell in map.cellBounds.allPositionsWithin)
                            if(map.HasTile(cell))paletteMap.SetTile(origin+cell,map.GetTile(cell));
                    }
                    index++;
                }
                SavePalette(palette,Root+"/Palettes/GreenLowlandsEnvironment.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("Green Lowlands Environment: "+catalog.entries.Length+" new prefabs, "+reused.Length+" retained Tutorial objects, one palette; authored scenes unchanged.");
            }
            finally
            {
                if(atlas)Object.DestroyImmediate(atlas);
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scratch,true);
            }
        }
    }
}
