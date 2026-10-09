using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoadOfTheOldKing.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace RoadOfTheOldKing.EditorTools
{
    // One-time route seed. The resulting GreenLowlands scene remains the authoring owner.
    public static class GreenLowlandsLayoutBuilder
    {
        const string ScenePath = "Assets/Scenes/GreenLowlands.unity";
        const string RootName = "Sleeping River Greybox";
        static readonly Color Grass = new Color(.34f,.43f,.27f);
        static readonly Color Upper = new Color(.47f,.51f,.31f);
        static readonly Color Silt = new Color(.45f,.40f,.29f);
        static readonly Color Cliff = new Color(.24f,.29f,.24f);
        static readonly Color Water = new Color(.20f,.42f,.47f);
        static Tile squareTile;
        static Sprite square;
        static Font font;

        [MenuItem("Road of the Old King/World/Seed Sleeping River Layout")]
        public static void Seed()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var original = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("Preserve unsaved regional work first.");
                if (scene.GetRootGameObjects().Any(g=>g.name==RootName)) throw new InvalidOperationException("Layout already seeded; edit the scene directly.");
                Directory.CreateDirectory("../tmp");
                string backup = "../tmp/GreenLowlands-before-sleeping-river.unity";
                if (!File.Exists(backup)) File.Copy(ScenePath, backup);
                SceneManager.SetActiveScene(scene);
                Build(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
            }
            finally
            {
                if (original.IsValid()) SceneManager.SetActiveScene(original);
                if (opened) EditorSceneManager.CloseScene(scene,true);
            }
        }

        static void Build(Scene scene)
        {
            square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/WhiteSquare.png");
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/RotOKPixel.ttf");
            const string tilePath = "Assets/Art/Tiles/Prototype/SleepingRiverBlock.asset";
            squareTile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (squareTile == null)
            {
                squareTile = ScriptableObject.CreateInstance<Tile>();
                squareTile.sprite = square;
                squareTile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(squareTile,tilePath);
            }
            var root = new GameObject(RootName);
            root.AddComponent<Grid>();
            var ground = Map(root.transform,"Ground - placeholder",Grass,40,false);
            var heights = Map(root.transform,"Upper terraces - placeholder",Upper,41,false);
            var basin = Map(root.transform,"Basin paving - placeholder",Silt,41,false);
            var blocked = Map(root.transform,"Cliffs and outer boundary",Cliff,42,true);
            var permanent = Map(root.transform,"Permanent river continuation",Water,43,true);
            var pondRoot = new GameObject("High pond - saved collision state");
            pondRoot.transform.SetParent(root.transform,false);
            var pond = Map(pondRoot.transform,"Pond and northern road-cut",Water,44,true);

            RectInt[] walking = {
                new RectInt(30,20,14,6),new RectInt(29,23,25,10),new RectInt(50,29,11,5),
                new RectInt(57,32,4,10),new RectInt(42,38,19,9),new RectInt(42,36,6,4),
                new RectInt(48,45,6,15),new RectInt(34,56,20,5),new RectInt(33,48,5,10),
                new RectInt(12,47,24,5),new RectInt(4,51,12,8),new RectInt(3,27,9,29),
                new RectInt(12,24,27,23),new RectInt(16,20,4,5),new RectInt(39,42,5,13),
                new RectInt(63,38,5,7),new RectInt(54,53,5,3),new RectInt(62,53,5,4)
            };
            for(int y=20;y<64;y++) for(int x=-2;x<71;x++)
            {
                var cell=new Vector3Int(x,y,0);var point=new Vector2Int(x,y);
                bool river=x>=20&&x<24&&y<27;
                bool floor=walking.Any(r=>r.Contains(point));
                bool ridge=(x>=12&&x<33&&y>=46&&y<48)||(x>=44&&x<46&&y>=42&&y<49);
                ground.SetTile(cell,squareTile);
                if (!floor || river || ridge) blocked.SetTile(cell,squareTile);
                if (river) permanent.SetTile(cell,squareTile);
                if ((x>=42&&x<61&&y>=36&&y<47)||(x>=33&&y>=48)||(x>=12&&x<36&&y>=47&&y<52))
                    if(floor) heights.SetTile(cell,squareTile);
                bool pondCell=(x>=12&&x<39&&y>=24&&y<47)||(x>=39&&x<44&&y>=42&&y<52);
                bool island=(x>=17&&x<20&&y>=25&&y<28)||(x>=29&&x<32&&y>=43&&y<46);
                if(pondCell && floor && !river && !ridge)
                {
                    basin.SetTile(cell,squareTile);
                    if(!island) pond.SetTile(cell,squareTile);
                }
            }
            for(int x=40;x<71;x++) blocked.SetTile(new Vector3Int(x,19,0),squareTile);

            var sluice = CopySluice(root.transform,scene);
            var serialized = new SerializedObject(sluice);
            var oldWater = (GameObject)serialized.FindProperty("highWater").objectReferenceValue;
            oldWater.transform.SetParent(pondRoot.transform,true);
            serialized.FindProperty("highWater").objectReferenceValue=pondRoot;
            serialized.FindProperty("puzzleId").stringValue="green-lowlands/sluice";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Open only the two new seams, outside the accepted 40x20 art rectangle.
            var zone=scene.GetRootGameObjects().First(g=>g.name=="Green Lowlands Zone");
            var arrivalCollision=zone.transform.Find("Collision").GetComponent<Tilemap>();
            for(int x=16;x<20;x++) arrivalCollision.SetTile(new Vector3Int(x,20,0),null);
            for(int x=30;x<38;x++) arrivalCollision.SetTile(new Vector3Int(x,20,0),null);

            var marks=new GameObject("Design markers - replace during integration");
            marks.transform.SetParent(root.transform,false);
            Marker(marks.transform,"Mill overlook",18,22,new Color(.8f,.72f,.47f));
            Marker(marks.transform,"S3 - basin shard",18.5f,26.5f,new Color(.98f,.80f,.26f));
            Marker(marks.transform,"Lower pasture",45,28,Grass);
            Marker(marks.transform,"H1 - upper spur",45,36.5f,new Color(.85f,.38f,.38f));
            Marker(marks.transform,"S2 - orchard chest",53,43,new Color(.98f,.80f,.26f));
            Marker(marks.transform,"H2 - lower recess",30.5f,44.5f,new Color(.85f,.38f,.38f));
            Marker(marks.transform,"North bank",27,49,Upper);
            Marker(marks.transform,"Barrow road - reserved exit",42,54,new Color(.70f,.70f,.62f));
            Marker(marks.transform,"Storehouse - future interior",26,30,new Color(.10f,.12f,.13f));
            Marker(marks.transform,"F1 - rope workshop",65,42,new Color(.73f,.55f,.32f));
            Marker(marks.transform,"High Pass - far-side shortcut",64,55,new Color(.70f,.70f,.62f));
            Marker(marks.transform,"Basin coin chest",33,31,new Color(.73f,.55f,.32f));
            // Reserve a safe surface arrival; no unconfigured exit pretends to load an interior.
            var entry=new GameObject("Storehouse return reservation");
            entry.transform.SetParent(root.transform,false);entry.transform.position=new Vector3(26,28);
            var spawn=entry.AddComponent<SceneSpawnPoint>();
            var spawnData=new SerializedObject(spawn);spawnData.FindProperty("id").stringValue="from-lowlands-storehouse";spawnData.ApplyModifiedPropertiesWithoutUndo();
            foreach(var c in root.GetComponentsInChildren<TilemapCollider2D>()) c.ProcessTilemapChanges();
            arrivalCollision.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            Physics2D.SyncTransforms();
        }

        static RecallSluice CopySluice(Transform parent,Scene destination)
        {
            var source=EditorSceneManager.OpenPreviewScene(LowlandsSluiceProofBuilder.ScenePath);
            try
            {
                var group=new GameObject("Sluice bays - placeholder");
                SceneManager.MoveGameObjectToScene(group,source);
                var excluded=new HashSet<string>{"Ground","Player","FollowCamera","Main Light","Isolated Sluice Session","Sluice Proof Setup","Bonfire","Basin chest","North boundary","East boundary"};
                foreach(var go in source.GetRootGameObjects())
                    if(go!=group&&!excluded.Contains(go.name)) go.transform.SetParent(group.transform,true);
                var clone=Object.Instantiate(group);clone.name=group.name;
                SceneManager.MoveGameObjectToScene(clone,destination);
                clone.transform.SetParent(parent,false);clone.transform.localPosition=new Vector3(8,50);
                foreach(var renderer in clone.GetComponentsInChildren<SpriteRenderer>(true)) renderer.sortingLayerName="World";
                // The proof's east wall gains a lower basin exit, while the high approach enters from north.
                Box(clone.transform,"East outer wall upper",4,-5,1,22,Cliff,true);
                Box(clone.transform,"East outer wall lower",4,-23,1,4,Cliff,true);
                return clone.GetComponentInChildren<RecallSluice>();
            }
            finally { EditorSceneManager.ClosePreviewScene(source); }
        }

        static Tilemap Map(Transform parent,string name,Color color,int order,bool solid)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            var map=go.AddComponent<Tilemap>();map.color=color;
            var renderer=go.AddComponent<TilemapRenderer>();renderer.sortingLayerName="Ground";renderer.sortingOrder=order;
            if(solid)go.AddComponent<TilemapCollider2D>();
            return map;
        }

        static GameObject Box(Transform parent,string name,float x,float y,float w,float h,Color color,bool solid)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(x,y);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=square;sr.drawMode=SpriteDrawMode.Sliced;sr.size=new Vector2(w,h);sr.color=color;sr.sortingLayerName="World";
            if(solid)go.AddComponent<BoxCollider2D>().size=new Vector2(w,h);
            return go;
        }

        static void Marker(Transform parent,string name,float x,float y,Color color)
        {
            var go=Box(parent,name,x,y,.6f,.6f,color,false);
            var sr=go.GetComponent<SpriteRenderer>();sr.sortingLayerName="World";sr.sortingOrder=5;
            var label=new GameObject("Greybox label");label.transform.SetParent(go.transform,false);label.transform.localPosition=new Vector3(0,.8f,0);
            var text=label.AddComponent<TextMesh>();text.text=name;text.font=font;text.fontSize=32;text.characterSize=.4f;text.anchor=TextAnchor.LowerCenter;text.color=new Color(.94f,.91f,.76f);
            var renderer=label.GetComponent<MeshRenderer>();renderer.sharedMaterial=font.material;renderer.sortingLayerName="World";renderer.sortingOrder=10;
        }
    }
}
