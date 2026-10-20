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
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    /// <summary>Shared field dressing with direct references to the shared meadow stamps.</summary>
    public static class FieldDetailKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/Shared/DetailDecoration/Field";
        public const string VisualRoot = "Assets/Prefabs/Decoration/Shared";
        const string MeadowRoot = MeadowDetailKitBuilder.Root;
        [Serializable] public class Item
        {
            public string name, source;
            public RectInt sourceRect;
            public int width, height, drawWidth, drawHeight;
            public int[] swatches;
            public bool ground;
        }
        [Serializable] public class Catalog { public Item[] items; public string[] reusedStamps, reusedVisuals; }
        [Serializable] sealed class MeadowStamp { public string name=null; public int width=0, height=0; public bool ground=false; }
        [Serializable] sealed class MeadowCatalog { public MeadowStamp[] items=null; }
        public static Catalog ReadCatalog() => JsonUtility.FromJson<Catalog>(File.ReadAllText(Root+"/DecorationManifest.json"));
        public static string TilePath(string name,int x,int y) => Root+"/Tiles/"+name+"_"+x+"_"+y+".asset";

        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Shared/Refresh Field Details")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode.");
            var catalog=ReadCatalog();
            var swatches=File.ReadAllLines("../Docs/Art/Palettes/green-lowlands-field-and-old-road-v1.gpl")
                .Select(line=>line.Split((char[])null,StringSplitOptions.RemoveEmptyEntries))
                .Where(parts=>parts.Length>=3&&byte.TryParse(parts[0],out _))
                .Select(parts=>new Color32(byte.Parse(parts[0]),byte.Parse(parts[1]),byte.Parse(parts[2]),255)).ToArray();
            if(swatches.Length!=16)throw new InvalidDataException("Expected Field and Old Road palette.");
            if(catalog.items.Select(i=>i.name).Distinct().Count()!=catalog.items.Length)throw new InvalidDataException("Duplicate decoration names.");
            var meadow=JsonUtility.FromJson<MeadowCatalog>(File.ReadAllText(MeadowRoot+"/DecorationManifest.json"));
            var retained=catalog.reusedStamps.Select(name=>meadow.items.Single(i=>i.name==name)).ToArray();
            foreach(var stamp in retained)
            {
                if(!stamp.ground||stamp.height!=16)throw new InvalidDataException("Shared meadow stamp is not a ground strip: "+stamp.name);
                for(int x=0;x<stamp.width/16;x++)
                    if(!AssetDatabase.LoadAssetAtPath<Tile>(MeadowRoot+"/Tiles/"+stamp.name+"_"+x+".asset"))
                        throw new InvalidDataException("Missing shared meadow tile: "+stamp.name);
            }
            foreach(string name in catalog.reusedVisuals)
                if(!AssetDatabase.LoadAssetAtPath<GameObject>(VisualRoot+"/"+name+".prefab"))
                    throw new InvalidDataException("Missing retained shared visual: "+name);
            var previous=SceneManager.GetActiveScene();
            var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            Texture2D atlas=null;
            try
            {
                SceneManager.SetActiveScene(scratch);
                Directory.CreateDirectory(Root+"/Tiles");Directory.CreateDirectory(VisualRoot);
                atlas=new Texture2D(256,128,TextureFormat.RGBA32,false);
                atlas.SetPixels32(new Color32[256*128]);
                var entries=new List<Entry>();int ax=2,ay=2,shelfHeight=0;
                foreach(var item in catalog.items)
                {
                    if(item.width<=0||item.height<=0||item.width%16!=0||item.height%16!=0||item.width>64||item.height>64)
                        throw new InvalidDataException("Decoration must fit its 4-cell palette slot: "+item.name);
                    if(item.drawWidth<=0||item.drawHeight<=0||item.drawWidth>item.width||item.drawHeight>item.height)
                        throw new InvalidDataException("Invalid native drawing bounds: "+item.name);
                    if(item.swatches==null||item.swatches.Length==0||item.swatches.Length>6||item.swatches.Any(i=>i<0||i>=swatches.Length))
                        throw new InvalidDataException("Decoration requires 1-6 regional swatches: "+item.name);
                    if(ax+item.width+2>atlas.width){ax=2;ay+=shelfHeight+4;shelfHeight=0;}
                    if(ax+item.width+2>atlas.width||ay+item.height+2>atlas.height)throw new InvalidDataException("Atlas capacity exceeded.");
                    var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    Color32[] pixels;
                    try
                    {
                        if(!source.LoadImage(File.ReadAllBytes("../ArtSource/Shared/Decoration/Field/"+item.source+".png")))throw new InvalidDataException(item.source);
                        pixels=PrepareSprite(source,item,swatches);
                    }
                    finally{Object.DestroyImmediate(source);}
                    if(!pixels.Any(p=>p.a==255))throw new InvalidDataException("Native decoration is empty: "+item.name);
                    atlas.SetPixels32(ax,ay,item.width,item.height,pixels);
                    entries.Add(new Entry{name=item.name,x=ax,y=ay,width=item.width,height=item.height});
                    for(int y=0;y<item.height/16;y++)for(int x=0;x<item.width/16;x++)
                        entries.Add(new Entry{name=item.name+"_"+x+"_"+y,x=ax+x*16,y=ay+y*16,width=16,height=16});
                    ax+=item.width+4;shelfHeight=Math.Max(shelfHeight,item.height);
                }
                atlas.Apply();File.WriteAllBytes(Root+"/FieldDetails16.png",atlas.EncodeToPNG());
                AssetDatabase.Refresh();
                var sprites=ImportSprites(Root+"/FieldDetails16.png",entries.ToArray());
                var palette=new GameObject("FieldDetails",typeof(Grid));
                palette.GetComponent<Grid>().cellSize=Vector3.one;
                var map=AddMap(palette.transform,"Complete decoration stamps",30);
                int index=0;
                foreach(var item in catalog.items)
                {
                    var origin=new Vector3Int(index%4*4,-index/4*4,0);
                    for(int y=0;y<item.height/16;y++)for(int x=0;x<item.width/16;x++)
                    {
                        var tile=GetOrCreate<Tile>(TilePath(item.name,x,y));
                        tile.sprite=sprites[item.name+"_"+x+"_"+y];tile.colliderType=Tile.ColliderType.None;
                        tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(tile);
                        map.SetTile(origin+new Vector3Int(x,y,0),tile);
                    }
                    if(!item.ground)
                    {
                        var prop=new GameObject(item.name);
                        var art=new GameObject("Artwork",typeof(SpriteRenderer));
                        art.transform.SetParent(prop.transform,false);
                        art.transform.localPosition=new Vector3(0,item.height/32f,0);
                        var renderer=art.GetComponent<SpriteRenderer>();
                        renderer.sprite=sprites[item.name];renderer.sortingLayerName="World";renderer.sortingOrder=1;
                        PrefabUtility.SaveAsPrefabAsset(prop,VisualRoot+"/"+item.name+".prefab");Object.DestroyImmediate(prop);
                    }
                    index++;
                }
                foreach(var stamp in retained)
                {
                    var origin=new Vector3Int(index%4*4,-index/4*4,0);
                    for(int x=0;x<stamp.width/16;x++)
                        map.SetTile(origin+new Vector3Int(x,0,0),AssetDatabase.LoadAssetAtPath<Tile>(MeadowRoot+"/Tiles/"+stamp.name+"_"+x+".asset"));
                    index++;
                }
                SavePalette(palette,Root+"/FieldDetails.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("Shared Field Details: "+catalog.items.Length+" field designs, "+retained.Length+" shared meadow stamps, "+catalog.reusedVisuals.Length+" retained visual props; authored scenes unchanged.");
            }
            finally
            {
                if(atlas)Object.DestroyImmediate(atlas);
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scratch,true);
            }
        }

        // Source rectangles are top-left; output and tile pieces use Unity's bottom-left convention.
        static Color32[] PrepareSprite(Texture2D source,Item item,Color32[] swatches)
        {
            var area=item.sourceRect;
            if(area.width<=0||area.height<=0||area.xMin<0||area.yMin<0||area.xMax>source.width||area.yMax>source.height)
                throw new InvalidDataException("Source rectangle outside image: "+item.name);
            var input=source.GetPixels32();
            int minX=source.width,minY=source.height,maxX=-1,maxY=-1;
            for(int y=area.yMin;y<area.yMax;y++)for(int x=area.xMin;x<area.xMax;x++)
                if(input[(source.height-1-y)*source.width+x].a>=128)
                {minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
            if(maxX<minX)throw new InvalidDataException("Empty source: "+item.name);
            int sw=maxX-minX+1,sh=maxY-minY+1;
            float scale=Math.Min((float)item.drawWidth/sw,(float)item.drawHeight/sh);
            int w=Math.Max(1,Mathf.RoundToInt(sw*scale)),h=Math.Max(1,Mathf.RoundToInt(sh*scale));
            int ox=(item.width-w)/2,oy=item.ground?(item.height-h)/2:0;
            var output=new Color32[item.width*item.height];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                int sx=minX+Math.Min(sw-1,(int)((x+.5f)*sw/w));
                int sy=maxY-Math.Min(sh-1,(int)((y+.5f)*sh/h));
                var c=input[(source.height-1-sy)*source.width+sx];if(c.a<128)continue;
                int best=item.swatches[0],distance=int.MaxValue;
                foreach(int i in item.swatches)
                {
                    var p=swatches[i];
                    int d=(c.r-p.r)*(c.r-p.r)+(c.g-p.g)*(c.g-p.g)+(c.b-p.b)*(c.b-p.b);
                    if(d<distance){distance=d;best=i;}
                }
                output[(oy+y)*item.width+ox+x]=swatches[best];
            }
            return output;
        }
    }
}
