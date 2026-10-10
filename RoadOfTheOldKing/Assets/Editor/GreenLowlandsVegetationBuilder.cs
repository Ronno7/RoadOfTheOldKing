using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.EditorTools
{
    // Regional density owns placement, while current scene surfaces/colliders remain authoritative.
    // Re-baking preserves a hand-painted density PNG and never repaints terrain or static decoration.
    public static class GreenLowlandsVegetationBuilder
    {
        public const string DensityPath = "Assets/Art/Vegetation/GreenLowlandsDensity.png";
        public const string LayoutPath = "Assets/Art/Vegetation/GreenLowlandsVegetation.asset";
        static readonly Vector2Int Origin = new Vector2Int(-14, -68);
        const int Width = 172, Height = 174;
        const float Clearance = .45f, Spacing = .3125f;

        sealed class Mask
        {
            public readonly HashSet<Vector2Int> grass = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> excluded = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> water = new HashSet<Vector2Int>();
            public readonly List<Vector3> clearings = new List<Vector3>();
            public bool Allows(Vector2 p)
            {
                for (int y=-1;y<=1;y++) for (int x=-1;x<=1;x++)
                {
                    var c=Vector2Int.FloorToInt(p+new Vector2(x,y)*Clearance);
                    if(!grass.Contains(c)||excluded.Contains(c)) return false;
                }
                foreach(var c in clearings)
                    if((p-new Vector2(c.x,c.y)).sqrMagnitude<c.z*c.z) return false;
                foreach(var c in Physics2D.OverlapCircleAll(p,Clearance))
                    if(c.enabled&&!c.isTrigger&&(c.attachedRigidbody==null||
                       c.attachedRigidbody.bodyType==RigidbodyType2D.Static)) return false;
                return true;
            }
            public bool NearWater(Vector2 p)
            {
                var c=Vector2Int.FloorToInt(p);
                for(int y=-2;y<=2;y++)for(int x=-2;x<=2;x++)
                    if(water.Contains(c+new Vector2Int(x,y)))return true;
                return false;
            }
        }

        static Mask ReadMask()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode ||
               SceneManager.GetActiveScene().path!="Assets/Scenes/GreenLowlands.unity")
                throw new InvalidOperationException("Open GreenLowlands in stopped Edit Mode.");
            var mask=new Mask();
            var roots=SceneManager.GetActiveScene().GetRootGameObjects();
            foreach(var map in roots.SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true)))
            {
                string n=map.name;
                bool grass=n=="Ground - regional meadow"||n=="Border grass"||
                    (n=="Grass"&&map.transform.parent.name=="Ground");
                bool water=n=="Pond and northern road-cut"||n=="Permanent river continuation"||
                    n=="Border river"||n=="MeadowWater"||n=="StoneWater"||n=="DeepWater";
                bool exclude=water||n=="Cliffs and outer boundary"||n=="Wooded banks - placeholder"||
                    n=="Basin paving - placeholder"||n=="Route traces - placeholder"||
                    n=="Border lane"||n=="Border collision"||n=="Paths - sluice paving"||
                    n=="Terrain - orchard ascent"||
                    n=="Collision"||n=="Detail Decoration"||
                    (map.transform.parent.name=="Paths")||
                    (map.transform.parent.name=="Ground"&&n!="Grass");
                if(!grass&&!exclude)continue;
                foreach(var cell in map.cellBounds.allPositionsWithin)
                {
                    if(!map.HasTile(cell))continue;
                    Vector3 p=map.GetCellCenterWorld(cell);
                    var c=new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y));
                    if(grass)mask.grass.Add(c);
                    if(exclude)mask.excluded.Add(c);
                    if(water)mask.water.Add(c);
                }
            }
            foreach(var c in roots.SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if(c==null)continue;
                string n=c.GetType().Name;
                float radius=c is WorldPickup?1.6f:n=="Bonfire"?2.8f:
                    n=="SceneSpawnPoint"||n=="SceneExit"?2f:
                    n.Contains("Rope")?2f:n=="RewardChest"||n.Contains("Inscription")?1.8f:0f;
                if(radius>0)mask.clearings.Add(new Vector3(c.transform.position.x,c.transform.position.y,radius));
            }
            // Keep both precision bays entirely quiet; also preserve the arrival's existing decoration.
            for(int y=61;y<=94;y++)for(int x=2;x<=13;x++)mask.excluded.Add(new Vector2Int(x,y));
            Physics2D.SyncTransforms();
            return mask;
        }

        static bool CombatInterior(Vector2 p)
        {
            return new Rect(74,26,20,12).Contains(p)||new Rect(109,84,14,8).Contains(p)||
                new Rect(133,46,8,6).Contains(p)||new Rect(3,-45,6,4).Contains(p)||
                new Rect(20,-24,8,4).Contains(p);
        }

        [MenuItem("Tools/Road of the Old King/Vegetation/Bake Green Lowlands Density")]
        public static void Bake()
        {
            var mask=ReadMask();
            if(!File.Exists(DensityPath))
            {
                var pixels=new Color32[Width*Height];
                for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
                {
                    var p=(Vector2)Origin+new Vector2(x+.5f,y+.5f);
                    float d=0;
                    if(mask.Allows(p))
                    {
                        float broad=Mathf.PerlinNoise(p.x*.12f+18.1f,p.y*.12f+31.7f);
                        d=Mathf.Lerp(.035f,.5f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.73f,broad)));
                        if(CombatInterior(p))d*=.17f;
                        if(p.y<18&&p.y>=0)d*=.45f;
                    }
                    byte v=(byte)Mathf.RoundToInt(d*255);
                    pixels[x+y*Width]=new Color32(v,v,v,255);
                }
                var tex=new Texture2D(Width,Height,TextureFormat.RGBA32,false,true);
                try {tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(DensityPath,tex.EncodeToPNG());}
                finally {UnityEngine.Object.DestroyImmediate(tex);}
            }
            AssetDatabase.ImportAsset(DensityPath,ImportAssetOptions.ForceSynchronousImport);
            var imp=(TextureImporter)AssetImporter.GetAtPath(DensityPath);
            imp.textureType=TextureImporterType.Default;imp.sRGBTexture=false;imp.isReadable=true;
            imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled=false;imp.wrapMode=TextureWrapMode.Clamp;
            imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=256;imp.SaveAndReimport();
            var density=AssetDatabase.LoadAssetAtPath<Texture2D>(DensityPath);
            if(density.width!=Width||density.height!=Height)throw new InvalidDataException("Density must be 172 x 174.");
            var tutorial=AssetDatabase.LoadAssetAtPath<VegetationLayout>(TutorialVegetationBuilder.LayoutPath);
            if(tutorial==null||tutorial.species.Length!=5)throw new InvalidDataException("Accepted Tutorial vegetation species missing.");
            var layout=AssetDatabase.LoadAssetAtPath<VegetationLayout>(LayoutPath);
            if(layout==null){layout=ScriptableObject.CreateInstance<VegetationLayout>();AssetDatabase.CreateAsset(layout,LayoutPath);}
            Undo.RecordObject(layout,"Bake Lowlands vegetation");
            layout.origin=Origin;layout.density=density;
            // Copy serialized metadata without changing the Tutorial asset or shared source sprites.
            var copy=UnityEngine.Object.Instantiate(tutorial);
            layout.species=copy.species;UnityEngine.Object.DestroyImmediate(copy);
            var plants=new List<VegetationLayout.Plant>();
            var buckets=new Dictionary<Vector2Int,List<Vector2>>();
            var input=density.GetPixels32();
            for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)for(int slot=0;slot<4;slot++)
            {
                uint key=unchecked((uint)(layout.seed+(x+y*Width)*17+slot*7919));
                if(Random01(key)>=input[x+y*Width].r/255f)continue;
                var p=(Vector2)Origin+new Vector2(x+.25f+(slot%2)*.5f+(Random01(key+1)-.5f)*.25f,
                    y+.25f+(slot/2)*.5f+(Random01(key+2)-.5f)*.25f);
                p=new Vector2(Mathf.Round(p.x*16)/16,Mathf.Round(p.y*16)/16);
                if(!mask.Allows(p)||!FarEnough(p,buckets))continue;
                float v=Random01(key+3);
                int species=v<.32f?0:v<.88f?1:v<.94f?2:3;
                if(mask.NearWater(p))species=4;
                plants.Add(new VegetationLayout.Plant{position=p,species=species,phase=Random01(key+4)*Mathf.PI*2});
                var cell=Vector2Int.FloorToInt(p/Spacing);
                if(!buckets.TryGetValue(cell,out var list))buckets.Add(cell,list=new List<Vector2>());
                list.Add(p);
            }
            layout.plants=plants.ToArray();EditorUtility.SetDirty(layout);AssetDatabase.SaveAssetIfDirty(layout);
            var root=GameObject.Find("Lowlands landscape art");
            var field=root.GetComponentsInChildren<InteractiveVegetation>(true).FirstOrDefault(f=>f.layout==layout);
            if(field==null){var go=new GameObject("Interactive Lowlands vegetation");go.transform.SetParent(root.transform,false);
                Undo.RegisterCreatedObjectUndo(go,"Create Lowlands vegetation");field=Undo.AddComponent<InteractiveVegetation>(go);}
            Undo.RecordObject(field,"Configure Lowlands vegetation");
            field.layout=layout;field.vegetationShader=Shader.Find("RoadOfTheOldKing/InteractiveVegetation");
            if(field.vegetationShader==null)throw new InvalidOperationException("Vegetation shader missing.");
            field.groundSortingOrder=55;
            field.interactor=UnityEngine.Object.FindFirstObjectByType<PlayerMovement>().transform;
            field.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);field.transform.localScale=Vector3.one;
            field.Rebuild();EditorUtility.SetDirty(field);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Lowlands density baked: "+plants.Count+" plants. "+ValidateLayout());
        }

        static float Random01(uint n)
        {
            unchecked{n^=n>>16;n*=0x7feb352du;n^=n>>15;n*=0x846ca68bu;n^=n>>16;}
            return(n&0xffffffu)/16777216f;
        }
        static bool FarEnough(Vector2 p,Dictionary<Vector2Int,List<Vector2>> buckets)
        {
            var cell=Vector2Int.FloorToInt(p/Spacing);
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
                if(buckets.TryGetValue(cell+new Vector2Int(x,y),out var list))
                    foreach(var other in list)if((p-other).sqrMagnitude<Spacing*Spacing-.00001f)return false;
            return true;
        }

        [MenuItem("Tools/Road of the Old King/Vegetation/Validate Green Lowlands Density")]
        public static void Validate()=>Debug.Log(ValidateLayout());
        public static string ValidateLayout()
        {
            var mask=ReadMask();var layout=AssetDatabase.LoadAssetAtPath<VegetationLayout>(LayoutPath);
            if(layout==null)return "No regional layout baked.";
            int invalid=0,overlap=0;var buckets=new Dictionary<Vector2Int,List<Vector2>>();
            foreach(var p in layout.plants)
            {
                if(!mask.Allows(p.position)||p.species<0||p.species>=layout.species.Length)invalid++;
                if(!FarEnough(p.position,buckets))overlap++;
                var c=Vector2Int.FloorToInt(p.position/Spacing);
                if(!buckets.TryGetValue(c,out var list))buckets.Add(c,list=new List<Vector2>());list.Add(p.position);
            }
            return invalid+" invalid roots, "+overlap+" spacing violations.";
        }
    }
}
