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
using static TheLostShrine.EditorTools.TileKitAssets;

namespace TheLostShrine.EditorTools
{
    /// <summary>Imports generated artwork into native pixels and stock Unity assets. No runtime generator.</summary>
    public static class TutorialEnvironmentKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/Tutorial/Environment";
        public const string PrefabRoot = "Assets/Prefabs/Environment/Tutorial";
        public const string VisualRoot = "Assets/Prefabs/WorldArt/Tutorial";
        [Serializable] public class Area { public int x,y,width,height; }
        [Serializable] public class Prop
        {
            public string name, source, kind;
            public Area sourceRect;
            public int width,height,overheadRow;
            public int[] swatches;
            public Area[] collision;
            public bool interactiveArt,native,stretch,hueAware;
        }
        [Serializable] public class Catalog { public Prop[] entries; }
        public static Prop[] ReadCatalog() => JsonUtility.FromJson<Catalog>(File.ReadAllText(Root+"/EnvironmentManifest.json")).entries;
        public static string TilePath(string name,int x,int y) => Root+"/Tiles/"+name+"_"+x+"_"+y+".asset";
        private static string PieceName(string name,int x,int y) => name+"_"+x+"_"+y;
        private static bool IsOverhead(Prop prop,int y) => prop.kind=="bridge" ? y==0||y==prop.height/16-1 : y>=prop.overheadRow;

