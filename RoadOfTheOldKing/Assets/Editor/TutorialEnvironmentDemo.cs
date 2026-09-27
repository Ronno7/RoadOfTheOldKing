using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TheLostShrine.EditorTools
{
    // Dress the single art presentation with reusable prefab instances. No gameplay scripts.
    internal static class TutorialEnvironmentDemo
    {
        internal static void Dress(GameObject world)
        {
            var maps=world.GetComponentsInChildren<Tilemap>().ToDictionary(m=>m.name);
            var collision=maps["Collision"];
            var structures=new GameObject("Environment Objects");
            structures.transform.SetParent(world.transform,false);
            Func<string,float,float,GameObject> place=(name,x,y)=>{
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(TutorialEnvironmentKitBuilder.PrefabRoot+"/"+name+".prefab");
                if(!prefab)throw new InvalidOperationException("Build Environment first: "+name);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,structures.transform);
                go.name=name;go.transform.localPosition=new Vector3(x,y,0);
                // Independent maps keep transparent cells in one object from erasing a neighbour.
                // Nearby foreground crowns draw over more distant crowns.
                foreach(var renderer in go.GetComponentsInChildren<TilemapRenderer>())
                    if(renderer.sortingLayerName=="Player")renderer.sortingOrder=100+Mathf.RoundToInt((40-y)*4);
                return go;
            };
            Action<string,string,int,int,int,int> paint=(map,tile,x,y,w,h)=>{
                var brush=AssetDatabase.LoadAssetAtPath<TileBase>(tile);
                if(!brush)throw new InvalidOperationException("Missing brush: "+tile);
                for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)maps[map].SetTile(new Vector3Int(xx,yy,0),brush);
            };
            string foot=TutorialPathsKitBuilder.Root+"/Rules/Paint_Footpath.asset";
            string lane=TutorialPathsKitBuilder.Root+"/Rules/Paint_DirtLane.asset";
            string earth=TutorialGroundKitBuilder.Root+"/Rules/Paint_Earth.asset";

            // Two-cell approaches follow the offset workshop and longhouse entrances.
            foreach(var p in maps["Footpath"].cellBounds.allPositionsWithin)
                if(p.y>=27)maps["Footpath"].SetTile(p,null);
            place("Cottage_Clay",5.375f,29);place("Cottage_Thatch",12.25f,32);
            paint("Footpath",foot,6,27,2,3);
            paint("Footpath",foot,7,27,7,2);
            paint("Footpath",foot,12,27,3,3);
            paint("Footpath",foot,13,29,4,2);
            paint("Footpath",foot,16,29,2,4);
            // A third home opens onto the square, next to the shared well.
            place("Cottage_Clay",2.375f,18);
            paint("Footpath",foot,3,16,2,3);
            paint("Footpath",foot,4,17,7,1);
            place("Village_Well",9,19);paint("Footpath",foot,9,18,4,1);
            place("Woodshed",0,10);
            paint("Footpath",foot,0,7,2,4);paint("Footpath",foot,1,7,4,1);
            place("Firewood_Rack",0,14);place("Hatchet_Stump",2,5);place("Fallen_Log",0,2);

            // Restore the stream under the crossing, then open the two-cell bridge deck.
            var water=AssetDatabase.LoadAssetAtPath<TileBase>(TutorialTerrainKitBuilder.Root+"/Rules/Paint_MeadowWater.asset");
            var block=AssetDatabase.LoadAssetAtPath<TileBase>(TutorialGroundKitBuilder.CollisionPath);
            for(int y=20;y<=21;y++)
            {
                int start=35+Mathf.RoundToInt(Mathf.Sin(y*.18f)*3);
                for(int x=start;x<start+6;x++)maps["MeadowWater"].SetTile(new Vector3Int(x,y,0),water);
            }
            for(int x=33;x<43;x++)for(int y=20;y<=21;y++)collision.SetTile(new Vector3Int(x,y,0),null);
            for(int x=33;x<43;x++)for(int y=19;y<=22;y++)maps["DirtLane"].SetTile(new Vector3Int(x,y,0),null);
            place("Stone_Bridge",33,19);

            // Practice enclosure: native-pixel offsets align the independently placed posts.
            paint("Earth",earth,47,9,13,8);
            paint("Footpath",foot,52,9,1,4);paint("Footpath",foot,52,6,1,3);
            paint("DirtLane",lane,52,17,1,5);
            foreach(float y in new[]{8f,16.5f})
            {
                foreach(float x in new[]{46.25f,48.625f,53.125f,55.5f,57.875f})place("Fence_Horizontal",x,y);
                place("Gate_Open",51,y);
            }
            foreach(float x in new[]{46f,60f})
                for(int i=0;i<4;i++)place("Fence_Vertical",x,8.1875f+i*2.125f);
            foreach(var item in new[]{new {name="Target_Worn",x=49f,y=13f},new {name="Target_New",x=54f,y=13f},new {name="Practice_Dummy",x=58f,y=13f}})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(TutorialEnvironmentKitBuilder.VisualRoot+"/"+item.name+"_Visual.prefab");
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,world.transform.Find("Interactive Objects"));
                visual.transform.localPosition=new Vector3(item.x,item.y,0);
            }
            place("Hatchet_Stump",47,5);place("Firewood_Rack",55,5);
            place("Fence_Repaired",57,3);place("Fence_Post",60,3);
            // A modest ruin above the open forecourt; its arch remains a real passage.
            place("Old_Arch",52,32);place("Broken_Pillar",48,32);place("Ruined_Wall",56,34);
            paint("Footpath",foot,53,29,1,4);

            foreach(var p in new[]{new Vector2Int(0,31),new Vector2Int(9,36),new Vector2Int(28,31),new Vector2Int(28,36),new Vector2Int(0,26),new Vector2Int(60,31),new Vector2Int(59,0),new Vector2Int(56,2),new Vector2Int(28,0),new Vector2Int(19,0)})
                place("Oak",p.x,p.y);
            foreach(var p in new[]{new Vector2Int(26,25),new Vector2Int(44,31),new Vector2Int(61,23),new Vector2Int(43,2)})
                place("Birch",p.x,p.y);
            foreach(var p in new[]{new Vector2Int(6,0),new Vector2Int(15,0),new Vector2Int(23,7),new Vector2Int(22,12)})
                place("Apple_Tree",p.x,p.y);
            place("Hedgerow",0,36);place("Hedgerow",25,37);place("Hedgerow",47,1);
            place("Boulder_Cluster",29,26);place("Boulder",44,25);place("Boulder",24,3);
            place("Small_Stones",22,2);place("Small_Stones",56,25);place("Fallen_Log",44,2);
        }
    }
}
