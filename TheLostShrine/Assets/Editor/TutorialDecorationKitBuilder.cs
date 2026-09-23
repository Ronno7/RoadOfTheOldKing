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

namespace TheLostShrine.EditorTools
{
    // Compact twelve-design kit. Approved sample pixels are copied unchanged.
    internal static class TutorialDecorationKitBuilder
    {
        internal const string Root = "Assets/Art/Tiles/Tutorial/DetailDecoration";
        internal const string VisualRoot = "Assets/Prefabs/WorldArt/Tutorial";
        const string Sources = "../Docs/Art/Tutorial/Sources/Decoration/";
        [Serializable] sealed class Item
        {
            public string name;
            public RectInt source;
            public int x, y, width, height=16, drawWidth, drawHeight;
            public int[] swatches;
            public bool ground, native;
        }
        [Serializable] sealed class Manifest { public Item[] items; }
        static Item[] Items()
        {
            return new[] {
                new Item { name="MeadowFlowers", source=new RectInt(0,0,16,16), x=0, width=16, ground=true, native=true },
                new Item { name="GrassTuft", source=new RectInt(16,0,16,16), x=16, width=16, ground=true, native=true },
                new Item { name="FallenLeaves", source=new RectInt(32,0,32,16), x=32, width=32, ground=true, native=true },
                new Item { name="CarvedStone", source=new RectInt(64,0,16,16), x=64, width=16, ground=true, native=true },
                new Item { name="ClayJar", source=new RectInt(80,0,16,16), x=80, width=16, native=true },
                new Item { name="WoodChips", source=new RectInt(96,0,32,16), x=96, width=32, ground=true, native=true },
                new Item { name="LooseGrass", source=new RectInt(0,0,560,500), x=0,y=16,width=16,drawWidth=12,drawHeight=10,swatches=new[]{1,2,3},ground=true },
                new Item { name="SplitLogs", source=new RectInt(560,0,560,500), x=16,y=16,width=32,drawWidth=23,drawHeight=11,swatches=new[]{4,5,6,7},ground=true },
                new Item { name="WoodenBucket", source=new RectInt(1120,0,597,500), x=48,y=16,width=16,drawWidth=10,drawHeight=13,swatches=new[]{4,5,6,7} },
                new Item { name="GrainSack", source=new RectInt(0,500,560,416), x=64,y=16,width=16,drawWidth=11,drawHeight=13,swatches=new[]{5,6,7,11} },
                new Item { name="WornPractice", source=new RectInt(560,500,560,416), x=80,y=16,width=32,drawWidth=27,drawHeight=12,swatches=new[]{5,6,7},ground=true },
                new Item { name="FireRing", source=new RectInt(1120,500,597,416), x=0,y=32,width=32,height=32,drawWidth=27,drawHeight=19,swatches=new[]{4,5,6,12,13} }
            };
        }

