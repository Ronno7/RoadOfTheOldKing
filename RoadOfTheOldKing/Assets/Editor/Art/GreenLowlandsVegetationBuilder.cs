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
        public const string LeafAtlas = "Assets/Art/Vegetation/LowlandsGrassLeaves16.png";
        static readonly Vector2Int Origin = new Vector2Int(-14, -68);
        const int Width = 172, Height = 174;
        const float Clearance = .45f, Spacing = .3125f;
        // Matches the shader's final +/-70 degree clamp. Only the editor bake uses this envelope.
        const float MaximumLeafAngle = 70f;

        static Color32[] GrassPalette()
        {
            var colors = Newtonsoft.Json.Linq.JObject.Parse(
                File.ReadAllText("../ArtSource/Shared/Palettes/Meadow.json"))["colors"];
            return new[] { "deepgreen", "leafshade", "grasspatch", "leaflight" }.Select(key => {
                if (!ColorUtility.TryParseHtmlString("#" + (string)colors[key], out var color))
                    throw new InvalidDataException("Missing Lowlands grass swatch: " + key);
                return (Color32)color;
            }).ToArray();
        }

        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Shared/Refresh Grass Atlas")]
        public static void BuildLeafArt()
        {
            TutorialVegetationBuilder.BuildLeafArt("../ArtSource/Shared/Vegetation/LowlandsGrassLeaves_Source.png",
                LeafAtlas, GrassPalette(), 16, 12, 7);
        }

        static VegetationLayout.Species[] ReadSpecies()
        {
            var tutorial = AssetDatabase.LoadAssetAtPath<VegetationLayout>(TutorialVegetationBuilder.LayoutPath);
            if (tutorial == null || tutorial.species.Length != 5)
                throw new InvalidDataException("Accepted Tutorial vegetation species missing.");
            var sprites = AssetDatabase.LoadAllAssetsAtPath(LeafAtlas).OfType<Sprite>().ToDictionary(s => s.name);
            if (sprites.Count != 6) throw new InvalidDataException("Run Shared > Refresh Grass Atlas first.");
            var copy = UnityEngine.Object.Instantiate(tutorial);
            var species = copy.species;
            UnityEngine.Object.DestroyImmediate(copy);
            var angles = new[] { new[] { 4f, -6f }, new[] { 3f, -5f }, new[] { 0f },
                new[] { 8f, -10f }, new[] { 0f } };
            var palette = GrassPalette();
            for (int s = 0; s < species.Length; s++)
                for (int l = 0; l < species[s].leaves.Length; l++)
                {
                    var leaf = species[s].leaves[l];
                    leaf.sprite = sprites[leaf.sprite.name];
                    if (leaf.sprite.name.StartsWith("GrassLeaf_", StringComparison.Ordinal))
                    {
                        leaf.restAngle = angles[s][l];
                        species[s].debrisColor = palette[2];
                    }
                }
            return species;
        }

        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Green Lowlands/Refresh Vegetation Art")]
        public static void RefreshSpecies()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit Mode.");
            var layout = AssetDatabase.LoadAssetAtPath<VegetationLayout>(LayoutPath);
            if (layout == null) throw new InvalidOperationException("Bake Lowlands density first.");
            var species = ReadSpecies();
            if (layout.plants.Any(p => p.species < 0 || p.species >= species.Length))
                throw new InvalidDataException("Existing layout has an unknown species slot.");
            Undo.RecordObject(layout, "Refresh Lowlands leaf art");
            layout.species = species;
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssetIfDirty(layout);
            foreach (var field in UnityEngine.Object.FindObjectsByType<InteractiveVegetation>(FindObjectsSortMode.None))
                if (field.layout == layout) field.Rebuild();
            Debug.Log("Lowlands leaf art refreshed; density, plant records and scenes preserved.");
        }

        internal sealed class Mask
        {
            public readonly HashSet<Vector2Int> grass = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> excluded = new HashSet<Vector2Int>();
            public readonly HashSet<Vector2Int> structure = new HashSet<Vector2Int>();
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
            public bool Allows(Vector2 p, Rect canopy)
            {
                if(!Allows(p))return false;
                // Root clearance alone misses tall leaves leaning sideways over a neighboring face.
                // Check the swept sprite footprint against authored structure, not all walkable turf.
                int x0=Mathf.FloorToInt(p.x+canopy.xMin), x1=Mathf.FloorToInt(p.x+canopy.xMax-.0001f);
                int y0=Mathf.FloorToInt(p.y+canopy.yMin), y1=Mathf.FloorToInt(p.y+canopy.yMax-.0001f);
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                    if(structure.Contains(new Vector2Int(x,y)))return false;
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

        internal static Mask ReadMask()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode ||
               SceneManager.GetActiveScene().path!="Assets/Scenes/GreenLowlands.unity")
                throw new InvalidOperationException("Open GreenLowlands in stopped Edit Mode.");
            var mask=new Mask();
            var structuralTiles=new Dictionary<TileBase,bool>();
            var roots=SceneManager.GetActiveScene().GetRootGameObjects();
            var topHeights=new Dictionary<Vector2Int,int>();
            foreach(var map in roots.SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true)))
            {
                int h;bool top;
                if(!GreenLowlandsShadowBuilder.TryTerrainLayer(map,out h,out top)||!top)continue;
                foreach(var cell in map.cellBounds.allPositionsWithin)if(map.HasTile(cell))
                {
                    var p=Vector2Int.FloorToInt(map.GetCellCenterWorld(cell));
                    int previous;
                    if(!topHeights.TryGetValue(p,out previous)||h>previous)topHeights[p]=h;
                }
            }
            var elevation=roots.SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true))
                .FirstOrDefault(m=>m.name==LowlandsElevationAuthoring.ElevationName);
            foreach(var map in roots.SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true)))
            {
                string n=map.name;
                int terrainHeight; bool terrainTop;
                bool terrainLayer=GreenLowlandsShadowBuilder.TryTerrainLayer(map,out terrainHeight,out terrainTop);
                // Camera coverage is render-only: it neither permits roots nor excludes the scene
                // beneath it, even when the backdrop lives under the arrival Ground group.
                if(n=="Meadow Backdrop")continue;
                bool grass=n=="Ground - regional meadow"||n=="Border grass"||
                    (n=="Grass"&&map.transform.parent.name=="Ground");
                bool water=n=="Flooded pond and road-cut"||n=="Permanent river continuation"||
                    n=="Border river"||n=="MeadowWater"||n=="StoneWater"||n=="DeepWater";
                bool exclude=water||n=="Cliffs and outer boundary"||n=="Woodland barriers"||
                    n=="Basin exclusion mask"||n=="Path exclusion mask"||
                    n=="Border lane"||n=="Border collision"||n=="Paths - sluice paving"||
                    n=="Ground - worksite paving"||
                    n=="Ground - storehouse threshold paving"||
                    n=="Terrain - orchard ascent"||n=="Terrain - height connectors"||
                    n=="Terrain - submerged height connectors"||
                    n=="Terrain - structural bank edges"||n=="Terrain - shard island masonry"||
                    n=="Terrain - island worn paving"||n=="Terrain - boundary uplands"||n=="Farm hedge crowns"||
                    n=="Terrain - village hedgerows"||terrainLayer&&!terrainTop||
                    n=="Height boundaries"||
                    n=="Arrival collision"||n=="Collision"||n=="Detail Decoration"||
                    (map.transform.parent.name=="Paths")||
                    (map.transform.parent.name=="Ground"&&n!="Grass");
                foreach(var cell in map.cellBounds.allPositionsWithin)
                {
                    var tile=map.GetTile(cell);
                    if(tile==null)continue;
                    if(!structuralTiles.TryGetValue(tile,out bool structural))
                        structuralTiles.Add(tile,structural=IsStructuralTile(tile));
                    if(terrainLayer&&!terrainTop)structural=true;
                    bool blockedTop=false;
                    if(terrainLayer&&terrainTop)
                    {
                        // Lower footprints deliberately continue beneath higher
                        // terrain. Only the visible upper surface owns planting.
                        var worldCell=Vector2Int.FloorToInt(map.GetCellCenterWorld(cell));
                        if(topHeights[worldCell]>terrainHeight)continue;
                        int height;
                        bool floor=elevation!=null&&LowlandsElevationAuthoring.TryHeight(
                            LowlandsElevationAuthoring.ReadClassification(elevation,elevation.WorldToCell(map.GetCellCenterWorld(cell))),out height)&&height==terrainHeight;
                        // A top-map boundary can be a flat shore or material edge.
                        // Actual faces and collision own drop clearance; do not
                        // infer a wall from a missing neighboring cap tile.
                        blockedTop=!floor;
                    }
                    if(!grass&&!exclude&&!structural&&!blockedTop)continue;
                    Vector3 p=map.GetCellCenterWorld(cell);
                    var c=new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y));
                    if(grass)mask.grass.Add(c);
                    if(exclude||structural||blockedTop)mask.excluded.Add(c);
                    if(structural||blockedTop)mask.structure.Add(c);
                    // Complete cliff sprites extend three cells below their painted top.
                    if(tile is RaisedCliffTile)for(int drop=1;drop<=((RaisedCliffTile)tile).projectedRise;drop++)
                    {
                        var faceCell=c+Vector2Int.down*drop;
                        mask.excluded.Add(faceCell);mask.structure.Add(faceCell);
                    }
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

        static bool IsStructuralTile(TileBase tile)
        {
            if(tile is RaisedCliffTile)return true;
            var terrain=tile as TerrainRuleTile;
            if(terrain!=null&&terrain.part==TerrainRuleTile.Part.Face)return true;
            // A hand-painted face can live on the cap map or on a later overlay. The authored
            // tile family is authoritative; renaming a scene layer must not make that face turf.
            string path=AssetDatabase.GetAssetPath(tile), n=tile.name;
            if(path.StartsWith("Assets/Art/Tiles/Shared/Terrain/",StringComparison.Ordinal))
                return n=="Paint_CliffEarthFace"||n=="Paint_CliffStoneFace"||n=="Paint_CliffStoneTop"||
                    n.StartsWith("CliffEarthFace_",StringComparison.Ordinal)||
                    n.StartsWith("CliffStoneFace_",StringComparison.Ordinal)||
                    n.StartsWith("EarthCliff_",StringComparison.Ordinal)||
                    n.StartsWith("StoneCliff_",StringComparison.Ordinal)||
                    n.StartsWith("ShoreFace_",StringComparison.Ordinal)||
                    n.StartsWith("ShoreReturn_",StringComparison.Ordinal)||
                    n.StartsWith("ShoreInside_",StringComparison.Ordinal)||
                    n.StartsWith("ShoreLip_",StringComparison.Ordinal)||
                    n.StartsWith("Bank_",StringComparison.Ordinal)||
                    n.StartsWith("Stone_",StringComparison.Ordinal)||
                    n.StartsWith("GrassLedge_",StringComparison.Ordinal)||
                    n.StartsWith("StoneLedge_",StringComparison.Ordinal)||
                    n=="Paint_GrassLedge"||n=="Paint_StoneLedge";
            if(path.StartsWith("Assets/Art/Tiles/GreenLowlands/Environment/Architecture/Tiles/",StringComparison.Ordinal))
                return n.StartsWith("Foundation_",StringComparison.Ordinal)||n.StartsWith("Wall_",StringComparison.Ordinal);
            return false;
        }

        static Rect CanopyBounds(VegetationLayout.Species species)
        {
            float minX=0,minY=0,maxX=0,maxY=0;
            if(species!=null&&species.leaves!=null)foreach(var leaf in species.leaves)
            {
                if(leaf==null||leaf.sprite==null)continue;
                var s=leaf.sprite;
                float left=-s.pivot.x/s.pixelsPerUnit,right=(s.rect.width-s.pivot.x)/s.pixelsPerUnit;
                float bottom=-s.pivot.y/s.pixelsPerUnit,top=(s.rect.height-s.pivot.y)/s.pixelsPerUnit;
                for(int corner=0;corner<4;corner++)
                {
                    var v=new Vector2((corner&1)==0?left:right,(corner&2)==0?bottom:top);
                    if(leaf.flipX)v.x=-v.x;
                    float phase=Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg;
                    // End angles plus axis extrema give the full continuous sweep, not a few poses.
                    for(int sample=0;sample<6;sample++)
                    {
                        float angle=sample==0?-MaximumLeafAngle:sample==1?MaximumLeafAngle:
                            Mathf.DeltaAngle(0,-phase+(sample-2)*90);
                        if(Mathf.Abs(angle)>MaximumLeafAngle)continue;
                        float a=angle*Mathf.Deg2Rad,cos=Mathf.Cos(a),sin=Mathf.Sin(a);
                        var q=leaf.offset+new Vector2(v.x*cos-v.y*sin,v.x*sin+v.y*cos);
                        minX=Mathf.Min(minX,q.x);minY=Mathf.Min(minY,q.y);
                        maxX=Mathf.Max(maxX,q.x);maxY=Mathf.Max(maxY,q.y);
                    }
                }
            }
            const float pixel=1f/16;
            return Rect.MinMaxRect(minX-pixel,minY-pixel,maxX+pixel,maxY+pixel);
        }

        internal static bool CombatInterior(Vector2 p)
        {
            return new Rect(74,26,20,12).Contains(p)||new Rect(109,84,14,8).Contains(p)||
                new Rect(133,46,8,6).Contains(p)||new Rect(-5,-43,17,11).Contains(p)||
                new Rect(16,-23,17,8).Contains(p)||new Rect(40,-24,7,9).Contains(p);
        }


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
            var speciesMetadata=ReadSpecies();
            var canopyBounds=speciesMetadata.Select(CanopyBounds).ToArray();
            var layout=AssetDatabase.LoadAssetAtPath<VegetationLayout>(LayoutPath);
            int seed=layout!=null?layout.seed:1703;
            var plants=new List<VegetationLayout.Plant>();
            var buckets=new Dictionary<Vector2Int,List<Vector2>>();
            var input=density.GetPixels32();
            for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)for(int slot=0;slot<4;slot++)
            {
                uint key=unchecked((uint)(seed+(x+y*Width)*17+slot*7919));
                if(Random01(key)>=input[x+y*Width].r/255f)continue;
                var p=(Vector2)Origin+new Vector2(x+.25f+(slot%2)*.5f+(Random01(key+1)-.5f)*.25f,
                    y+.25f+(slot/2)*.5f+(Random01(key+2)-.5f)*.25f);
                p=new Vector2(Mathf.Round(p.x*16)/16,Mathf.Round(p.y*16)/16);
                float v=Random01(key+3);
                int species=v<.32f?0:v<.88f?1:v<.94f?2:3;
                if(mask.NearWater(p))species=4;
                if(!mask.Allows(p,canopyBounds[species])||!FarEnough(p,buckets))continue;
                plants.Add(new VegetationLayout.Plant{position=p,species=species,phase=Random01(key+4)*Mathf.PI*2});
                var cell=Vector2Int.FloorToInt(p/Spacing);
                if(!buckets.TryGetValue(cell,out var list))buckets.Add(cell,list=new List<Vector2>());
                list.Add(p);
            }
            // A broken scene mask must not silently erase an accepted field. An all-black density
            // remains a deliberate way to clear it, but nonempty authoring input needs some roots.
            if(plants.Count==0&&input.Any(p=>p.r>0))
                throw new InvalidDataException("Nonempty Lowlands density produced zero plants. Check scene masks; existing layout was not changed.");
            if(layout==null){layout=ScriptableObject.CreateInstance<VegetationLayout>();AssetDatabase.CreateAsset(layout,LayoutPath);}
            Undo.RecordObject(layout,"Bake Lowlands vegetation");
            layout.origin=Origin;layout.density=density;
            layout.species=speciesMetadata;
            layout.plants=plants.ToArray();EditorUtility.SetDirty(layout);AssetDatabase.SaveAssetIfDirty(layout);
            var root=GameObject.Find("Green Lowlands/Scenery");
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


        public static void Validate()=>Debug.Log(ValidateLayout());
        public static string ValidateLayout()
        {
            var mask=ReadMask();var layout=AssetDatabase.LoadAssetAtPath<VegetationLayout>(LayoutPath);
            if(layout==null)return "No regional layout baked.";
            if(layout.plants==null||layout.plants.Length==0)return "Invalid regional layout: 0 plants. Check density and scene masks.";
            int invalid=0,overlap=0;var buckets=new Dictionary<Vector2Int,List<Vector2>>();
            var canopyBounds=layout.species.Select(CanopyBounds).ToArray();
            foreach(var p in layout.plants)
            {
                if(p.species<0||p.species>=layout.species.Length||
                   !mask.Allows(p.position,canopyBounds[p.species]))invalid++;
                if(!FarEnough(p.position,buckets))overlap++;
                var c=Vector2Int.FloorToInt(p.position/Spacing);
                if(!buckets.TryGetValue(c,out var list))buckets.Add(c,list=new List<Vector2>());list.Add(p.position);
            }
            return invalid+" invalid roots, "+overlap+" spacing violations.";
        }
    }
}
