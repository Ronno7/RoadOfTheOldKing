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
    // One deliberately authored presentation scene, independent of kit rebuilds.
    public static class DemoTutorialBuilder
    {
        public const string ScenePath = "Assets/Scenes/DemoTutorial.unity";

        [MenuItem("Tools/The Lost Shrine/Rebuild Demo Tutorial Presentation")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
            var previous = SceneManager.GetActiveScene();
            var loaded = SceneManager.GetSceneByPath(ScenePath);
            if (loaded.IsValid() && loaded.isDirty) throw new InvalidOperationException("Save your DemoTutorial edits before explicitly rebuilding it.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            if (loaded.IsValid()) EditorSceneManager.CloseScene(loaded, true);
            try
            {
                SceneManager.SetActiveScene(scene);
                var world = TutorialTerrainKitBuilder.CreateTemplate();
                world.name = "Tutorial World";
                Paint(world);
                var camera = new GameObject("Presentation Camera", typeof(Camera)).GetComponent<Camera>();
                camera.transform.position = new Vector3(32,20,-10);
                camera.orthographic = true; camera.orthographicSize = 21.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(56,75,60,255); camera.tag = "MainCamera";
                new GameObject("World Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
                EditorSceneManager.SaveScene(scene, ScenePath);
                Selection.activeGameObject = world;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.in2DMode = true;
                    SceneView.lastActiveSceneView.LookAt(new Vector3(32,20,0), Quaternion.identity, 24);
                }
            }
            catch
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene,true);
                throw;
            }
        }

        private static void Paint(GameObject world)
        {
            var maps = world.GetComponentsInChildren<Tilemap>().ToDictionary(m=>m.name);
            var ground = new Dictionary<string,RuleTile>(); var terrain = new Dictionary<string,RuleTile>();
            foreach (var name in new[]{"Grass","DryGrass","Earth","DampEarth","WornStone","Cobbles","TilledSoil"})
                ground[name] = AssetDatabase.LoadAssetAtPath<RuleTile>(TutorialGroundKitBuilder.Root+"/Rules/Paint_"+name+".asset");
            foreach (var name in new[]{"MeadowWater","StoneWater","DeepWater","GrassLedge","StoneLedge","StoneWall"})
                terrain[name] = AssetDatabase.LoadAssetAtPath<RuleTile>(TutorialTerrainKitBuilder.Root+"/Rules/Paint_"+name+".asset");
            var lane=AssetDatabase.LoadAssetAtPath<RuleTile>(TutorialPathsKitBuilder.Root+"/Rules/Paint_DirtLane.asset");
            var footpath=AssetDatabase.LoadAssetAtPath<RuleTile>(TutorialPathsKitBuilder.Root+"/Rules/Paint_Footpath.asset");
            if(lane==null||footpath==null)throw new InvalidOperationException("Build the Paths kit before the demo.");
            Func<string,TileBase> piece = name=>AssetDatabase.LoadAssetAtPath<TileBase>(TutorialTerrainKitBuilder.Root+"/Tiles/"+name+".asset");
            Func<string,TileBase> animated = name=>AssetDatabase.LoadAssetAtPath<TileBase>(TutorialTerrainKitBuilder.Root+"/Animations/Animated_"+name+".asset");
            Action<string,TileBase,int,int,int,int> rect = (map,tile,x,y,w,h)=>{
                for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)maps[map].SetTile(new Vector3Int(xx,yy,0),tile);
            };
            Action<string,TileBase,int,int,int,int> oval = (map,tile,x,y,rx,ry)=>{
                for(int yy=y-ry;yy<=y+ry;yy++)for(int xx=x-rx;xx<=x+rx;xx++)
                    if(Mathf.Pow((xx-x)/(rx+.5f),2)+Mathf.Pow((yy-y)/(ry+.5f),2)<=1)maps[map].SetTile(new Vector3Int(xx,yy,0),tile);
            };
            rect("Grass",ground["Grass"],0,0,64,40);
            // Soft dry meadow patches leave quiet space around the constructed areas.
            foreach(var p in new[]{new Vector3Int(5,32,0),new Vector3Int(4,20,0),new Vector3Int(27,8,0),new Vector3Int(56,6,0),new Vector3Int(60,20,0),new Vector3Int(45,36,0)})
                oval("DryGrass",ground["DryGrass"],p.x,p.y,4,3);

            // Winding stream widens into an island pool below the dry crossing.
            for(int y=0;y<40;y++)
            {
                int x=35+Mathf.RoundToInt(Mathf.Sin(y*.18f)*3);
                rect("MeadowWater",terrain["MeadowWater"],x,y,6,1);
            }
            oval("MeadowWater",terrain["MeadowWater"],37,11,8,6);
            oval("MeadowWater",null,37,11,2,2);
            oval("DeepWater",terrain["DeepWater"],33,10,2,3);
            oval("DeepWater",terrain["DeepWater"],42,12,1,2);
            rect("MeadowWater",null,28,20,18,2);
            foreach(var p in maps["DeepWater"].cellBounds.allPositionsWithin)
                if(maps["DeepWater"].HasTile(p)&&(!maps["MeadowWater"].HasTile(p)||TileKitAssets.Neighbors.Any(n=>!maps["MeadowWater"].HasTile(p+n))))maps["DeepWater"].SetTile(p,null);
            // Damp ground hugs a quiet bank; shallow stone shelves break up the meadow.
            oval("DampEarth",ground["DampEarth"],28,12,3,3);
            oval("WornStone",ground["WornStone"],47,8,3,2);
            oval("StoneLedge",terrain["StoneLedge"],58,19,2,1);
            oval("StoneLedge",terrain["StoneLedge"],48,4,2,1);

            // Main high meadow: stepped silhouette, stone stairs and a spring feeding a fall.
            rect("GrassLedge",terrain["GrassLedge"],6,26,21,9);
            rect("GrassLedge",terrain["GrassLedge"],9,35,15,3);
            rect("GrassLedge",terrain["GrassLedge"],4,29,2,4);
            for(int x=6;x<=26;x++)for(int y=23;y<=25;y++)
                maps["CliffFaces"].SetTile(new Vector3Int(x,y,0),piece("EarthCliff_"+(y==23?"Foot":"Middle")+"_"+(x==6?"Left":x==26?"Right":"Center")));
            for(int x=12;x<=14;x++)for(int y=23;y<=26;y++)
                maps["Stairs and Ramps"].SetTile(new Vector3Int(x,y,0),piece("StoneStairs_"+(y==26?"Top":y==23?"Foot":"Middle")+"_"+(x==12?"Left":x==14?"Right":"Center")));
            oval("MeadowWater",terrain["MeadowWater"],23,31,3,3);
            rect("MeadowWater",terrain["MeadowWater"],23,26,2,6);
            oval("StoneWater",terrain["StoneWater"],24,21,4,2);
            for(int x=23;x<=24;x++)
            {
                maps["WaterDetails"].SetTile(new Vector3Int(x,26,0),piece("FallLip_00"));
                for(int y=23;y<=25;y++)maps["WaterDetails"].SetTile(new Vector3Int(x,y,0),animated("FallBody"));
                maps["WaterDetails"].SetTile(new Vector3Int(x,22,0),animated("FallFoam"));
            }

            // Opposite bank: a stone terrace above an open forecourt, accessed by a broad ramp.
            rect("StoneLedge",terrain["StoneLedge"],48,29,12,7);
            rect("StoneLedge",terrain["StoneLedge"],50,36,8,2);
            for(int x=48;x<60;x++)for(int y=27;y<=28;y++)
                maps["CliffFaces"].SetTile(new Vector3Int(x,y,0),piece("StoneCliff_"+(y==27?"Foot":"Middle")+"_"+(x==48?"Left":x==59?"Right":"Center")));
            for(int x=52;x<=54;x++)for(int y=27;y<=29;y++)
                maps["Stairs and Ramps"].SetTile(new Vector3Int(x,y,0),piece("EarthRamp_"+(y==29?"Top":y==27?"Foot":"Middle")+"_"+(x==52?"Left":x==54?"Right":"Center")));
            oval("Earth",ground["Earth"],52,22,7,4);
            oval("WornStone",ground["WornStone"],54,22,4,2);
            rect("DirtLane",lane,52,24,3,3);

            // Walled kitchen garden and a cobbled village square, with clear gate openings.
            rect("Earth",ground["Earth"],6,6,15,10);
            foreach(int x in new[]{8,12,16})rect("TilledSoil",ground["TilledSoil"],x,8,2,6);
            for(int x=5;x<=21;x++)foreach(int y in new[]{5,16})if(x<12||x>14)maps["StoneWall"].SetTile(new Vector3Int(x,y,0),terrain["StoneWall"]);
            for(int y=5;y<=16;y++)foreach(int x in new[]{5,21})maps["StoneWall"].SetTile(new Vector3Int(x,y,0),terrain["StoneWall"]);
            rect("Earth",ground["Earth"],8,17,12,6);
            rect("Paving",ground["Cobbles"],10,18,8,4);
            rect("Paving",ground["Cobbles"],12,16,3,3);
            rect("Paving",ground["Cobbles"],12,21,3,2);
            rect("DirtLane",lane,12,14,3,4);
            rect("DirtLane",lane,12,22,3,1);
            rect("DirtLane",lane,12,2,3,4);
            // Broad trail skirts the waterfall pool then crosses the river.
            rect("DirtLane",lane,18,17,15,2);
            rect("DirtLane",lane,31,18,3,4);
            rect("DirtLane",lane,33,20,16,2);
            // Smaller footpaths lead to the spring and a quiet riverbank viewpoint.
            rect("Footpath",footpath,13,27,1,5);
            rect("Footpath",footpath,14,31,6,1);
            rect("Footpath",footpath,26,15,1,3);
            rect("Footpath",footpath,25,15,1,1);
            rect("Footpath",footpath,25,6,1,9);
            rect("Footpath",footpath,26,6,3,1);
            // Quiet water movement, positioned away from banks and the dry causeway.
            foreach(var p in new[]{new Vector3Int(39,34,0),new Vector3Int(35,28,0),new Vector3Int(35,24,0),new Vector3Int(34,16,0),new Vector3Int(41,14,0),new Vector3Int(33,8,0),new Vector3Int(39,4,0),new Vector3Int(23,31,0),new Vector3Int(25,21,0)})
                maps["WaterDetails"].SetTile(p,animated("Ripple"));

            // Author collision separately from art; this scene contains no gameplay objects.
            var block=AssetDatabase.LoadAssetAtPath<Tile>(TutorialGroundKitBuilder.CollisionPath);
            foreach(string name in new[]{"MeadowWater","StoneWater","StoneWall","CliffFaces"})
                foreach(var p in maps[name].cellBounds.allPositionsWithin)if(maps[name].HasTile(p))maps["Collision"].SetTile(p,block);
            foreach(string name in new[]{"GrassLedge","StoneLedge"})
                foreach(var p in maps[name].cellBounds.allPositionsWithin)
                    if(maps[name].HasTile(p)&&new[]{Vector3Int.left,Vector3Int.right,Vector3Int.up,Vector3Int.down}.Any(n=>!maps[name].HasTile(p+n)))maps["Collision"].SetTile(p,block);
            foreach(var p in maps["Stairs and Ramps"].cellBounds.allPositionsWithin)if(maps["Stairs and Ramps"].HasTile(p))maps["Collision"].SetTile(p,null);
            TutorialEnvironmentDemo.Dress(world);
            TutorialDecorationKitBuilder.Dress(world);
        }
    }
}
