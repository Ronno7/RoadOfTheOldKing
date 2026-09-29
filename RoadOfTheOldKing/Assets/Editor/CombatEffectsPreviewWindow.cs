using TheLostShrine.Input;
using TheLostShrine.Player;
using TheLostShrine.Prototype;
using TheLostShrine.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheLostShrine.Editor
{
    public sealed class CombatEffectsPreviewWindow : EditorWindow
    {
        [SerializeField] private float speed = .25f, angle, distance = 1.4f, zoom = 3.2f;
        [SerializeField] private bool loop, pauseAtActive = true, target = true, wall, mute;
        [SerializeField] private CombatPreviewDriver.Action action;
        private RenderTexture preview;
        private PlayerCombatController player;
        private CombatPreviewDriver driver;
        private bool ownsTime;
        private float previousSpeed;
        private bool previousPause, previousAudioPause, previousBackground;
        private Vector2 scroll;

        [MenuItem("Road of the Old King/Combat/Combat Effects Preview")]
        public static void Open() => GetWindow<CombatEffectsPreviewWindow>("Combat effects");

        private void OnEnable()
        {
            minSize = new Vector2(480, 640);
            EditorApplication.update += Refresh;
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += RestoreTime;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Refresh;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload -= RestoreTime;
            if (driver != null) { driver.Loop = false; driver.PauseAtActive = false; }
            RestoreTime();
            if (preview != null) { preview.Release(); DestroyImmediate(preview); }
        }

        private void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) RestoreTime();
            if (state == PlayModeStateChange.EnteredPlayMode && CombatPreviewSession.IsPreview)
            { Refresh(); ApplyTime(); }
        }

        private void Refresh()
        {
            if (ownsTime && EditorApplication.isPlaying)
                TheLostShrine.UI.SimulationPause.SetAudioPaused(mute || EditorApplication.isPaused);
            if (EditorApplication.isPlaying && player == null)
                player = FindFirstObjectByType<PlayerCombatController>();
            driver = player != null ? player.GetComponent<CombatPreviewDriver>() : null;
            if (driver != null)
            {
                driver.Angle = angle; driver.TargetDistance = distance;
                driver.SelectedAction = action; driver.Loop = loop; driver.PauseAtActive = pauseAtActive;
                driver.ShowTarget = target; driver.ShowWall = wall;
                driver.Camera.orthographicSize = zoom;
            }
            Repaint();
        }

        private void OwnTime()
        {
            if (ownsTime || !EditorApplication.isPlaying) return;
            previousSpeed = TheLostShrine.UI.SimulationPause.UnpausedTimeScale; previousPause = EditorApplication.isPaused;
            previousAudioPause = TheLostShrine.UI.SimulationPause.UnpausedAudio; previousBackground = Application.runInBackground;
            Application.runInBackground = true;
            ownsTime = true;
        }

        private void ApplyTime()
        {
            OwnTime();
            TheLostShrine.UI.SimulationPause.SetTimeScale(speed);
            TheLostShrine.UI.SimulationPause.SetAudioPaused(mute || EditorApplication.isPaused);
        }

        private void RestoreTime()
        {
            if (!ownsTime) return;
            TheLostShrine.UI.SimulationPause.SetTimeScale(previousSpeed);
            TheLostShrine.UI.SimulationPause.SetAudioPaused(previousAudioPause);
            Application.runInBackground = previousBackground;
            if (EditorApplication.isPlaying) EditorApplication.isPaused = previousPause;
            ownsTime = false;
        }

        private void OnGUI()
        {
            using (var view = new EditorGUILayout.ScrollViewScope(scroll))
            { scroll = view.scrollPosition; DrawControls(); }
        }

        private void DrawControls()
        {
            EditorGUILayout.LabelField("Combat effects", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Launch the save-free practice scene with the standard player, axe and dummy. Your open scenes return when Play Mode ends.", MessageType.Info);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("Start practice preview", GUILayout.Height(30))) CombatPreviewSession.Start();
            }
            else
            {
                EditorGUILayout.LabelField(driver != null ? "Practice preview • no saves" : "Live session • " + SceneManager.GetActiveScene().name);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(EditorApplication.isPaused ? "Resume" : "Pause", GUILayout.Height(28)))
                    { OwnTime(); EditorApplication.isPaused = !EditorApplication.isPaused; ApplyTime(); }
                    using (new EditorGUI.DisabledScope(!EditorApplication.isPaused))
                        if (GUILayout.Button("Step frame", GUILayout.Height(28))) { OwnTime(); EditorApplication.Step(); }
                    if (driver != null && GUILayout.Button("Stop preview", GUILayout.Height(28))) EditorApplication.isPlaying = false;
                }
            }
            EditorGUI.BeginChangeCheck();
            speed = EditorGUILayout.Slider("Game speed", speed, .05f, 1f);
            using (new EditorGUILayout.HorizontalScope())
                foreach (float preset in new[] { .1f, .25f, .5f, 1f })
                    if (GUILayout.Button(preset.ToString("0.##") + "×")) { speed = preset; GUI.changed = true; }
            mute = EditorGUILayout.Toggle("Mute audio", mute);
            if (EditorGUI.EndChangeCheck() && EditorApplication.isPlaying) ApplyTime();
            EditorGUILayout.LabelField("Pause freezes the current effect. Step advances one Unity frame at the selected speed.", EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying && driver == null))
            {
                EditorGUILayout.Space();
                action = (CombatPreviewDriver.Action)EditorGUILayout.EnumPopup("Action", action);
                angle = EditorGUILayout.Slider("Direction (degrees)", angle, 0, 360);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("East")) angle = 0;
                    if (GUILayout.Button("North")) angle = 90;
                    if (GUILayout.Button("West")) angle = 180;
                    if (GUILayout.Button("South")) angle = 270;
                }
                distance = EditorGUILayout.Slider("Target distance", distance, .5f, 4f);
                zoom = EditorGUILayout.Slider("Preview zoom", zoom, 2f, 9f);
                using (new EditorGUILayout.HorizontalScope())
                { target = GUILayout.Toggle(target, "Dummy"); wall = GUILayout.Toggle(wall, "Blocking wall"); loop = GUILayout.Toggle(loop, "Loop"); }
                pauseAtActive = EditorGUILayout.Toggle("Pause at active melee", pauseAtActive);
                using (new EditorGUI.DisabledScope(driver == null))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Replay action", GUILayout.Height(28)))
                    { Refresh(); OwnTime(); driver.Replay(); EditorApplication.isPaused = false; ApplyTime(); }
                    if (GUILayout.Button("Reset / apply setup", GUILayout.Height(28)))
                    { Refresh(); driver.ResetPreview(); if (EditorApplication.isPaused) EditorApplication.Step(); }
                }
            }
            if (EditorApplication.isPlaying && driver == null)
                EditorGUILayout.LabelField("Live mode controls time only. Exit Play Mode to launch repeatable practice actions.", EditorStyles.wordWrappedMiniLabel);

            var weapon = player != null ? player.Weapon : null;
            if (weapon != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("State", weapon.State + " / " + weapon.LightPhase + (weapon.IsImpactPaused ? " / impact pause" : ""));
                EditorGUILayout.LabelField("Light reach / arc", weapon.LightReach.ToString("0.00") + " units / " + weapon.LightArc.ToString("0") + "°");
                float duration = weapon.LightDuration;
                EditorGUILayout.LabelField("Windup / active / recovery", string.Format("{0:0} / {1:0} / {2:0} ms", duration * weapon.Settings.lightWindupFraction * 1000,
                    duration * (weapon.Settings.lightSwingEndFraction - weapon.Settings.lightWindupFraction) * 1000, duration * (1 - weapon.Settings.lightSwingEndFraction) * 1000));
                var bar = GUILayoutUtility.GetRect(10, 18, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(bar, weapon.State == AxeState.LightChop ? weapon.AttackProgress : 0, "Swing progress");
                if (driver != null) EditorGUILayout.LabelField("Confirmed hits", driver.Hits.ToString());
            }
            Rect area = GUILayoutUtility.GetRect(10, Mathf.Max(180, position.height - 550), GUILayout.ExpandWidth(true));
            DrawPreview(area);
        }

        private void DrawPreview(Rect area)
        {
            var camera = EditorApplication.isPlaying ? Camera.main : null;
            if (camera == null || Event.current.type != EventType.Repaint) return;
            int width = Mathf.Clamp(Mathf.RoundToInt(area.width), 128, 1200);
            int height = Mathf.Clamp(Mathf.RoundToInt(area.height), 128, 900);
            if (preview == null || preview.width != width || preview.height != height)
            {
                if (preview != null) { preview.Release(); DestroyImmediate(preview); }
                preview = new RenderTexture(width, height, 24) { name = "Combat effects preview", hideFlags = HideFlags.HideAndDontSave };
            }
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            try { camera.targetTexture = preview; camera.Render(); GUI.DrawTexture(area, preview, ScaleMode.ScaleToFit, false); }
            finally { camera.targetTexture = oldTarget; RenderTexture.active = oldActive; }
        }
    }

    [InitializeOnLoad]
    public static class CombatPreviewSession
    {
        public const string ScenePath = "Assets/Scenes/CombatEffectsPreview.unity";
        private const string Pending = "CombatEffectsPreview.Pending", PreviousStart = "CombatEffectsPreview.PreviousStart";
        public static bool IsPreview => EditorApplication.isPlaying && SceneManager.GetActiveScene().path == ScenePath;
        static CombatPreviewSession() => EditorApplication.playModeStateChanged += OnPlayMode;

        public static void Start()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            CombatEffectsPreviewWindow.Open();
            EnsureScene();
            SessionState.SetString(PreviousStart, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(Pending, true);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorApplication.isPlaying = true;
        }

        private static void EnsureScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            var original = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try { EditorSceneManager.SaveScene(scene, ScenePath); }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(original); }
        }

        private static void RestoreStartScene()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(PreviousStart, ""));
            SessionState.SetBool(Pending, false);
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RestoreStartScene();
                if (IsPreview)
                {
                    try { BuildPractice(); }
                    catch (System.Exception exception)
                    { Debug.LogException(exception); EditorApplication.isPlaying = false; }
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode) RestoreStartScene();
        }

        private static void BuildPractice()
        {
            var camera = new GameObject("Preview camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 3.8f;
            camera.transform.position = new Vector3(0, .5f, -10);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.20f, .25f, .23f);
            camera.gameObject.AddComponent<AudioListener>();
            var holder = new GameObject("Preview player"); holder.SetActive(false);
            var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"), holder.transform);
            var input = player.AddComponent<CombatPreviewDriver>();
            input.Player = player.GetComponent<PlayerCombatController>(); input.Camera = camera;
            player.GetComponent<PlayerCombatInput>().enabled = false;
            player.GetComponent<PlayerMovementInput>().enabled = false;
            var serialized = new SerializedObject(input.Player);
            serialized.FindProperty("inputSource").objectReferenceValue = input;
            serialized.FindProperty("aimCamera").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            input.Weapon = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Axe.prefab")).GetComponent<AxeWeapon>();
            input.Target = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/PracticeDummy.prefab"), new Vector3(1.4f, 0, 0), Quaternion.identity).GetComponent<PracticeTarget>();
            var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/WhiteSquare.png");
            var wall = new GameObject("Preview blocking wall");
            var sprite = wall.AddComponent<SpriteRenderer>(); sprite.sprite = square; sprite.color = new Color(.36f,.31f,.25f); sprite.sortingLayerName = "World";
            wall.transform.localScale = new Vector3(.15f, 3f, 1); wall.AddComponent<BoxCollider2D>();
            input.Wall = wall;
            holder.SetActive(true); input.Initialize();
        }
    }
}
