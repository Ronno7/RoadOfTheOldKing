using System;
using RoadOfTheOldKing.Cameras;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.Prototype;
using RoadOfTheOldKing.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RoadOfTheOldKing.EditorTools
{
    // Creates the first greybox once. Later edits belong to the authored proof scene.
    public static class LowlandsSluiceProofBuilder
    {
        public const string ScenePath = "Assets/Scenes/LowlandsSluiceProof.unity";
        static Sprite square;
        static readonly Color Stone = new Color(.34f, .38f, .34f);
        static readonly Color Bronze = new Color(.70f, .44f, .19f);
        static readonly Color Water = new Color(.20f, .46f, .51f);

        [MenuItem("Road of the Old King/World/Create Sluice Proof")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use stopped Edit Mode.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("Proof already exists; edit its scene instead of rebuilding it.");
            square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/WhiteSquare.png");
            if (square == null) throw new InvalidOperationException("WhiteSquare sprite missing.");
            var original = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                Build();
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save proof.");
            }
            finally
            {
                if (original.IsValid()) SceneManager.SetActiveScene(original);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void Build()
        {
            var session = Instance("Assets/Prefabs/Progression/GameSession.prefab").GetComponent<GameSession>();
            session.name = "Isolated Sluice Session";
            Set(session, "saveKey", LowlandsSluiceProof.SaveKey);
            Set(session, "legacySaveKeys", Array.Empty<Object>());
            Set(session, "newGameScene", ScenePath);
            Set(session, "titleScene", "");
            var player = Instance("Assets/Prefabs/Player/Player.prefab");
            player.transform.position = new Vector3(-3, 0);
            var camera = Instance("Assets/Prefabs/Cameras/FollowCamera.prefab");
            Set(camera.GetComponent<CameraFollow2D>(), "target", player.transform);
            camera.transform.position = new Vector3(-2, 0, -10);
            var light = new GameObject("Main Light").AddComponent<Light>();
            light.type = LightType.Directional;

            Box("Ground", -.75f, -9, 10.5f, 33, new Color(.25f,.32f,.24f), false, -100);
            Box("West boundary", -5.5f,-9, 1,33, Stone);
            Box("East boundary", 4,-9, 1,33, Stone);
            Box("North boundary", -.75f,6, 10.5f,1, Stone);
            Box("South boundary", -.75f,-24, 10.5f,1, Stone);
            Box("Service corridor east bank", .625f,-4, 6.25f,5, Stone);
            Box("Basin corridor east bank", .625f,-15.25f, 6.25f,3.5f, Stone);

            var mechanism = new GameObject("Recall Sluice");
            var sluice = mechanism.AddComponent<RecallSluice>();
            Set(sluice, "puzzleId", "prototype/sluice");
            // West-facing facade, a .625-unit aperture against a .36-unit axe / .75-unit player.
            WallWithSlot("Distributor facade", 0,-1.5f,5,0);
            Box("Distributor north",1.75f,5,3.5f,.5f,Stone);
            Box("Distributor south",1.75f,-1.5f,3.5f,.5f,Stone);
            Box("Distributor east",3.5f,1.75f,.5f,7,Stone);
            Target("Distributor fork",2,0,.4f,.9f,sluice,SluicePart.DistributorFork);
            Target("Distributor vane",0,2,.5f,.7f,sluice,SluicePart.DistributorVane);
            var bolt = Box("Distributor bolt",-.55f,2,.55f,.18f,Bronze,false,7);
            Set(sluice,"distributorBolt",bolt);
            Crank("Distributor crank",-2,2,sluice,false);
            var service = Box("Service water and collision",-3.5f,-3,2.5f,1.8f,Water,true,-20);
            Set(sluice,"serviceWater",service);

            WallWithSlot("Main facade",-2,-13.5f,-6.5f,-12);
            Box("Main north",.75f,-6.5f,5.5f,.5f,Stone);
            Box("Main south",.75f,-13.5f,5.5f,.5f,Stone);
            Box("Main east",3.5f,-10,.5f,7.5f,Stone);
            Box("Carriage rail",2,-10,.125f,4.5f,Bronze,false,2);
            var carriage = Target("Main fork carriage",2,-12,.4f,.9f,sluice,SluicePart.MainFork);
            Set(sluice,"carriage",carriage.transform);
            Set(sluice,"loadingPosition",new Vector3(2,-12));
            Set(sluice,"drawingPosition",new Vector3(2,-8));
            Target("Brake vane",.5f,-8,.45f,.75f,sluice,SluicePart.BrakeVane);
            Target("Catch vane",-1,-8,.45f,.75f,sluice,SluicePart.CatchVane);
            var brake = Box("Brake linkage",-.15f,-7.5f,1.8f,.15f,Bronze,false,4);
            Set(sluice,"brake",brake.transform);
            Crank("Carriage crank",-3.5f,-10,sluice,true);
            var pond = Box("Pond water and road collision",-3.5f,-15.3f,2.5f,3.4f,Water,true,-20);
            Set(sluice,"highWater",pond);
            var basin = new GameObject("Exposed basin");
            Box("Revealed paving",-3.5f,-15.3f,2.5f,3.4f,new Color(.51f,.48f,.35f),false,-30).transform.SetParent(basin.transform,true);
            Set(sluice,"exposedBasin",basin);
            basin.SetActive(false);

            var chest = Box("Basin chest",0,-20,1.15f,.7f,new Color(.32f,.20f,.12f));
            var reward = chest.AddComponent<RewardChest>();
            Set(reward,"rewardId","prototype/sluice/cache");
            Set(reward,"requiredMilestone","prototype/sluice/pond-low");
            var closed = Box("Closed lid",0,-19.9f,1.25f,.45f,Bronze,false,5);
            closed.transform.SetParent(chest.transform,true);
            var open = Box("Raised lid",0,-19.45f,1.25f,.25f,Bronze,false,5);
            open.transform.SetParent(chest.transform,true);
            open.SetActive(false);
            Set(reward,"closedVisual",closed);
            Set(reward,"openVisual",open);
            // A real fire supports rest/death/reload checks without touching production checkpoints.
            var fire = Instance("Assets/Prefabs/World/Bonfire.prefab");
            fire.transform.position = new Vector3(-3,4);
            var bonfire = fire.GetComponent<Bonfire>();
            Set(bonfire,"id","prototype/sluice/fire");
            Set(bonfire,"displayName","Sluice Proof Fire");
            Set(bonfire,"allowsUpgrades",false);
            var proof = new GameObject("Sluice Proof Setup").AddComponent<LowlandsSluiceProof>();
            Set(proof,"sluice",sluice);
        }

        static GameObject Instance(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing prefab: " + path);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        static GameObject Box(string name,float x,float y,float w,float h,Color color,bool solid=true,int order=0)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(x,y);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(w,h);
            sr.color = color;
            sr.sortingOrder = order;
            if (solid) go.AddComponent<BoxCollider2D>().size = new Vector2(w,h);
            return go;
        }

        static void WallWithSlot(string name,float x,float bottom,float top,float opening)
        {
            const float gap = .625f;
            float lowerTop = opening-gap*.5f, upperBottom = opening+gap*.5f;
            Box(name+" lower",x,(bottom+lowerTop)*.5f,.5f,lowerTop-bottom,Stone);
            Box(name+" upper",x,(top+upperBottom)*.5f,.5f,top-upperBottom,Stone);
        }

        static GameObject Target(string name,float x,float y,float w,float h,RecallSluice sluice,SluicePart part)
        {
            var go = Box(name,x,y,w,h,Bronze,true,3);
            var target = go.AddComponent<SluiceTarget>();
            Set(target,"sluice",sluice);
            Set(target,"part",(int)part);
            return go;
        }

        static void Crank(string name,float x,float y,RecallSluice sluice,bool carriage)
        {
            var go = Box(name,x,y,.55f,.55f,Bronze,false,4);
            var crank = go.AddComponent<SluiceCrank>();
            Set(crank,"sluice",sluice);
            Set(crank,"carriage",carriage);
        }

        static void Set(Object target,string name,object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(name);
            if (prop == null) throw new InvalidOperationException(target.GetType().Name+" has no field "+name);
            if (value is Object[] array) prop.arraySize = array.Length;
            else if (value is Object obj) prop.objectReferenceValue = obj;
            else if (value is string str) prop.stringValue = str;
            else if (value is bool flag) prop.boolValue = flag;
            else if (value is int number) prop.intValue = number;
            else if (value is Vector3 position) prop.vector3Value = position;
            else throw new ArgumentException("Unsupported serialized value");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
