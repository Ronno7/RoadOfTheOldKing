using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using TheLostShrine.Combat;

namespace TheLostShrine.EditorTools
{
    // Deterministic native-grid export/import of the generated source drawings.
    public static class WolfAssets
    {
        public const string SheetPath = "Assets/Art/Sprites/Enemies/Wolf.png";
        public const string SetPath = "Assets/Animations/Enemies/Wolf.asset";
        private const int CellSize = 48;
        private const float PixelsPerUnit = 24f;
        private const float GroundPivot = 7.5f;
        private static readonly string[] SideNames = { "idle", "step0", "step1", "step2", "step3", "crouch", "bite", "dead" };
        private static readonly string[] DirectionNames = { "idle", "step0", "step1", "crouch", "bite", "dead" };
        private static readonly Color32[] Palette = {
            new Color32(37,34,39,255), new Color32(54,49,54,255), new Color32(76,70,72,255),
            new Color32(102,94,91,255), new Color32(144,133,116,255), new Color32(205,190,162,255), new Color32(184,59,43,255)
        };

        [MenuItem("Road of the Old King/Art/Rebuild Wolf Sprites")]
        public static void Rebuild()
        {
            string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../../ArtSource/Enemies/Wolf"));
            var atlas = new Texture2D(CellSize*8,CellSize*3,TextureFormat.RGBA32,false);
            atlas.SetPixels32(new Color32[CellSize*8*CellSize*3]);
            var rects = new List<SpriteRect>();
            Export(Path.Combine(source,"Side.png"),4,2,atlas,rects,false);
            if (File.Exists(Path.Combine(source,"Directions.png"))) Export(Path.Combine(source,"Directions.png"),3,4,atlas,rects,true);
            atlas.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(SheetPath));
            File.WriteAllBytes(SheetPath,atlas.EncodeToPNG());
            Object.DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(SheetPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(SheetPath);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=PixelsPerUnit; importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true; importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;
            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape=false;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories(); factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var previous=provider.GetSpriteRects().ToDictionary(r=>r.name,r=>r.spriteID);
            foreach(var rect in rects) rect.spriteID=previous.TryGetValue(rect.name,out var id)?id:GUID.Generate();
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s=>s.name);
            Directory.CreateDirectory(Path.GetDirectoryName(SetPath)); AssetDatabase.Refresh();
            var set=AssetDatabase.LoadAssetAtPath<WolfAnimationSet>(SetPath);
            if(set==null){set=ScriptableObject.CreateInstance<WolfAnimationSet>();AssetDatabase.CreateAsset(set,SetPath);}
            set.side=Poses(sprites,"side",4);
            set.south=sprites.ContainsKey("south_idle")?Poses(sprites,"south",2):set.side;
            set.north=sprites.ContainsKey("north_idle")?Poses(sprites,"north",2):set.side;
            EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
        }

        private static WolfAnimationSet.Poses Poses(Dictionary<string,Sprite> sprites,string direction,int steps) =>
            new WolfAnimationSet.Poses {idle=sprites[direction+"_idle"],crouch=sprites[direction+"_crouch"],bite=sprites[direction+"_bite"],dead=sprites[direction+"_dead"],
                move=Enumerable.Range(0,steps).Select(i=>sprites[direction+"_step"+i]).ToArray()};

