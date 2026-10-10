using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    // Native sampling/import only. Never repaints scenes or modifies gameplay prefabs.
    public static class RewardPropArtBuilder
    {
        public const string Root = "Assets/Art/Sprites/World/LowlandsRewards";
        [MenuItem("Tools/Road of the Old King/Art/Build Lowlands Reward Sprites")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Use stopped Edit Mode.");
            Directory.CreateDirectory(Root);
            var palette = new[]{"#543F36","#826047","#B58C61","#DFC291","#A17D3D","#E7B85C"}
                .Select(hex => { Color c; ColorUtility.TryParseHtmlString(hex, out c); return (Color32)c; }).ToArray();
            var chest = Load("TimberChest_Source");
            var pot = Load("ClayPot_Source");
            var atlas = new Texture2D(96,24,TextureFormat.RGBA32,false);
            try
            {
                var pixels = new Color32[96*24];
                // Equal source/native scale, common baseline and centered base across all lid poses.
                for (int i=0;i<3;i++)
                    Sample(chest, new RectInt(new[]{103,816,1529}[i],85,540,540), pixels, i*24,0,24,24,palette);
                // Register the unchanged lower body exactly; only the lid/interior changes.
                for(int i=1;i<3;i++) for(int y=0;y<8;y++) for(int x=0;x<24;x++)
                    pixels[y*96+i*24+x]=pixels[y*96+x];
                Sample(pot,new RectInt(420,231,534,616),pixels,74,1,12,14,palette.Take(5).ToArray());
                atlas.SetPixels32(pixels); atlas.Apply();
                File.WriteAllBytes(Root+"/LowlandsRewards16.png",atlas.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(chest); UnityEngine.Object.DestroyImmediate(pot); UnityEngine.Object.DestroyImmediate(atlas); }
            ImportSprites(Root+"/LowlandsRewards16.png",new[]{
                new Entry{name="TimberChest_Closed",x=0,y=0,width=24,height=24},
                new Entry{name="TimberChest_HalfOpen",x=24,y=0,width=24,height=24},
                new Entry{name="TimberChest_Open",x=48,y=0,width=24,height=24},
                new Entry{name="ClayPot",x=72,y=0,width=16,height=16}
            });
            BuildPickups();
            BuildTraversal();
            BuildTrader();
            AssetDatabase.SaveAssets();
        }

        static void BuildPickups()
        {
            var names=new[]{"SunShard","BronzeCoin","HeartFragment"};
            var crops=new[]{new RectInt(316,273,472,1021),new RectInt(664,125,759,758),new RectInt(531,338,387,362)};
            var palettes=new[]{
                new[]{"#826047","#B58C61","#E7B85C","#DFC291","#FFF0B3"},
                new[]{"#543F36","#826047","#B58C61","#DFC291"},
                new[]{"#703C3C","#A34E46","#CA6E57","#E6A275","#DFC291"}
            };
            var pixels=new Color32[96*24];
            for(int i=0;i<names.Length;i++)
            {
                var source=Load(names[i]+"_Source");
                try
                {
                    var palette=palettes[i].Select(hex=>{Color c;ColorUtility.TryParseHtmlString(hex,out c);return (Color32)c;}).ToArray();
                    Sample(source,crops[i],pixels,new[]{3,18,33}[i],new[]{2,2,2}[i],new[]{10,11,14}[i],new[]{20,11,12}[i],palette);
                }
                finally { UnityEngine.Object.DestroyImmediate(source); }
            }
            var atlas=new Texture2D(96,24,TextureFormat.RGBA32,false);
            try {atlas.SetPixels32(pixels);atlas.Apply();File.WriteAllBytes(Root+"/LowlandsPickups16.png",atlas.EncodeToPNG());}
            finally {UnityEngine.Object.DestroyImmediate(atlas);}
            ImportSprites(Root+"/LowlandsPickups16.png",new[]{
                new Entry{name="SunShard",x=0,y=0,width=16,height=24},
                new Entry{name="BronzeCoin",x=16,y=0,width=16,height=16},
                new Entry{name="HeartFragment",x=32,y=0,width=16,height=16}
            });
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
            try {atlas.SetPixels32(pixels);atlas.Apply();File.WriteAllBytes(Root+"/LowlandsTraversal16.png",atlas.EncodeToPNG());}
            finally {UnityEngine.Object.DestroyImmediate(atlas);}
            ImportSprites(Root+"/LowlandsTraversal16.png",new[]{
                new Entry{name="RopeAnchor",x=0,y=24,width=16,height=24},
                new Entry{name="ForgeCarving",x=16,y=24,width=24,height=24},
                new Entry{name="RopeSpan",x=0,y=0,width=64,height=8}
            });
        }

        static void BuildTrader()
        {
            var source=Load("TravelingTrader_Source");
            var atlas=new Texture2D(96,32,TextureFormat.RGBA32,false);
            try
            {
                var palette=new[]{"#393C39","#543F36","#826047","#B58C61","#DFC291","#A17D3D","#E7B85C","#536848","#798952","#A6AA8A"}
                    .Select(hex=>{Color c;ColorUtility.TryParseHtmlString(hex,out c);return (Color32)c;}).ToArray();
                var pixels=new Color32[96*32];
                Sample(source,new RectInt(288,267,510,910),pixels,4,2,16,28,palette);
                atlas.SetPixels32(pixels);atlas.Apply();File.WriteAllBytes(Root+"/LowlandsTrader16.png",atlas.EncodeToPNG());
            }
            finally {UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(atlas);}
            ImportSprites(Root+"/LowlandsTrader16.png",new[]{new Entry{name="TravelingTrader",x=0,y=0,width=24,height=32}});
        }

        static Texture2D Load(string name)
        {
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            if (!texture.LoadImage(File.ReadAllBytes("../ArtSource/GreenLowlands/Interactive/"+name+".png")))
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