        [MenuItem("Tools/The Lost Shrine/Build Tutorial Environment Kit")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode.");
            var previous=SceneManager.GetActiveScene();
            var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var sources=new Dictionary<string,Texture2D>();
            Texture2D atlas=null;
            try
            {
                SceneManager.SetActiveScene(scratch);
                foreach(var folder in new[]{Root+"/Tiles",Root+"/Palettes",PrefabRoot,VisualRoot})Directory.CreateDirectory(folder);
                var props=ReadCatalog();
                var palette=File.ReadAllLines("../Docs/Art/Palettes/tutorial-hearth-and-meadow-v1.gpl")
                    .Select(line=>line.Split((char[])null,StringSplitOptions.RemoveEmptyEntries))
                    .Where(parts=>parts.Length>=3&&byte.TryParse(parts[0],out _))
                    .Select(parts=>new Color32(byte.Parse(parts[0]),byte.Parse(parts[1]),byte.Parse(parts[2]),255)).ToArray();
                if(palette.Length!=16)throw new InvalidDataException("Expected 16 Tutorial palette colors.");
                foreach(var source in props.Select(p=>p.source).Distinct())
                {
                    var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    if(!texture.LoadImage(File.ReadAllBytes("../Docs/Art/Tutorial/Sources/Environment/"+source+".png")))throw new InvalidDataException(source);
                    sources.Add(source,texture);
                }
                // Preserve native reference sprites; constrain generated sources to the same palette.
                atlas=new Texture2D(512,512,TextureFormat.RGBA32,false);
                atlas.SetPixels32(new Color32[512*512]);
                var entries=new List<Entry>();int ax=2,ay=2,shelfHeight=0;
                foreach(var prop in props)
                {
                    if(ax+prop.width+2>512){ax=2;ay+=shelfHeight+4;shelfHeight=0;}
                    if(ay+prop.height+2>512)throw new InvalidOperationException("Environment atlas is full.");
                    var pixels=PrepareSprite(sources[prop.source],prop,palette);
                    if(pixels.Any(p=>p.a!=0&&(p.a!=255||!palette.Contains(p))))throw new InvalidDataException("Invalid palette/alpha: "+prop.name);
                    atlas.SetPixels32(ax,ay,prop.width,prop.height,pixels);
                    entries.Add(new Entry{name=prop.name,x=ax,y=ay,width=prop.width,height=prop.height});
                    if(!prop.interactiveArt)
                        for(int y=0;y<prop.height/16;y++)for(int x=0;x<prop.width/16;x++)
                            entries.Add(new Entry{name=PieceName(prop.name,x,y),x=ax+x*16,y=ay+y*16,width=16,height=16});
                    ax+=prop.width+4;shelfHeight=Math.Max(shelfHeight,prop.height);
                }
                atlas.Apply();File.WriteAllBytes(Root+"/TutorialEnvironment16.png",atlas.EncodeToPNG());
                AssetDatabase.Refresh();
                var sprites=ImportSprites(Root+"/TutorialEnvironment16.png",entries.ToArray());
                // This folder is generated. Remove old dimensions/names after the new atlas imports.
                var expected=new HashSet<string>(entries.Where(e=>e.width==16&&e.height==16).Select(e=>e.name));
                foreach(var path in Directory.GetFiles(Root+"/Tiles","*.asset"))
                    if(!expected.Contains(Path.GetFileNameWithoutExtension(path)))AssetDatabase.DeleteAsset(path.Replace('\\','/'));
                foreach(var path in Directory.GetFiles(PrefabRoot,"*.prefab"))
                    if(!props.Any(p=>!p.interactiveArt&&p.name==Path.GetFileNameWithoutExtension(path)))AssetDatabase.DeleteAsset(path.Replace('\\','/'));
                foreach(var prop in props.Where(p=>!p.interactiveArt))
                    for(int y=0;y<prop.height/16;y++)for(int x=0;x<prop.width/16;x++)
                    {
                        var tile=GetOrCreate<Tile>(TilePath(prop.name,x,y));
                        tile.sprite=sprites[PieceName(prop.name,x,y)];tile.colliderType=Tile.ColliderType.None;
                        tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(tile);
                    }
                var paletteObject=new GameObject("Tutorial Environment",typeof(Grid));
                var paletteMap=AddMap(paletteObject.transform,"Complete object stamps",0);
                for(int i=0;i<props.Length;i++)
                {
                    var prop=props[i];
                    if(prop.interactiveArt)
                    {
                        var visual=CreateVisual(prop,sprites[prop.name]);
                        PrefabUtility.SaveAsPrefabAsset(visual,VisualRoot+"/"+prop.name+"_Visual.prefab");Object.DestroyImmediate(visual);
                        continue;
                    }
                    var prefab=new GameObject(prop.name,typeof(Grid));
                    var baseMap=AddMap(prefab.transform,"Environment",0,"World");
                    var overhead=AddMap(prefab.transform,"Above Player",100,"Player");
                    var collision=AddMap(prefab.transform,"Collision",0);
                    ConfigureCollision(collision);
                    Stamp(prop,baseMap,overhead,collision,Vector3Int.zero);
                    PrefabUtility.SaveAsPrefabAsset(prefab,PrefabRoot+"/"+prop.name+".prefab");Object.DestroyImmediate(prefab);
                    for(int y=0;y<prop.height/16;y++)for(int x=0;x<prop.width/16;x++)
                        paletteMap.SetTile(new Vector3Int(i%4*12+x,-i/4*10+y,0),AssetDatabase.LoadAssetAtPath<Tile>(TilePath(prop.name,x,y)));
                }
                SavePalette(paletteObject,Root+"/Palettes/TutorialEnvironment.prefab");
                var template=TutorialTerrainKitBuilder.CreateTemplate();
                PrefabUtility.SaveAsPrefabAsset(template,TutorialTerrainKitBuilder.TemplatePath);Object.DestroyImmediate(template);
                AssetDatabase.SaveAssets();
                Debug.Log("Tutorial Environment: "+props.Length+" objects, "+entries.Count(e=>e.width==16&&e.height==16)+" tile pieces, one palette, separate overhead/collision and three visual-only practice props.");
            }
            finally
            {
                foreach(var source in sources.Values)Object.DestroyImmediate(source);
                if(atlas)Object.DestroyImmediate(atlas);
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scratch,true);
            }
        }

        private static Color32[] PrepareSprite(Texture2D source,Prop prop,Color32[] palette)
        {
            var a=prop.sourceRect;int left=a.x,right=a.x+a.width-1,top=a.y,bottom=a.y+a.height-1;
            if(left<0||top<0||right>=source.width||bottom>=source.height)throw new InvalidDataException("Crop outside source: "+prop.name);
            var input=source.GetPixels32();
            if(prop.native)
            {
                if(a.width!=prop.width||a.height!=prop.height)throw new InvalidDataException("Native size mismatch: "+prop.name);
                var native=new Color32[prop.width*prop.height];
                for(int y=0;y<prop.height;y++)for(int x=0;x<prop.width;x++)
                    native[y*prop.width+x]=input[(source.height-a.y-a.height+y)*source.width+a.x+x];
                return native;
            }
            int minX=right,minY=bottom,maxX=left,maxY=top;
            for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++)
                if(input[(source.height-1-y)*source.width+x].a>=128){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
            if(maxX<=minX||maxY<=minY)throw new InvalidDataException("Empty source: "+prop.name);
            int sw=maxX-minX+1,sh=maxY-minY+1;
            float scale=Math.Min((prop.width-2f)/sw,(prop.height-2f)/sh);
            int width=Math.Max(1,Mathf.RoundToInt(sw*scale)),height=Math.Max(1,Mathf.RoundToInt(sh*scale));
            if(prop.stretch){width=prop.width-2;height=prop.height-2;}
            int ox=(prop.width-width)/2,oy=1;
            var result=new Color32[prop.width*prop.height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int sx=minX+Math.Min(sw-1,(int)((x+.5f)*sw/width));
                int sy=maxY-Math.Min(sh-1,(int)((y+.5f)*sh/height));
                var pixel=input[(source.height-1-sy)*source.width+sx];
                if(pixel.a<128)continue;
                int best=0;float distance=float.MaxValue;
                foreach(int c in prop.swatches)
                {
                    int dr=pixel.r-palette[c].r,dg=pixel.g-palette[c].g,db=pixel.b-palette[c].b;
                    float d=dr*dr+dg*dg+db*db;
                    // Keep nearby wood/clay/straw values in their material hue ramp.
                    if(prop.hueAware)
                    {
                        Color.RGBToHSV(pixel,out float h,out float s,out _);
                        Color.RGBToHSV(palette[c],out float ph,out _,out _);
                        float dh=Mathf.Abs(h-ph);dh=Mathf.Min(dh,1-dh);
                        d+=Mathf.Pow(dh*1800,2)*Mathf.Clamp01(s*4);
                    }
                    if(d<distance){distance=d;best=c;}
                }
                result[(oy+y)*prop.width+ox+x]=palette[best];
            }
            return result;
        }

        private static void ConfigureCollision(Tilemap map)
        {
            map.gameObject.layer=LayerMask.NameToLayer("Environment");
            map.GetComponent<TilemapRenderer>().enabled=false;
            map.gameObject.AddComponent<TilemapCollider2D>();
        }

        public static void Stamp(Prop prop,Tilemap bases,Tilemap overhead,Tilemap collision,Vector3Int origin)
        {
            for(int y=0;y<prop.height/16;y++)for(int x=0;x<prop.width/16;x++)
            {
                var tile=AssetDatabase.LoadAssetAtPath<Tile>(TilePath(prop.name,x,y));
                if(!tile)throw new InvalidDataException("Build Environment first: "+prop.name);
                var cell=origin+new Vector3Int(x,y,0);
                (IsOverhead(prop,y)?overhead:bases).SetTile(cell,tile);
                (IsOverhead(prop,y)?bases:overhead).SetTile(cell,null);
            }
            var block=AssetDatabase.LoadAssetAtPath<Tile>(TutorialGroundKitBuilder.CollisionPath);
            if(!block)throw new InvalidDataException("Build Ground first.");
            foreach(var area in prop.collision)
                for(int y=area.y;y<area.y+area.height;y++)for(int x=area.x;x<area.x+area.width;x++)collision.SetTile(origin+new Vector3Int(x,y,0),block);
        }

        public static GameObject CreateVisual(Prop prop,Sprite sprite)
        {
            var root=new GameObject(prop.name+" Visual");
            var child=new GameObject("Artwork",typeof(SpriteRenderer));child.transform.SetParent(root.transform,false);
            child.transform.localPosition=new Vector3(0,prop.height/32f,0);
            var renderer=child.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingLayerName="World";
            return root;
        }
    }
}
