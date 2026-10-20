using RoadOfTheOldKing.Enemies;
using System;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Editor;
using RoadOfTheOldKing.Tutorial;
using RoadOfTheOldKing.World;
using UnityEditor;
using UnityEngine;

namespace RoadOfTheOldKing.EditorTools
{
    public static class LowlandsEnemyBuilder
    {
        public const string ScavengerPath = "Assets/Prefabs/Combat/RoadScavenger.prefab";
        public const string BoarPath = "Assets/Prefabs/Combat/MossbackBoar.prefab";

        [MenuItem("Road of the Old King/Art/Create Mossback Boar Prefab")]
        public static void CreateBoar()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BoarPath) != null)
                throw new InvalidOperationException("Mossback Boar already exists; preserve authored tuning.");
            const string folder = "Assets/Settings/Enemies";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Settings", "Enemies");
            var charge = AssetDatabase.LoadAssetAtPath<EnemyAttackDefinition>(folder + "/MossbackCharge.asset");
            if (charge == null)
            {
                charge = ScriptableObject.CreateInstance<EnemyAttackDefinition>();
                charge.settings = new EnemyAttackSpec { windup = .85f, active = .8f, recovery = 1.1f,
                    chargeSpeed = 10f, trackingTime = .35f, reach = 1.05f, arc = 80, damage = 24, knockback = 4f, stagger = .25f };
                AssetDatabase.CreateAsset(charge, folder + "/MossbackCharge.asset");
            }
            var root = PrefabUtility.LoadPrefabContents(ScavengerPath);
            try
            {
                root.name = "Mossback Boar";
                var set = AssetDatabase.LoadAssetAtPath<EnemyAnimationSet>("Assets/Animations/Enemies/MossbackBoar/AnimationSet.asset");
                var view = new SerializedObject(root.GetComponent<MeleeSpriteView>());
                view.FindProperty("animations").objectReferenceValue = set;
                var sprite = (SpriteRenderer)view.FindProperty("bodyVisual").objectReferenceValue;
                sprite.sprite = set.rotations.Frame(6, 0);
                view.ApplyModifiedPropertiesWithoutUndo();
                var brain = new SerializedObject(root.GetComponent<PursuitBrain>());
                brain.FindProperty("attack").objectReferenceValue = charge;
                brain.FindProperty("moveSpeed").floatValue = 2.8f;
                brain.FindProperty("noticeRadius").floatValue = 8f;
                brain.FindProperty("leashRadius").floatValue = 12f;
                brain.FindProperty("attackDistance").floatValue = 7f;
                brain.ApplyModifiedPropertiesWithoutUndo();
                var hp = new SerializedObject(root.GetComponent<Damageable>());
                hp.FindProperty("maxHealth").intValue = 90; hp.ApplyModifiedPropertiesWithoutUndo();
                var shape = root.GetComponent<CircleCollider2D>();
                shape.radius = .5f;
                SetHurtbox(root, new Vector2(1.65f, 1.9f), new Vector2(0, .9f));
                root.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var bar = new SerializedObject(root.GetComponent<EnemyHealthIndicator>());
                bar.FindProperty("anchorHeight").floatValue = 2.1f; bar.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, BoarPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // Explicit one-time authoring command, never an automatic scene painter or prefab overwrite.
        [MenuItem("Road of the Old King/Art/Create Road Scavenger Prefab")]
        public static void CreateScavenger()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ScavengerPath) != null)
                throw new InvalidOperationException("Road Scavenger already exists; tune the authored prefab directly.");
            var set = AssetDatabase.LoadAssetAtPath<EnemyAnimationSet>("Assets/Animations/Enemies/RoadScavenger/AnimationSet.asset");
            if (set == null || set.rotations == null || set.walk == null)
                throw new InvalidOperationException("Import the approved Road Scavenger standing and walking views first.");
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Combat/MeleeSentinel.prefab");
            try
            {
                root.name = "Road Scavenger";
                var oldView = root.GetComponent<DebugEnemyView>();
                var old = new SerializedObject(oldView);
                var body = (SpriteRenderer)old.FindProperty("bodyVisual").objectReferenceValue;
                var outline = (LineRenderer)old.FindProperty("attackOutline").objectReferenceValue;
                ((Transform)old.FindProperty("facingMarker").objectReferenceValue).gameObject.SetActive(false);
                ((TextMesh)old.FindProperty("label").objectReferenceValue).gameObject.SetActive(false);
                UnityEngine.Object.DestroyImmediate(oldView);
                body.transform.localScale = Vector3.one; body.transform.localPosition = Vector3.zero;
                body.sprite = set.rotations.Frame(6, 0); body.color = Color.white;
                var view = new SerializedObject(root.AddComponent<MeleeSpriteView>());
                view.FindProperty("animations").objectReferenceValue = set;
                view.FindProperty("bodyVisual").objectReferenceValue = body;
                view.FindProperty("attackOutline").objectReferenceValue = outline;
                view.ApplyModifiedPropertiesWithoutUndo();
                var health = new SerializedObject(root.GetComponent<Damageable>());
                health.FindProperty("maxHealth").intValue = 75; health.ApplyModifiedPropertiesWithoutUndo();
                var brain = new SerializedObject(root.GetComponent<PursuitBrain>());
                brain.FindProperty("moveSpeed").floatValue = 3.3f;
                brain.FindProperty("noticeRadius").floatValue = 7f;
                brain.FindProperty("leashRadius").floatValue = 10f;
                brain.FindProperty("attackDistance").floatValue = 1.85f;
                brain.FindProperty("attackReach").floatValue = 2.25f;
                brain.FindProperty("windupDuration").floatValue = .5f;
                brain.FindProperty("recoveryDuration").floatValue = .8f;
                brain.FindProperty("damage").intValue = 18;
                brain.ApplyModifiedPropertiesWithoutUndo();
                SetHurtbox(root, new Vector2(1.1f, 1.65f), new Vector2(0, .75f));
                var coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/CoinSource_Placeholder.prefab");
                var coinSource = new SerializedObject(coinPrefab.GetComponent<CoinSource>());
                var coin = UnityEngine.Object.Instantiate((GameObject)coinSource.FindProperty("coinVisual").objectReferenceValue, root.transform);
                coin.name = "Bronze coin drop"; coin.transform.localPosition = Vector3.zero; coin.SetActive(false);
                var drop = new SerializedObject(root.AddComponent<EnemyCoinDrop>());
                drop.FindProperty("coinVisual").objectReferenceValue = coin; drop.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ScavengerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void SetHurtbox(GameObject root, Vector2 size, Vector2 offset)
        {
            var existing = root.GetComponentInChildren<EnemyHurtbox>(true);
            var body = existing != null ? existing.gameObject : new GameObject("Hit body");
            body.transform.SetParent(root.transform, false);
            var shape = body.GetComponent<CapsuleCollider2D>();
            if (shape == null) shape = body.AddComponent<CapsuleCollider2D>();
            shape.isTrigger = true; shape.size = size; shape.offset = offset;
            if (existing == null) existing = body.AddComponent<EnemyHurtbox>();
            var view = (Component)root.GetComponent<MeleeSpriteView>() ?? root.GetComponent<PredatorSpriteView>();
            if (view == null) return;
            var animationSet = new SerializedObject(view).FindProperty("animations").objectReferenceValue;
            var rotations = (RoadOfTheOldKing.Player.DirectionalSpriteAnimation)
                new SerializedObject(animationSet).FindProperty("rotations").objectReferenceValue;
            var authored = new SerializedObject(existing);
            var sizes = authored.FindProperty("sizes"); var offsets = authored.FindProperty("offsets");
            sizes.arraySize = offsets.arraySize = 8;
            for (int facing = 0; facing < 8; facing++)
            {
                var sprite = rotations.Frame(facing, 0);
                var pixels = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    pixels.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
                    var rect = sprite.rect; int width = (int)rect.width, height = (int)rect.height;
                    var colors = pixels.GetPixels32(); int left = width, right = -1, bottom = height, top = -1;
                    for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                        if (colors[((int)rect.y + y) * pixels.width + (int)rect.x + x].a > 127)
                        { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
                    // One-pixel inset omits the outline and empty sprite canvas.
                    var measuredSize = new Vector2(right - left - 1, top - bottom - 1) / sprite.pixelsPerUnit;
                    var measuredOffset = (new Vector2((left + right + 1) * .5f, (bottom + top + 1) * .5f) - sprite.pivot) / sprite.pixelsPerUnit;
                    sizes.GetArrayElementAtIndex(facing).vector2Value = measuredSize;
                    offsets.GetArrayElementAtIndex(facing).vector2Value = measuredOffset;
                    if (facing == 6) { shape.size = measuredSize; shape.offset = measuredOffset; }
                }
                finally { UnityEngine.Object.DestroyImmediate(pixels); }
            }
            authored.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