        [MenuItem("Tools/The Lost Shrine/Build Tutorial Decoration Kit")]
        public static void Build()
        {
            Directory.CreateDirectory(Root+"/Tiles");
            Directory.CreateDirectory(VisualRoot);
            var palette=File.ReadAllLines("../Docs/Art/Palettes/tutorial-hearth-and-meadow-v1.gpl")
                .Select(l=>l.Split((char[])null,StringSplitOptions.RemoveEmptyEntries))
                .Where(p=>p.Length>=3 && byte.TryParse(p[0],out _))
                .Select(p=>new Color32(byte.Parse(p[0]),byte.Parse(p[1]),byte.Parse(p[2]),255)).ToArray();
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            var approved=new Texture2D(2,2,TextureFormat.RGBA32,false);
            var atlas=new Texture2D(128,64,TextureFormat.RGBA32,false);
            var entries=new List<TileKitAssets.Entry>();
            var items=Items();
            try
            {
                source.LoadImage(File.ReadAllBytes(Sources+"Expansion.png"));
                approved.LoadImage(File.ReadAllBytes(Sources+"ApprovedSample16.png"));
                var input=source.GetPixels32();
                var original=approved.GetPixels32();
                var output=new Color32[128*64];
                foreach(var item in items)
                {
                    if(item.native)
                    {
                        for(int y=0;y<item.height;y++)for(int x=0;x<item.width;x++)
                            output[(item.y+y)*128+item.x+x]=original[(item.source.y+y)*approved.width+item.source.x+x];
                    }
                    else
                    {
                        int minX=source.width,minY=source.height,maxX=-1,maxY=-1;
                        for(int y=item.source.yMin;y<item.source.yMax;y++)
                        for(int x=item.source.xMin;x<item.source.xMax;x++)
                            if(input[(source.height-1-y)*source.width+x].a>=128)
                            { minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y); }
                        if(maxX<minX)throw new InvalidOperationException("Empty source: "+item.name);
                        int sw=maxX-minX+1,sh=maxY-minY+1;
                        float scale=Math.Min((float)item.drawWidth/sw,(float)item.drawHeight/sh);
                        int w=Math.Max(1,Mathf.RoundToInt(sw*scale)),h=Math.Max(1,Mathf.RoundToInt(sh*scale));
                        int ox=item.x+(item.width-w)/2,oy=item.y+(item.ground ? (item.height-h)/2 : 0);
                        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                        {
                            int sx=minX+Math.Min(sw-1,(int)((x+.5f)*sw/w));
                            int sy=maxY-Math.Min(sh-1,(int)((y+.5f)*sh/h));
                            var c=input[(source.height-1-sy)*source.width+sx];
                            if(c.a<128)continue;
                            int best=item.swatches[0],distance=int.MaxValue;
                            foreach(int i in item.swatches)
                            {
                                var p=palette[i];
                                int d=(c.r-p.r)*(c.r-p.r)+(c.g-p.g)*(c.g-p.g)+(c.b-p.b)*(c.b-p.b);
                                if(d<distance){distance=d;best=i;}
                            }
                            // Wear should read as scuffed earth, without bright marker-like spots.
                            if(item.name=="WornPractice")best=best==7 ? 6 : 5;
                            output[(oy+y)*128+ox+x]=palette[best];
                        }
                    }
                    entries.Add(new TileKitAssets.Entry {name=item.name,x=item.x,y=item.y,width=item.width,height=item.height});
                    if(item.ground)for(int x=0;x<item.width/16;x++)
                        entries.Add(new TileKitAssets.Entry {name=item.name+"_"+x,x=item.x+x*16,y=item.y,width=16,height=16});
                }
                atlas.SetPixels32(output);atlas.Apply();
                File.WriteAllBytes(Root+"/TutorialDecoration16.png",atlas.EncodeToPNG());
                File.WriteAllText(Root+"/DecorationManifest.json",JsonUtility.ToJson(new Manifest{items=items},true));
            }
            finally { Object.DestroyImmediate(source);Object.DestroyImmediate(approved);Object.DestroyImmediate(atlas); }
            var sprites=TileKitAssets.ImportSprites(Root+"/TutorialDecoration16.png",entries.ToArray());
            var previous=SceneManager.GetActiveScene();
            var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scratch);
                var root=new GameObject("TutorialDecoration",typeof(Grid));
                var map=TileKitAssets.AddMap(root.transform,"Ground decorations",30);
                int column=0,row=0;
                foreach(var item in items.Where(i=>i.ground))
                {
                    if(column+item.width/16>10){column=0;row-=2;}
                    for(int x=0;x<item.width/16;x++)
                    {
                        string name=item.name+"_"+x;
                        var tile=TileKitAssets.GetOrCreate<Tile>(Root+"/Tiles/"+name+".asset");
                        tile.sprite=sprites[name];tile.colliderType=Tile.ColliderType.None;
                        tile.color=Color.white;tile.transform=Matrix4x4.identity;
                        EditorUtility.SetDirty(tile);
                        map.SetTile(new Vector3Int(column+x,row,0),tile);
                    }
                    column+=item.width/16+1;
                }
                TileKitAssets.SavePalette(root,Root+"/TutorialDecoration.prefab");
                foreach(var item in items.Where(i=>!i.ground))
                {
                    var prop=new GameObject(item.name+"_Visual");
                    var art=new GameObject("Artwork",typeof(SpriteRenderer));
                    art.transform.SetParent(prop.transform,false);
                    art.transform.localPosition=new Vector3(0,item.height/32f,0);
                    var renderer=art.GetComponent<SpriteRenderer>();
                    renderer.sprite=sprites[item.name];
                    renderer.sortingLayerName=item.name=="FireRing" ? "Ground" : "World";
                    renderer.sortingOrder=item.name=="FireRing" ? 31 : 1;
                    PrefabUtility.SaveAsPrefabAsset(prop,VisualRoot+"/"+item.name+"_Visual.prefab");
                    Object.DestroyImmediate(prop);
                }
                AssetDatabase.SaveAssets();
            }
            finally { SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scratch,true); }
            Debug.Log("Tutorial Decoration: 12 designs, 12 ground tiles, 4 visual-only props.");
        }

        internal static void Dress(GameObject world)
        {
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(VisualRoot+"/FireRing_Visual.prefab"))return;
            foreach(string name in new[]{"Decoration Sample","Decoration Objects"})
            {
                var previous=world.transform.Find(name);
                if(previous)Object.DestroyImmediate(previous.gameObject);
            }
            // Use the shared template's existing layer, keeping a single ground-detail map.
            var map=world.transform.Find("Detail Decoration").GetComponent<Tilemap>();
            map.ClearAllTiles();
            var root=new GameObject("Decoration Objects");
            root.transform.SetParent(world.transform,false);
            Action<string,int,int> place=(name,x,y)=>{
                var item=Items().First(i=>i.name==name);
                for(int dx=0;dx<item.width/16;dx++)
                {
                    var tile=AssetDatabase.LoadAssetAtPath<Tile>(Root+"/Tiles/"+name+"_"+dx+".asset");
                    if(!tile)throw new InvalidOperationException("Missing decoration tile: "+name);
                    map.SetTile(new Vector3Int(x+dx,y,0),tile);
                }
            };
            Action<string,float,float> prop=(name,x,y)=>{
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(VisualRoot+"/"+name+"_Visual.prefab");
                if(!prefab)throw new InvalidOperationException("Missing visual: "+name);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);
                go.transform.localPosition=new Vector3(x,y,0);
            };
            // Approved corner: preserve its density and placements.
            place("MeadowFlowers",0,18);place("MeadowFlowers",5,22);place("MeadowFlowers",4,23);
            place("GrassTuft",2,16);place("GrassTuft",5,23);place("GrassTuft",0,20);place("GrassTuft",3,23);
            place("FallenLeaves",0,22);place("FallenLeaves",2,24);place("CarvedStone",7,22);
            place("WoodChips",1,13);place("WoodChips",3,14);
            prop("ClayJar",1.75f,18.125f);prop("WoodenBucket",8.25f,19.125f);
            prop("GrainSack",3.5f,10.125f);place("SplitLogs",3,12);
            // Two small home accents, orchard and chopping area.
            place("MeadowFlowers",5,30);place("MeadowFlowers",18,33);
            place("GrassTuft",10,32);place("LooseGrass",19,35);
            prop("ClayJar",10.5f,30.125f);prop("GrainSack",13.5f,33.125f);
            place("FallenLeaves",5,35);place("FallenLeaves",16,1);
            place("GrassTuft",10,1);place("LooseGrass",22,5);
            place("WoodChips",4,5);place("SplitLogs",1,4);
            // Scuffs belong only at the three practice positions.
            place("WornPractice",48,11);place("WornPractice",53,11);place("WornPractice",57,11);
            place("WoodChips",48,4);place("SplitLogs",58,5);
            place("GrassTuft",61,7);place("LooseGrass",61,18);
            // Quiet outskirts and a future rest-site visual, with clear approaches.
            place("LooseGrass",30,24);place("GrassTuft",31,27);
            place("CarvedStone",31,23);place("FallenLeaves",28,30);
            prop("FireRing",47,26);place("SplitLogs",49,26);
            place("LooseGrass",45,28);place("GrassTuft",46,28);
            place("CarvedStone",57,32);place("LooseGrass",58,33);
        }

        [MenuItem("Tools/The Lost Shrine/Dress Demo with Tutorial Decorations")]
        public static void PlaceInDemo()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/DemoTutorial.unity" || scene.isDirty)
                throw new InvalidOperationException("Open and save DemoTutorial before dressing it.");
            var world=scene.GetRootGameObjects().Single(g=>g.name=="Tutorial World");
            Dress(world);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
