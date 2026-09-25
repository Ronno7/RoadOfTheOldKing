using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace TheLostShrine.EditorTools
{
    // One-shot authoring pass. Refuses to repaint an existing terrain layout.
    public static class TutorialZoneTerrainPass
    {
        const string Terrain = "Assets/Art/Tiles/Tutorial/Terrain";
        const string Ground = "Assets/Art/Tiles/Tutorial/Ground";
        static readonly Vector3Int[] Directions = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
        static TileBase Load(string path) { var t = AssetDatabase.LoadAssetAtPath<TileBase>(path); if (!t) throw new Exception(path); return t; }
        static Vector3Int P(int x, int y) { return new Vector3Int(x,y,0); }
        static void Oval(HashSet<Vector3Int> set, float x, float y, float rx, float ry)
        {
            for(int j=Mathf.FloorToInt(y-ry);j<=Mathf.CeilToInt(y+ry);j++)
                for(int i=Mathf.FloorToInt(x-rx);i<=Mathf.CeilToInt(x+rx);i++)
                    if(Mathf.Pow((i+.5f-x)/rx,2)+Mathf.Pow((j+.5f-y)/ry,2)<=1) set.Add(P(i,j));
        }
        static void Lane(HashSet<Vector3Int> set, float radius, params Vector2[] points)
        {
            for(int i=1;i<points.Length;i++) {
                int steps=Mathf.CeilToInt(Vector2.Distance(points[i-1],points[i])*3);
                for(int k=0;k<=steps;k++) { var p=Vector2.Lerp(points[i-1],points[i],(float)k/steps); Oval(set,p.x,p.y,radius,radius); }
            }
        }
        static void Paint(Tilemap map, HashSet<Vector3Int> cells, TileBase tile)
        { var positions=cells.ToArray(); map.SetTiles(positions,Enumerable.Repeat(tile,positions.Length).ToArray()); }
        static Tilemap Map(Transform parent,string name,int order,string layer="Ground")
        {
            var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer)); go.transform.SetParent(parent,false);
            var r=go.GetComponent<TilemapRenderer>(); r.sortingLayerName=layer;r.sortingOrder=order;
            return go.GetComponent<Tilemap>();
        }

        public static string Build()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.path!="Assets/Scenes/Tutorial.unity") throw new Exception("Open Tutorial in Edit Mode.");
            var world=scene.GetRootGameObjects().Single(g=>g.GetComponent<Grid>());
            var maps=world.GetComponentsInChildren<Tilemap>().ToDictionary(m=>m.name);
            if(maps.Where(k=>k.Key!="Grass").Any(k=>k.Value.GetUsedTilesCount()>0) || world.transform.Find("Boundary Woodland"))
                throw new Exception("Terrain already authored; edit the scene directly instead of repainting it.");
            if(maps["Grass"].cellBounds.xMin!=0 || maps["Grass"].cellBounds.yMin!=0 || maps["Grass"].cellBounds.size!=new Vector3Int(100,100,1))
                throw new Exception("Expected the approved 0..99 grass canvas.");
            Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Build tutorial terrain and boundaries");
            if(PrefabUtility.IsPartOfPrefabInstance(world)) PrefabUtility.UnpackPrefabInstance(world,PrefabUnpackMode.Completely,InteractionMode.UserAction);
            foreach(var m in maps.Values) Undo.RegisterCompleteObjectUndo(m,"Paint tutorial terrain");

            var clear=new HashSet<Vector3Int>();
            Oval(clear,18,16,13,8); // Home and free movement.
            Oval(clear,39,28,8,7); // Hatchet work clearing.
            Lane(clear,2.4f,new Vector2(28,18),new Vector2(33,20),new Vector2(36,26));
            Lane(clear,2.1f,new Vector2(40,31),new Vector2(40,43));
            Oval(clear,15,40,8,7); // Optional village well.
            Lane(clear,2,new Vector2(16,22),new Vector2(17,28),new Vector2(14,34),new Vector2(14,38));

            var terrace=new HashSet<Vector3Int>();
            for(int y=42;y<=63;y++) for(int x=34;x<=57;x++)
                if((x<=52 || y>=55) && !(x<37 && y>60) && !(x>54 && y>61)) terrace.Add(P(x,y));
            clear.UnionWith(terrace);
            Lane(clear,2.2f,new Vector2(55.5f,57),new Vector2(55.5f,51.5f),new Vector2(60,51.5f),new Vector2(61,55.5f),new Vector2(73,55.5f));
            Oval(clear,60,69,3.5f,3); // Short river overlook spur.
            Lane(clear,1.7f,new Vector2(61,56),new Vector2(61,64),new Vector2(60,68));
            Oval(clear,86,64,9,8); // Combat clearing with retreat space.
            Lane(clear,2.5f,new Vector2(72,55.5f),new Vector2(77,57),new Vector2(82,62));
            Oval(clear,87,82,8,6); // Quiet rest pocket.
            Lane(clear,2,new Vector2(85,70),new Vector2(82,74),new Vector2(84,79));
            Lane(clear,2.3f,new Vector2(88,86),new Vector2(90,90),new Vector2(88,96),new Vector2(88,101));

            var water=new HashSet<Vector3Int>();
            for(int y=0;y<=89;y++) {
                int left=63+Mathf.RoundToInt(2*Mathf.Sin((y-54)*.105f));
                if(y>=52 && y<=58) left=63;
                for(int x=left;x<left+6;x++) water.Add(P(x,y));
            }
            Oval(water,66,78,5,4); Oval(water,65,94,5.5f,4);
            for(int y=85;y<=94;y++) for(int x=64;x<=65;x++) water.Add(P(x,y));
            Paint(maps["MeadowWater"],water,Load(Terrain+"/Rules/Paint_MeadowWater.asset"));
            var deep=new HashSet<Vector3Int>(water.Where(p=>Directions.All(d=>water.Contains(p+d)&&water.Contains(p+d*2)) && p.y<83 && (p.y<52 || p.y>58)));
            Paint(maps["DeepWater"],deep,Load(Terrain+"/Rules/Paint_DeepWater.asset"));
            Paint(maps["GrassLedge"],terrace,Load(Terrain+"/Rules/Paint_GrassLedge.asset"));

            var spring=new HashSet<Vector3Int>();
            for(int y=88;y<100;y++) for(int x=44;x<=75;x++)
                if(y>=90 || (x>=49 && x<=72)) spring.Add(P(x,y));
            Paint(maps["StoneLedge"],spring,Load(Terrain+"/Rules/Paint_StoneLedge.asset"));
            var faces=new HashSet<Vector3Int>();
            foreach(var p in terrace.Where(p=>!terrace.Contains(p+Vector3Int.down))) {
                maps["CliffFaces"].SetTile(p+Vector3Int.down,Load(Terrain+"/Tiles/EarthCliff_Foot_"+(p.x==34?"Left":p.x==57?"Right":"Center")+".asset"));
                faces.Add(p+Vector3Int.down);
            }
            foreach(var p in spring.Where(p=>!spring.Contains(p+Vector3Int.down)))
                for(int j=1;j<=3;j++) {
                    var q=p+Vector3Int.down*j; faces.Add(q);
                    maps["CliffFaces"].SetTile(q,Load(Terrain+"/Tiles/StoneCliff_"+(j==3?"Foot":"Middle")+"_"+(p.x==44?"Left":p.x==75?"Right":"Center")+".asset"));
                }
            var ramps=new HashSet<Vector3Int>();
            foreach(var start in new[]{P(39,40),P(54,53)})
                for(int y=0;y<3;y++) for(int x=0;x<4;x++) {
                    var p=start+P(x,y); ramps.Add(p); clear.Add(p);
                    maps["Stairs and Ramps"].SetTile(p,Load(Terrain+"/Tiles/EarthRamp_"+(y==2?"Top":y==0?"Foot":"Middle")+"_"+(x==0?"Left":x==3?"Right":"Center")+".asset"));
                }
            for(int x=64;x<=65;x++) {
                maps["WaterDetails"].SetTile(P(x,88),Load(Terrain+"/Tiles/FallLip_00.asset"));
                for(int y=85;y<88;y++) maps["WaterDetails"].SetTile(P(x,y),Load(Terrain+"/Animations/Animated_FallBody.asset"));
                maps["WaterDetails"].SetTile(P(x,84),Load(Terrain+"/Animations/Animated_FallFoam.asset"));
            }
            for(int y=7;y<83;y+=11) {
                var row=water.Where(p=>p.y==y).OrderBy(p=>p.x).ToArray();
                maps["WaterDetails"].SetTile(row[row.Length/2],Load(Terrain+"/Animations/Animated_Ripple.asset"));
            }

            var forest=new HashSet<Vector3Int>();
            for(int y=0;y<100;y++) for(int x=0;x<100;x++) {
                var p=P(x,y); if(!clear.Contains(p)&&!water.Contains(p)&&!spring.Contains(p)&&!faces.Contains(p)) forest.Add(p);
            }
            var boundary=new GameObject("Boundary Woodland"); boundary.transform.SetParent(world.transform,false); Undo.RegisterCreatedObjectUndo(boundary,"Boundary woodland");
            // Existing oak art, staggered/overlapped on row maps; no new textures or thousands of prefab objects.
            var oak=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Tutorial/Oak.prefab");
            var oakMaps=oak.GetComponentsInChildren<Tilemap>().Where(m=>m.name!="Collision").ToArray();
            int trees=0;
            for(int y=0;y<98;y+=2) {
                var row=Map(boundary.transform,"Oak row "+y,100+(100-y)*2,"Player");
                for(int x=(y/2)%2;x<98;x+=2) {
                    bool fits=true;
                    for(int j=0;j<4;j++)for(int i=0;i<3;i++) if(!forest.Contains(P(x+i,y+j))) fits=false;
                    if(!fits) continue;
                    foreach(var src in oakMaps) foreach(var p in src.cellBounds.allPositionsWithin) if(src.HasTile(p)) row.SetTile(P(x,y)+p,src.GetTile(p));
                    trees++;
                }
            }
            // Dense undergrowth makes the collision edge visible, including spaces between trunks.
            var hedge=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Tutorial/Hedgerow.prefab");
            var edge=Map(boundary.transform,"Forest edge undergrowth",70,"Player");
            for(int y=0;y<99;y++)for(int x=0;x<98;x++) {
                if(!forest.Contains(P(x,y)) || !Directions.Any(d=>clear.Contains(P(x,y)+d))) continue;
                bool fits=true;
                for(int j=0;j<2;j++)for(int i=0;i<3;i++)if(!forest.Contains(P(x+i,y+j)))fits=false;
                if(fits) foreach(var src in hedge.GetComponentsInChildren<Tilemap>().Where(m=>m.name!="Collision"))
                    foreach(var p in src.cellBounds.allPositionsWithin)if(src.HasTile(p))edge.SetTile(P(x,y)+p,src.GetTile(p));
            }

            var blocked=new HashSet<Vector3Int>(forest); blocked.UnionWith(water);blocked.UnionWith(spring);blocked.UnionWith(faces);
            foreach(var p in terrace) if(Directions.Any(d=>!terrace.Contains(p+d))) blocked.Add(p);
            blocked.ExceptWith(ramps);
            // Reserve the bridge with the existing real deck, so the blockout is traversable now.
            var bridgePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Tutorial/Stone_Bridge.prefab");
            var bridge=(GameObject)PrefabUtility.InstantiatePrefab(bridgePrefab,world.transform.Find("Environment"));
            bridge.name="River Crossing - Stone Bridge";bridge.transform.localPosition=new Vector3(61,54,0);Undo.RegisterCreatedObjectUndo(bridge,"Bridge crossing");
            for(int y=55;y<=56;y++)for(int x=61;x<=70;x++)blocked.Remove(P(x,y));
            for(int n=0;n<100;n++) { blocked.Add(P(n,-1));blocked.Add(P(n,100));blocked.Add(P(-1,n));blocked.Add(P(100,n)); }
            // Exit is reserved at the north edge; capped until an actual scene transition is authored.
            Paint(maps["Collision"],blocked,Load(TutorialGroundKitBuilder.CollisionPath));
            foreach(var m in maps.Values)m.RefreshAllTiles();
            maps["Collision"].GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            var anchors=new GameObject("Layout Anchors (editor only)");anchors.tag="EditorOnly";anchors.transform.SetParent(world.transform,false);Undo.RegisterCreatedObjectUndo(anchors,"Layout anchors");
            string[] names={"01 Home and Player Start","02 Hatchet Stump","03 Throw and Retrieve","04 Melee and Dodge","05 Bridge Landing","06 Enemy and Shard","07 Bonfire","08 Overworld Exit","Optional Well","Optional River Overlook"};
            Vector2[] points={new Vector2(15,13),new Vector2(39,28),new Vector2(43,48),new Vector2(49,58),new Vector2(73,56),new Vector2(86,64),new Vector2(87,82),new Vector2(88,97),new Vector2(15,40),new Vector2(60,69)};
            for(int i=0;i<names.Length;i++) { var g=new GameObject(names[i]);g.transform.SetParent(anchors.transform,false);g.transform.localPosition=points[i];g.tag="EditorOnly"; }
            world.name="Tutorial Zone";
            RepairCliffEnds();
            RefineWoodland();
            var cam=Camera.main;Undo.RecordObject(cam.transform,"Terrain preview camera");Undo.RecordObject(cam,"Terrain preview camera");
            cam.transform.position=new Vector3(50,50,-10);cam.orthographic=true;cam.orthographicSize=52;cam.backgroundColor=new Color32(56,75,60,255);
            if(SceneView.lastActiveSceneView) { SceneView.lastActiveSceneView.in2DMode=true;SceneView.lastActiveSceneView.LookAt(new Vector3(50,50,0),Quaternion.identity,55); }
            Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "Terrain authored. Forest oak stamps: "+trees+"; water cells: "+water.Count+"; terrace cells: "+terrace.Count+". "+Verify();
        }

        public static string RepairCliffEnds()
        {
            var world=GameObject.Find("Tutorial Zone");
            var faces=world.transform.Find("Terrain/CliffFaces").GetComponent<Tilemap>();
            Undo.RegisterCompleteObjectUndo(faces,"Join cliff ends");
            var ledge=world.transform.Find("Terrain/GrassLedge").GetComponent<Tilemap>();
            var ramps=world.transform.Find("Terrain/Stairs and Ramps").GetComponent<Tilemap>();
            var collision=world.transform.Find("Collision").GetComponent<Tilemap>();
            Undo.RegisterCompleteObjectUndo(collision,"Join cliff returns beside ramps");
            foreach(var p in ramps.cellBounds.allPositionsWithin) {
                var ramp=ramps.GetTile(p);if(!ramp || !ramp.name.Contains("_Top_"))continue;
                foreach(var d in new[]{Vector3Int.left,Vector3Int.right}) {
                    var q=p+d+Vector3Int.down;
                    if(ledge.HasTile(p+d)&&!ledge.HasTile(q)&&!ramps.HasTile(q)&&!faces.HasTile(q)) {
                        faces.SetTile(q,Load(Terrain+"/Tiles/EarthCliff_Foot_Center.asset"));
                        collision.SetTile(q,Load(TutorialGroundKitBuilder.CollisionPath));
                    }
                }
            }
            int changed=0;
            foreach(var p in faces.cellBounds.allPositionsWithin) {
                var tile=faces.GetTile(p);if(!tile)continue;
                var parts=tile.name.Split('_');if(parts.Length!=3 || !parts[0].EndsWith("Cliff"))continue;
                Func<Vector3Int,bool> same=q=>faces.GetTile(q)!=null && faces.GetTile(q).name.StartsWith(parts[0]+"_");
                // End caps belong only on exposed ends at this height, including stepped runs.
                string end=!same(p+Vector3Int.left)?"Left":!same(p+Vector3Int.right)?"Right":"Center";
                var replacement=Load(Terrain+"/Tiles/"+parts[0]+"_"+parts[1]+"_"+end+".asset");
                if(tile!=replacement){faces.SetTile(p,replacement);changed++;}
            }
            faces.RefreshAllTiles();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            collision.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            return "Repaired "+changed+" exposed cliff end pieces.";
        }

        public static string RefineWoodland()
        {
            var world=GameObject.Find("Tutorial Zone");var group=world.transform.Find("Boundary Woodland");
            var maps=world.GetComponentsInChildren<Tilemap>().Where(m=>m.transform.parent!=group).GroupBy(m=>m.name).ToDictionary(g=>g.Key,g=>g.First());
            maps["Collision"]=world.transform.Find("Collision").GetComponent<Tilemap>();
            var forest=new HashSet<Vector3Int>();
            for(int y=0;y<100;y++)for(int x=0;x<100;x++) {
                var p=P(x,y);
                bool springTrees=maps["StoneLedge"].HasTile(p)&&y>=92&&!Directions.Any(d=>maps["MeadowWater"].HasTile(p+d)||maps["MeadowWater"].HasTile(p+d*2));
                if(maps["Collision"].HasTile(p)&&!new[]{"MeadowWater","GrassLedge","CliffFaces"}.Any(n=>maps[n].HasTile(p))&&(!maps["StoneLedge"].HasTile(p)||springTrees))forest.Add(p);
            }
            foreach(Transform child in group.Cast<Transform>().ToArray())if(child.name.StartsWith("Oak row"))Undo.DestroyObjectImmediate(child.gameObject);
            var random=new System.Random(240924);int count=0;
            var sources=new[]{"Oak","Birch"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Tutorial/"+n+".prefab").GetComponentsInChildren<Tilemap>().Where(m=>m.name!="Collision").ToArray()).ToArray();
            for(int y=0;y<98;y+=2) {
                var rows=new[]{Map(group,"Oak row "+y+" A",100+(100-y)*4,"Player"),Map(group,"Oak row "+y+" B",101+(100-y)*4,"Player")};
                for(int x=(y/2)%2;x<98;x+=2) {
                    int yy=y+random.Next(0,2);var srcs=sources[random.Next(8)==0?1:0];bool fits=true;
                    foreach(var src in srcs)foreach(var p in src.cellBounds.allPositionsWithin)if(src.HasTile(p)&&!forest.Contains(P(x,yy)+p))fits=false;
                    if(!fits)continue;
                    var row=rows[(x/2)%2];
                    foreach(var src in srcs)foreach(var p in src.cellBounds.allPositionsWithin)if(src.HasTile(p))row.SetTile(P(x,yy)+p,src.GetTile(p));
                    count++;
                }
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());return "Varied woodland stamps: "+count;
        }

        public static string Verify()
        {
            var world=GameObject.Find("Tutorial Zone");var collision=world.transform.Find("Collision").GetComponent<Tilemap>();
            var anchors=world.transform.Find("Layout Anchors (editor only)");
            var seen=new HashSet<Vector3Int>();var queue=new Queue<Vector3Int>();var start=P(15,13);seen.Add(start);queue.Enqueue(start);
            Physics2D.SyncTransforms();
            Func<Vector3Int,bool> blocked=p=>collision.HasTile(p)||Physics2D.OverlapBox(new Vector2(p.x+.5f,p.y+.5f),new Vector2(.8f,.8f),0)!=null;
            while(queue.Count>0) {
                var p=queue.Dequeue();foreach(var d in Directions) {var q=p+d;if(q.x<0||q.y<0||q.x>=100||q.y>=100||seen.Contains(q)||blocked(q))continue;seen.Add(q);queue.Enqueue(q);}
            }
            var missing=new List<string>();foreach(Transform a in anchors)if(!seen.Contains(Vector3Int.FloorToInt(a.position)))missing.Add(a.name);
            if(missing.Count>0)throw new Exception("Unreachable anchors: "+string.Join(", ",missing));
            return "PASS: all "+anchors.childCount+" route/optional anchors connected with 0.8-unit collider clearance; reachable cells "+seen.Count+".";
        }
    }
}
