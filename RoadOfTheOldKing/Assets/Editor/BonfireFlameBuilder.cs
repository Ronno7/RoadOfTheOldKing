using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    /// <summary>Native flame frames with a common baseline and sampling scale. Does not author scenes.</summary>
    public static class BonfireFlameBuilder
    {
        public const string Root = "Assets/Art/Sprites/World/Bonfire";
        [Serializable] public class Manifest
        {
            public string source;
            public RectInt[] frames;
            public int width, height, drawWidth, drawHeight;
            public string[] colors;
        }
        public static Manifest ReadManifest() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/BonfireFlameManifest.json"));

        [MenuItem("Tools/Road of the Old King/Build Shared Bonfire Flame")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode.");
            var manifest=ReadManifest();
            if(manifest.frames.Length!=4||manifest.width!=16||manifest.height!=24||manifest.colors.Length>5)
                throw new InvalidDataException("Expected four native 16x24 frames and at most five colors.");
            if(manifest.drawWidth<=0||manifest.drawHeight<=0||manifest.drawWidth>manifest.width||manifest.drawHeight>manifest.height)
                throw new InvalidDataException("Invalid flame drawing bounds.");
            var palette=manifest.colors.Select(hex=>{
                Color color;if(!ColorUtility.TryParseHtmlString(hex,out color))throw new InvalidDataException(hex);
                return (Color32)color;
            }).ToArray();
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
            var atlas=new Texture2D(64,32,TextureFormat.RGBA32,false);
            var entries=new List<Entry>();
            try
            {
                if(!source.LoadImage(File.ReadAllBytes("../ArtSource/GreenLowlands/Interactive/"+manifest.source+".png")))
                    throw new InvalidDataException(manifest.source);
                var input=source.GetPixels32();
                int minX=int.MaxValue,minY=int.MaxValue,maxX=-1,maxY=-1;
                foreach(var frame in manifest.frames)
                {
                    if(frame.width<=0||frame.height<=0||frame.xMin<0||frame.yMin<0||frame.xMax>source.width||frame.yMax>source.height)
                        throw new InvalidDataException("Flame source frame exceeds image bounds.");
                    for(int y=0;y<frame.height;y++)for(int x=0;x<frame.width;x++)
                        if(input[(source.height-1-frame.y-y)*source.width+frame.x+x].a>=128)
                        {minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
                }
                if(maxX<minX)throw new InvalidDataException("Empty flame source.");
                int sw=maxX-minX+1,sh=maxY-minY+1;
                float scale=Math.Min((float)manifest.drawWidth/sw,(float)manifest.drawHeight/sh);
                int w=Math.Max(1,Mathf.RoundToInt(sw*scale)),h=Math.Max(1,Mathf.RoundToInt(sh*scale));
                int ox=(manifest.width-w)/2;
                var output=new Color32[64*32];
                for(int n=0;n<manifest.frames.Length;n++)
                {
                    var frame=manifest.frames[n];bool nonempty=false;
                    for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                    {
                        int sx=frame.x+minX+Math.Min(sw-1,(int)((x+.5f)*sw/w));
                        int sy=frame.y+maxY-Math.Min(sh-1,(int)((y+.5f)*sh/h));
                        var color=input[(source.height-1-sy)*source.width+sx];if(color.a<128)continue;
                        int best=0,distance=int.MaxValue;
                        for(int i=0;i<palette.Length;i++)
                        {
                            var p=palette[i];
                            int d=(color.r-p.r)*(color.r-p.r)+(color.g-p.g)*(color.g-p.g)+(color.b-p.b)*(color.b-p.b);
                            if(d<distance){distance=d;best=i;}
                        }
                        output[y*64+n*manifest.width+ox+x]=palette[best];nonempty=true;
                    }
                    if(!nonempty)throw new InvalidDataException("Empty native flame frame "+n);
                    entries.Add(new Entry{name="BonfireFlame_"+n,x=n*manifest.width,y=0,width=manifest.width,height=manifest.height});
                }
                atlas.SetPixels32(output);atlas.Apply();
                File.WriteAllBytes(Root+"/BonfireFlame16.png",atlas.EncodeToPNG());
            }
            finally{Object.DestroyImmediate(source);Object.DestroyImmediate(atlas);}
            ImportSprites(Root+"/BonfireFlame16.png",entries.ToArray());
            AssetDatabase.SaveAssets();
            Debug.Log("Shared Bonfire Flame: four 16x24 frames, five-color limit, fixed registration; scenes unchanged.");
        }
    }
}
