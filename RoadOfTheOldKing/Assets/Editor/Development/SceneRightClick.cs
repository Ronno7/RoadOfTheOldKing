using System.Collections.Generic;
using System.Linq;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.EditorTools
{
    // Adds to Unity's own right-click menu in the Scene view: start Play at that spot on a throwaway save, or see what's
    // on that cell and choose a tilemap to paint on. Unity builds that menu from CONTEXT/<active tool context>/ items.
    [InitializeOnLoad]
    public static class SceneRightClick
    {
        private const string Items = "CONTEXT/GameObjectToolContext/";
        private const int Priority = 1000;
        private static Vector2 hoveredWorld, hoveredGui;
        private static bool hasHover, cellMenuPending;

        static SceneRightClick()
        {
            SceneView.duringSceneGui += OnSceneGui;
            // Leaving Play doesn't reload the domain, so that needs the event. This check covers a crash during Play.
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && DevPlayFromHere.IsActive) EndSession();
            };
            if (DevPlayFromHere.IsActive && !EditorApplication.isPlayingOrWillChangePlaymode) EndSession();
        }

        // Unity opens its menu through a shortcut and passes on no position, so the spot is where the mouse last moved in
        // the view. Moving over the open menu is in another window, so it doesn't disturb this.
        private static void OnSceneGui(SceneView view)
        {
            view.wantsMouseMove = true;
            var e = Event.current;
            if (e.type == EventType.MouseMove)
            {
                hoveredGui = e.mousePosition;
                hoveredWorld = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
                hasHover = true;
            }
            // A menu item runs outside any GUI event, so its popup waits for the view's next one.
            if (cellMenuPending && e.type == EventType.Layout)
            {
                cellMenuPending = false;
                BuildCellMenu(ClickedSpot()).DropDown(new Rect(hoveredGui, Vector2.zero));
            }
        }

        private static Vector2 ClickedSpot()
        {
            if (hasHover) return hoveredWorld;
            var view = SceneView.lastActiveSceneView;
            return view != null ? (Vector2)view.pivot : Vector2.zero;
        }

        public enum PlayStart { Fresh, WatcherDead, BasinDrained }

        [MenuItem(Items + "Play from here", false, Priority)]
        private static void PlayHere(MenuCommand command) => PlayFrom(ClickedSpot(), PlayStart.Fresh);

        [MenuItem(Items + "Play from here", true)]
        private static bool CanPlayHere(MenuCommand command) => !EditorApplication.isPlaying && Find<GameSession>() != null;

        [MenuItem(Items + "Play from here, Watcher dead", false, Priority + 1)]
        private static void PlayHereWatcherDead(MenuCommand command) => PlayFrom(ClickedSpot(), PlayStart.WatcherDead);

        [MenuItem(Items + "Play from here, Watcher dead", true)]
        private static bool CanPlayHereWatcherDead(MenuCommand command) => CanPlayHereDrained(command);

        [MenuItem(Items + "Play from here, basin drained", false, Priority + 2)]
        private static void PlayHereDrained(MenuCommand command) => PlayFrom(ClickedSpot(), PlayStart.BasinDrained);

        [MenuItem(Items + "Play from here, basin drained", true)]
        private static bool CanPlayHereDrained(MenuCommand command) => CanPlayHere(command) && Find<RecallSluice>() != null;

        [MenuItem(Items + "What's on this cell", false, Priority + 3)]
        private static void ShowCell(MenuCommand command)
        {
            cellMenuPending = true;
            SceneView.RepaintAll();
        }

        private static GenericMenu BuildCellMenu(Vector2 world)
        {
            var menu = new GenericMenu();
            var cell = ReadCell(world);
            menu.AddDisabledItem(new GUIContent($"Cell {cell.Cell.x}, {cell.Cell.y}"));
            foreach (var entry in cell.Tiles)
            {
                var map = entry.Key;
                menu.AddItem(new GUIContent($"Paint on: {map.name}  ({entry.Value})"), false, () => PaintOn(map));
            }
            if (cell.WaterWithoutCollision) menu.AddDisabledItem(new GUIContent("! Water drawn here has no collision"));
            if (cell.Solids.Count == 0) menu.AddDisabledItem(new GUIContent("Nothing solid here"));
            foreach (var solid in cell.Solids)
                menu.AddItem(new GUIContent("Solid here: " + Describe(solid)), false, () => Select(solid.gameObject));
            return menu;
        }

        public sealed class CellReport
        {
            public Vector3Int Cell;
            public List<KeyValuePair<Tilemap, string>> Tiles = new List<KeyValuePair<Tilemap, string>>();
            public List<Collider2D> Solids = new List<Collider2D>();
            public bool WaterWithoutCollision;
        }

        // Tilemaps that belong to a prop's prefab can't be painted here; those show up as solids if they block.
        public static CellReport ReadCell(Vector2 world)
        {
            var report = new CellReport();
            Physics2D.SyncTransforms();
            report.Solids = Physics2D.OverlapPointAll(world).Where(c => !c.isTrigger).ToList();
            var scene = SceneManager.GetActiveScene();
            bool water = false;
            foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (map.gameObject.scene != scene || PrefabUtility.IsPartOfPrefabInstance(map)) continue;
                var cell = map.WorldToCell(world);
                var tile = map.GetTile(cell);
                if (tile == null) continue;
                report.Cell = cell;
                report.Tiles.Add(new KeyValuePair<Tilemap, string>(map, tile.name));
                if (map.gameObject.activeInHierarchy && tile.name.Contains("Water")) water = true;
            }
            report.Tiles.Sort((a, b) => string.CompareOrdinal(a.Key.name, b.Key.name));
            report.WaterWithoutCollision = water && report.Solids.Count == 0;
            return report;
        }

        private static string Describe(Collider2D solid) =>
            solid.name + (solid.transform.parent != null ? " <" + solid.transform.parent.name + ">" : "");

        private static void PaintOn(Tilemap map)
        {
            GridPaintingState.scenePaintTarget = map.gameObject;
            Select(map.gameObject);
        }

        private static void Select(GameObject target)
        {
            Selection.activeGameObject = target;
            EditorGUIUtility.PingObject(target);
        }

        private static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        // Always a new save holding only the axe and Recall. The real save is never read, so nothing already done
        // carries over: the Watcher is alive and the basin full unless a start asks otherwise.
        public static void PlayFrom(Vector2 spot, PlayStart start)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var session = Find<GameSession>();
            if (session == null) { Debug.LogWarning("Play from here needs a game session in the open scene."); return; }
            ClearSave();
            var store = new PlayerPrefsProgressStore(DevPlayFromHere.SaveKey);
            var progress = store.Load();
            progress.hasAxe = progress.recallUnlocked = true;
            var sluice = Find<RecallSluice>();
            if (start != PlayStart.Fresh && sluice != null)
            {
                // A recorded defeat is what keeps the Watcher down and frees the sluice's controls.
                var guardian = new SerializedObject(sluice).FindProperty("guardian").objectReferenceValue as EncounterUnlock;
                if (guardian != null) progress.Complete(guardian.Milestone);
                if (start == PlayStart.BasinDrained) progress.Complete(sluice.LowWaterId);
            }
            store.Save(progress);
            PlayerPrefs.Save();
            DevPlayFromHere.Arm(spot);
            EditorSceneManager.playModeStartScene = null;
            EditorApplication.isPlaying = true;
        }

        private static void EndSession()
        {
            DevPlayFromHere.Disarm();
            ClearSave();
            PlayerPrefs.Save();
        }

        private static void ClearSave()
        {
            PlayerPrefs.DeleteKey(DevPlayFromHere.SaveKey);
            PlayerPrefs.DeleteKey(DevPlayFromHere.SaveKey + ".Resume");
        }
    }
}
