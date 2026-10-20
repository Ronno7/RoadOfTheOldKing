using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    // Native sampling/import only. Never repaints scenes or modifies gameplay prefabs.
    public static class WorldPropArtBuilder
    {
        public const string Root = "Assets/Art/Sprites/World/Shared/Rewards";
        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Shared/Refresh Rope Art")]
        public static void BuildRopeArt()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Use stopped Edit Mode.");
            Directory.CreateDirectory(Root);
            BuildTraversal();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Green Lowlands/Refresh Sluice Relief")]
        public static void BuildSluiceRelief()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Use stopped Edit Mode.");
            Directory.CreateDirectory("Assets/Art/Sprites/World/GreenLowlands");
            var source = Load("SluiceRelief_Source");
            var atlas = new Texture2D(96,32,TextureFormat.RGBA32,false);
            try
            {
                var palette = new[]{"#434944","#585F58","#7C8370","#A6AA8A","#D4CCAA","#4F7050"}
                    .Select(hex => { Color c; ColorUtility.TryParseHtmlString(hex, out c); return (Color32)c; }).ToArray();
                var pixels = new Color32[96*32];
                Sample(source,new RectInt(237,138,1301,583),pixels,0,1,64,29,palette);
                atlas.SetPixels32(pixels); atlas.Apply();
                File.WriteAllBytes("Assets/Art/Sprites/World/GreenLowlands/SluiceRelief16.png",atlas.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(atlas); }
            ImportSprites("Assets/Art/Sprites/World/GreenLowlands/SluiceRelief16.png",new[]{new Entry{name="SluiceRelief",x=0,y=0,width=64,height=32}});
        }

        static void BuildTraversal()
        {
            var names=new[]{"RopeAnchor","ForgeCarving","RopeSpan"};
            var crops=new[]{new RectInt(311,446,464,505),new RectInt(323,290,745,570),new RectInt(66,322,1867,147)};
            var palettes=new[]{
                new[]{"#585F58","#7C8370","#A6AA8A","#543F36","#B58C61","#4F7050"},
                new[]{"#48534F","#657266","#8B967B","#B1B69A","#536848","#798952"},
                new[]{"#543F36","#826047","#B58C61","#DFC291"}
            };
            var pixels=new Color32[96*48];
            for(int i=0;i<names.Length;i++)
            {
                var source=Load(names[i]+"_Source");
                try
                {
                    var palette=palettes[i].Select(hex=>{Color c;ColorUtility.TryParseHtmlString(hex,out c);return (Color32)c;}).ToArray();
                    Sample(source,crops[i],pixels,new[]{1,16,0}[i],new[]{26,27,1}[i],new[]{14,24,64}[i],new[]{16,18,5}[i],palette);
                }
                finally { UnityEngine.Object.DestroyImmediate(source); }
            }
            var atlas=new Texture2D(96,48,TextureFormat.RGBA32,false);
            try {atlas.SetPixels32(pixels);atlas.Apply();File.WriteAllBytes(Root+"/Traversal16.png",atlas.EncodeToPNG());}
            finally {UnityEngine.Object.DestroyImmediate(atlas);}
            ImportSprites(Root+"/Traversal16.png",new[]{
                new Entry{name="RopeAnchor",x=0,y=24,width=16,height=24},
                new Entry{name="ForgeCarving",x=16,y=24,width=24,height=24},
                new Entry{name="RopeSpan",x=0,y=0,width=64,height=8}
            });
        }

        static Texture2D Load(string name)
        {
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            if (!texture.LoadImage(File.ReadAllBytes((name == "SluiceRelief_Source" ? "../ArtSource/GreenLowlands/Interactive/" : "../ArtSource/Shared/Interactive/")+name+".png")))
                throw new InvalidDataException(name);
            return texture;
        }

        static void Sample(Texture2D source, RectInt crop, Color32[] output, int ox, int oy, int w, int h, Color32[] palette)
        {
            var input=source.GetPixels32();
            for(int y=0;y<h;y++) for(int x=0;x<w;x++)
            {
                int sx=crop.x+(int)((x+.5f)*crop.width/w), sy=crop.y+(int)((y+.5f)*crop.height/h);
                var c=input[sy*source.width+sx]; if(c.a<128)continue;
                int best=0,distance=int.MaxValue;
                for(int i=0;i<palette.Length;i++)
                {
                    var p=palette[i]; int d=(c.r-p.r)*(c.r-p.r)+(c.g-p.g)*(c.g-p.g)+(c.b-p.b)*(c.b-p.b);
                    if(d<distance){distance=d;best=i;}
                }
                output[(y+oy)*96+x+ox]=palette[best];
            }
        }
    }
}