        private static void Export(string path,int columns,int rows,Texture2D atlas,List<SpriteRect> rects,bool directions)
        {
            var source=new Texture2D(2,2);ImageConversion.LoadImage(source,File.ReadAllBytes(path));
            int width=source.width/columns,height=source.height/rows,count=columns*rows;
            var bounds=new RectInt[count];
            var masks=new bool[count][];
            for(int i=0;i<count;i++)
            {
                int x0=i%columns*width,y0=(rows-1-i/columns)*height,minx=width,miny=height,maxx=0,maxy=0;
                // Keep the complete connected animal, excluding faint matte and neighbouring
                // row fragments. Generated source sheets need not have exact cell padding.
                var visited=new bool[width*height];var largest=new List<int>();var queue=new Queue<int>();
                for(int start=0;start<visited.Length;start++)
                {
                    if(visited[start]||source.GetPixel(x0+start%width,y0+start/width).a<.8f)continue;
                    var component=new List<int>();visited[start]=true;queue.Enqueue(start);
                    while(queue.Count>0)
                    {
                        int point=queue.Dequeue();component.Add(point);int px=point%width,py=point/width;
                        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                        {
                            int nx=px+dx,ny=py+dy;if(nx<0||nx>=width||ny<0||ny>=height)continue;
                            int next=ny*width+nx;if(visited[next])continue;visited[next]=true;
                            if(source.GetPixel(x0+nx,y0+ny).a>=.8f)queue.Enqueue(next);
                        }
                    }
                    if(component.Count>largest.Count)largest=component;
                }
                masks[i]=new bool[width*height];foreach(int point in largest)masks[i][point]=true;
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(masks[i][y*width+x])
                {minx=Mathf.Min(minx,x);miny=Mathf.Min(miny,y);maxx=Mathf.Max(maxx,x);maxy=Mathf.Max(maxy,y);}
                bounds[i]=new RectInt(minx,miny,maxx-minx+1,maxy-miny+1);
            }
            // One uniform scale per sheet. Align feet without individually stretching poses.
            float scale=directions?33f/Mathf.Max(bounds[0].height,bounds[6].height):42f/bounds.Max(b=>b.width);
            for(int i=0;i<count;i++)
            {
                int row=directions?(i<6?1:2):0,column=directions?i%6:i;
                int ox=column*CellSize,oy=(2-row)*CellSize,x0=i%columns*width,y0=(rows-1-i/columns)*height;
                string name=(directions?(i<6?"south_":"north_"):"side_")+(directions?DirectionNames[i%6]:SideNames[i]);
                float center=directions?bounds[i].center.x:width*.5f;
                for(int y=0;y<CellSize;y++)for(int x=0;x<CellSize;x++)
                {
                    int sx=Mathf.FloorToInt(center+(x-(CellSize-1)*.5f)/scale),sy=Mathf.FloorToInt(bounds[i].yMin+(y-GroundPivot)/scale);
                    if(sx<0||sx>=width||sy<0||sy>=height)continue;
                    if(!masks[i][sy*width+sx])continue;
                    Color c=source.GetPixel(x0+sx,y0+sy);
                    Color32 best=Palette[0];float score=float.MaxValue;
                    foreach(Color32 candidate in Palette){Vector3 delta=new Vector3(c.r-candidate.r/255f,c.g-candidate.g/255f,c.b-candidate.b/255f);if(delta.sqrMagnitude<score){score=delta.sqrMagnitude;best=candidate;}}
                    atlas.SetPixel(ox+x,oy+y,best);
                }
                // Preserve the tiny authored eye accents when reducing to the native grid.
                // Each eye cluster becomes one pixel; no eyes are added to corpses or rear views.
                if(!name.EndsWith("dead")&&!name.StartsWith("north"))
                    for(int half=0;half<(directions?2:1);half++)
                    {
                        Vector2 sum=Vector2.zero;int samples=0;
                        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                        {
                            if(!masks[i][y*width+x]||(directions&&((x<center?0:1)!=half)))continue;
                            Color c=source.GetPixel(x0+x,y0+y);
                            if(c.r>c.g*1.5f&&c.r>c.b*1.5f&&c.r>.4f){sum+=new Vector2(x,y);samples++;}
                        }
                        if(samples>0){Vector2 eye=sum/samples;int x=Mathf.RoundToInt((eye.x-center)*scale+(CellSize-1)*.5f),y=Mathf.RoundToInt((eye.y-bounds[i].yMin)*scale+GroundPivot);if(x>=0&&x<CellSize&&y>=0&&y<CellSize)atlas.SetPixel(ox+x,oy+y,Palette[6]);}
                    }
                rects.Add(new SpriteRect{name=name,rect=new Rect(ox,oy,CellSize,CellSize),alignment=SpriteAlignment.Custom,pivot=new Vector2(.5f,GroundPivot/CellSize)});
            }
            Object.DestroyImmediate(source);
        }
    }
}
