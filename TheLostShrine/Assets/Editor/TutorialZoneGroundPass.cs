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
    // One-shot layout authoring; normal subsequent edits use the Tile Palette.
    public static class TutorialZoneGroundPass
    {
        static Vector3Int P(int x,int y) => new Vector3Int(x,y,0);
        static Vector2 V(float x,float y) => new Vector2(x,y);
        static readonly Vector3Int[] Steps={Vector3Int.up,Vector3Int.right,Vector3Int.down,Vector3Int.left};
        static void Oval(HashSet<Vector3Int> cells,float x,float y,float rx,float ry)
        {
            for(int yy=Mathf.FloorToInt(y-ry);yy<=Mathf.CeilToInt(y+ry);yy++)
                for(int xx=Mathf.FloorToInt(x-rx);xx<=Mathf.CeilToInt(x+rx);xx++)
                    if(Mathf.Pow((xx+.5f-x)/rx,2)+Mathf.Pow((yy+.5f-y)/ry,2)<=1)cells.Add(P(xx,yy));
        }
        static void Trail(HashSet<Vector3Int> cells,float width,params Vector2[] points)
        {
            for(int i=1;i<points.Length;i++){
                int n=Mathf.CeilToInt(Vector2.Distance(points[i-1],points[i])*4);
                for(int k=0;k<=n;k++){var p=Vector2.Lerp(points[i-1],points[i],k/(float)n);Oval(cells,p.x,p.y,width/2,width/2);}
            }
        }
        static TileBase Brush(string family,string name)
        {
            var tile=AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Art/Tiles/Tutorial/"+family+"/Rules/Paint_"+name+".asset");
            if(!tile)throw new Exception("Missing brush "+name);return tile;
        }
        public static string Build()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.path!="Assets/Scenes/Tutorial.unity")throw new Exception("Open Tutorial in Edit Mode.");
            var world=GameObject.Find("Tutorial Zone");var ground=world.transform.Find("Ground");var paths=world.transform.Find("Paths");
            var maps=ground.GetComponentsInChildren<Tilemap>().Concat(paths.GetComponentsInChildren<Tilemap>()).ToDictionary(m=>m.name);
            if(maps.Any(k=>k.Key!="Grass"&&k.Value.GetUsedTilesCount()>0))throw new Exception("Ground already painted; edit existing maps directly.");
            var terrain=world.transform.Find("Terrain");var collision=world.transform.Find("Collision").GetComponent<Tilemap>();
            var water=terrain.Find("MeadowWater").GetComponent<Tilemap>();var ledge=terrain.Find("GrassLedge").GetComponent<Tilemap>();
            var faces=terrain.Find("CliffFaces").GetComponent<Tilemap>();var ramps=terrain.Find("Stairs and Ramps").GetComponent<Tilemap>();
            var bridge=world.transform.Find("Environment/River Crossing - Stone Bridge");
            var bridgeArt=bridge.GetComponentsInChildren<Tilemap>().Where(m=>m.name!="Collision").ToArray();
            Func<Vector3Int,bool> deck=p=>bridgeArt.Any(m=>m.HasTile(m.WorldToCell(new Vector3(p.x+.5f,p.y+.5f,0))));
            Func<Vector3Int,bool> allowed=p=>maps["Grass"].HasTile(p)&&!collision.HasTile(p)&&!water.HasTile(p)&&!ramps.HasTile(p)&&!faces.HasTile(p)&&!deck(p);
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Tutorial paths and ground surfaces");
            foreach(var m in maps.Values)if(m.name!="Grass")Undo.RegisterCompleteObjectUndo(m,"Paint tutorial ground");
            var raised=new GameObject("Raised Surfaces");raised.transform.SetParent(ground,false);Undo.RegisterCreatedObjectUndo(raised,"Raised ground surfaces");
            var raisedMaps=new Dictionary<string,Tilemap>();
            foreach(var item in new[]{new{name="DryGrass",order=14},new{name="Earth",order=15},new{name="WornStone",order=16}}){
                var go=new GameObject(item.name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(raised.transform,false);
                var r=go.GetComponent<TilemapRenderer>();r.sortingLayerName="Ground";r.sortingOrder=item.order;raisedMaps[item.name]=go.GetComponent<Tilemap>();
            }
            var sets=new Dictionary<string,HashSet<Vector3Int>>();
            foreach(var name in new[]{"DryGrass","Earth","DampEarth","WornStone","Cobbles","TilledSoil","DirtLane","Footpath","Paving"})sets[name]=new HashSet<Vector3Int>();
            // Three-cell main trail connects the actual lesson locations and both ramp openings.
            Trail(sets["DirtLane"],3,V(15,13),V(23,13),V(28,17),V(33,20),V(36,24),V(39,28),V(40.5f,34),V(40.5f,43.5f),
                V(43,47),V(43,50),V(47,54),V(49,58),V(53,58),V(55.5f,57),V(55.5f,51.5f),V(59.5f,51.5f),V(61,55.5f),
                V(73,55.5f),V(77,57),V(82,61),V(86,64),V(85,70),V(82.5f,74),V(84,79),V(87,82),V(88,86),V(90,90),V(88,96),V(88,99.5f));
            // Quiet side trails: well, garden, house threshold and river overlook.
            Trail(sets["Footpath"],1.8f,V(16,13),V(16,24),V(17,28),V(14,34),V(13,38),V(15,40));
            Trail(sets["Footpath"],1.6f,V(61,55.5f),V(61,64),V(60,69));
            Trail(sets["Footpath"],1.6f,V(15,13),V(13.5f,15.5f),V(10.5f,15.5f));
            Trail(sets["Footpath"],2,V(23,13),V(23,18));
            // Broad, irregular patches leave most meadow visible.
            foreach(var a in new[]{new Vector4(9,18,3,2),new Vector4(25,11,4,2),new Vector4(43,24,3,2),new Vector4(19,35,3,2),
                new Vector4(37.5f,49,2.5f,3),new Vector4(49,61,3,1.5f),new Vector4(90,61,3,2),new Vector4(82,67,3,2),new Vector4(91,83,3,2)})
                Oval(sets["DryGrass"],a.x,a.y,a.z,a.w);
            foreach(var a in new[]{new Vector4(21,14,6,3.2f),new Vector4(10.5f,13,3.5f,3.5f),new Vector4(39,28,5,4),
                new Vector4(43,48,5,4),new Vector4(49,58,4.5f,3),new Vector4(86,64,3.5f,2.8f),new Vector4(87,82,3.5f,3)})
                Oval(sets["Earth"],a.x,a.y,a.z,a.w);
            Oval(sets["Earth"],36.5f,27,3,2);Oval(sets["Earth"],46,49,2,2);Oval(sets["Earth"],46.5f,57,3,2);
            foreach(var a in new[]{new Vector4(59,68,2.5f,2),new Vector4(61,61,1.5f,2),new Vector4(72,53,2,1.5f),new Vector4(17,40,2,1.5f)})
                Oval(sets["DampEarth"],a.x,a.y,a.z,a.w);
            foreach(var a in new[]{new Vector4(60.5f,55.5f,2,1.5f),new Vector4(72,55.5f,2.5f,1.8f),new Vector4(60,69,2,1.5f),
                new Vector4(87,82,2,2),new Vector4(88,97,1.6f,2),new Vector4(49,58,1.7f,1.3f)})
                Oval(sets["WornStone"],a.x,a.y,a.z,a.w);
            Oval(sets["Cobbles"],15,40,3.5f,2.7f);
            Oval(sets["Cobbles"],22.5f,16.5f,2.5f,1.8f);
            Trail(sets["Paving"],2,V(23,16),V(23,18));
            for(int y=11;y<15;y++)foreach(int x in new[]{8,9,11,12})sets["TilledSoil"].Add(P(x,y));
            // Dirt reaches under stone thresholds, but does not stripe over the plazas.
            sets["DirtLane"].ExceptWith(sets["Cobbles"]);sets["Footpath"].ExceptWith(sets["Cobbles"]);
            var counts=new List<string>();
            foreach(var pair in sets){
                string name=pair.Key;var tile=Brush(name=="DirtLane"||name=="Footpath"?"Paths":"Ground",name=="Paving"?"Cobbles":name);
                var cells=pair.Value.Where(allowed).ToArray();
                var low=cells.Where(p=>!ledge.HasTile(p)||!raisedMaps.ContainsKey(name)).ToArray();
                maps[name].SetTiles(low,Enumerable.Repeat(tile,low.Length).ToArray());
                if(raisedMaps.ContainsKey(name)){
                    var high=cells.Where(p=>ledge.HasTile(p)).ToArray();raisedMaps[name].SetTiles(high,Enumerable.Repeat(tile,high.Length).ToArray());
                }
                counts.Add(name+"="+cells.Length);
            }
            foreach(var m in ground.GetComponentsInChildren<Tilemap>().Concat(paths.GetComponentsInChildren<Tilemap>()))m.RefreshAllTiles();
            FinishMaterialJoins();
            Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);
            string report=VerifyPaintedRoute();EditorSceneManager.SaveScene(scene);
            return string.Join(", ",counts)+". "+report;
        }
        public static string FinishMaterialJoins()
        {
            var world=GameObject.Find("Tutorial Zone");var ground=world.transform.Find("Ground");var paths=world.transform.Find("Paths");
            var ramps=world.transform.Find("Terrain/Stairs and Ramps").GetComponent<Tilemap>();
            var cobbles=ground.Find("Cobbles").GetComponent<Tilemap>();
            var bridge=world.transform.Find("Environment/River Crossing - Stone Bridge").GetComponentsInChildren<Tilemap>().Where(m=>m.name!="Collision").ToArray();
            // Solid surfaces draw over the dirt underneath, so alpha edges join
            // without grass slivers or a dirt stripe bisecting the stone pads.
            foreach(var m in new[]{cobbles,ground.Find("WornStone").GetComponent<Tilemap>(),ground.Find("Raised Surfaces/WornStone").GetComponent<Tilemap>(),ramps}){
                var r=m.GetComponent<TilemapRenderer>();Undo.RecordObject(r,"Surface overlap order");r.sortingOrder=m==ramps?25:m==cobbles?23:22;
            }
            int count=0;
            foreach(string name in new[]{"DirtLane","Footpath"}){
                var map=paths.Find(name).GetComponent<Tilemap>();Undo.RegisterCompleteObjectUndo(map,"Join surface thresholds");
                var add=new HashSet<Vector3Int>();
                foreach(var p in map.cellBounds.allPositionsWithin)if(map.HasTile(p))foreach(var d in Steps){var q=p+d;
                    if(!map.HasTile(q)&&(ramps.HasTile(q)||cobbles.HasTile(q)||bridge.Any(m=>m.HasTile(m.WorldToCell(new Vector3(q.x+.5f,q.y+.5f,0))))))add.Add(q);
                }
                var cells=add.ToArray();map.SetTiles(cells,Enumerable.Repeat(Brush("Paths",name),cells.Length).ToArray());map.RefreshAllTiles();count+=cells.Length;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());return "Joined "+count+" threshold cells.";
        }

        public static string VerifyPaintedRoute()
        {
            var world=GameObject.Find("Tutorial Zone");var collision=world.transform.Find("Collision").GetComponent<Tilemap>();
            var maps=world.transform.Find("Ground").GetComponentsInChildren<Tilemap>().Where(m=>m.name!="Grass"&&m.name!="DryGrass"&&m.name!="TilledSoil")
                .Concat(world.transform.Find("Paths").GetComponentsInChildren<Tilemap>()).ToArray();
            var ramps=world.transform.Find("Terrain/Stairs and Ramps").GetComponent<Tilemap>();
            var bridge=world.transform.Find("Environment/River Crossing - Stone Bridge").GetComponentsInChildren<Tilemap>().Where(m=>m.name!="Collision").ToArray();
            Func<Vector3Int,bool> painted=p=>ramps.HasTile(p)||maps.Any(m=>m.HasTile(p))||bridge.Any(m=>m.HasTile(m.WorldToCell(new Vector3(p.x+.5f,p.y+.5f,0))));
            var queue=new Queue<Vector3Int>();var seen=new HashSet<Vector3Int>();var start=P(15,13);queue.Enqueue(start);seen.Add(start);
            while(queue.Count>0){var p=queue.Dequeue();foreach(var d in Steps){var q=p+d;if(seen.Contains(q)||q.x<0||q.y<0||q.x>=100||q.y>=100||collision.HasTile(q)||!painted(q))continue;seen.Add(q);queue.Enqueue(q);}}
            foreach(Transform anchor in world.transform.Find("Layout Anchors (editor only)"))if(!seen.Contains(Vector3Int.FloorToInt(anchor.position)))throw new Exception("Painted route disconnected: "+anchor.name);
            return "PASS: painted surfaces connect all ten anchors. "+TutorialZoneTerrainPass.Verify();
        }
    }
}
