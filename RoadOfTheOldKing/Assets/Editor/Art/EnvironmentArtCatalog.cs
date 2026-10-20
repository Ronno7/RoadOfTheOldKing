using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RoadOfTheOldKing.EditorTools
{
    // Current placement catalog and the shared native-source sampler. No legacy rebuild command.
    public static class EnvironmentArtCatalog
    {
        public const string Root = "Assets/Art/Tiles/Tutorial/Environment";
        public const string PrefabRoot = "Assets/Prefabs/Environment/Tutorial";
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

        public static string PrefabPath(string name, string regionalRoot = PrefabRoot)
        {
            if (name == "Oak" || name == "Birch" || name == "Apple_Tree")
                return "Assets/Prefabs/Environment/Shared/Trees/" + name + ".prefab";
            string shared = "Assets/Prefabs/Environment/Shared/" + name + ".prefab";
            return File.Exists(shared) ? shared : regionalRoot + "/" + name + ".prefab";
        }

        internal static Color32[] PrepareSprite(Texture2D source,Prop prop,Color32[] palette)
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

    }
}
