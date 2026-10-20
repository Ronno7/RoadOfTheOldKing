using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.EditorTools
{
    public sealed class LowlandsElevationWindow : EditorWindow
    {
        public const string PlanName = "Height plan";
        public const string MarkingsName = "Height plan markings";
        const string AssetRoot = "Assets/Editor/SceneAuthoring/HeightPlan";
        [SerializeField] int height, brushSize = 1;
        [SerializeField] HeightPlanKind kind;
        [SerializeField] bool overlay = true, labels = true;
        [SerializeField] float opacity = .5f;
        Tilemap plan;
        Tilemap markings, strokeMap;
        bool painting, erase;
        Tool previousTool;
        bool ownsTool;
        int undoGroup = -1, control;
        Vector3Int previousCell;
        HeightPlanTile strokeTile;
        int strokeSize;
        string status = "Choose a height, then turn on the brush.";
        Vector2 scroll;
        GUIStyle labelStyle;
        readonly Vector3[] corners = new Vector3[4];
        static readonly Dictionary<int, HeightPlanTile> Tiles = new Dictionary<int, HeightPlanTile>();

        [MenuItem("Road of the Old King/Height Brush", priority = 21)]
        public static void Open() => GetWindow<LowlandsElevationWindow>("Height Brush");

        void OnEnable()
        {
            titleContent = new GUIContent("Height Brush");
            minSize = new Vector2(280, 350);
            SceneView.duringSceneGui += DuringSceneGUI;
            Undo.undoRedoPerformed += RepaintViews;
            FindPlan();
        }

        void OnDisable()
        {
            SetPainting(false);
            SceneView.duringSceneGui -= DuringSceneGUI;
            Undo.undoRedoPerformed -= RepaintViews;
            SceneView.RepaintAll();
        }

        void RepaintViews() { FindPlan(); Repaint(); SceneView.RepaintAll(); }

        void FindPlan()
        {
            var scene = SceneManager.GetActiveScene();
            if (plan != null && plan.gameObject.scene == scene)
            {
                if (markings == null)
                    markings = plan.transform.parent.GetComponentsInChildren<Tilemap>(true)
                        .FirstOrDefault(m => m.name == MarkingsName && m.CompareTag("EditorOnly"));
                return;
            }
            SetPainting(false);
            plan = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true))
                .FirstOrDefault(m => m.name == PlanName && m.CompareTag("EditorOnly"));
            markings = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true))
                .FirstOrDefault(m => m.name == MarkingsName && m.CompareTag("EditorOnly"));
        }

        static bool Editable(Tilemap map) => map != null && (map.name == PlanName || map.name == MarkingsName) && map.CompareTag("EditorOnly")
            && !EditorUtility.IsPersistent(map) && map.gameObject.scene == SceneManager.GetActiveScene()
            && map.layoutGrid != null && map.layoutGrid.cellLayout == GridLayout.CellLayout.Rectangle
            && Quaternion.Angle(map.transform.rotation, Quaternion.identity) < .001f
            && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling
            && PrefabStageUtility.GetCurrentPrefabStage() == null;

        void OnGUI()
        {
            FindPlan();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField(SceneManager.GetActiveScene().name + " · Height plan", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Paint the height you want each area to have. This is the reference for rebuilding terrain. Save it with your scene (Ctrl+S).", MessageType.None);
            if (plan == null)
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null))
                    if (GUILayout.Button("Start height plan from this scene", GUILayout.Height(32)))
                        try { plan = CreatePlan(); status = "Plan started. Unpainted cells have no height assigned."; }
                        catch (Exception e) { status = e.Message; }
            }
            using (new EditorGUI.DisabledScope(!Editable(plan)))
            {
                EditorGUILayout.Space();
                kind = (HeightPlanKind)GUILayout.Toolbar((int)kind, new[] { "Height", "Cliff face", "Transition" });
                EditorGUILayout.LabelField("Height", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                for (int h = -3; h <= 3; h++)
                {
                    var old = GUI.backgroundColor; GUI.backgroundColor = ColorFor(h);
                    if (GUILayout.Toggle(!erase && kind == HeightPlanKind.Height && height == h, Label(h), "Button", GUILayout.Height(30)))
                    { height = h; erase = false; kind = HeightPlanKind.Height; }
                    GUI.backgroundColor = old;
                }
                EditorGUILayout.EndHorizontal();
                height = EditorGUILayout.IntSlider("Height level", height, -3, 5);
                erase = GUILayout.Toolbar(erase ? 1 : 0, new[] { "Paint", "Erase" }) == 1;
                brushSize = EditorGUILayout.IntSlider("Brush size (cells)", brushSize, 1, 16);
                EditorGUILayout.Space();
                bool enabled = GUILayout.Toggle(painting, painting ? "Brush ON — paint in Scene view" : "Turn brush ON", "Button", GUILayout.Height(34));
                if (enabled != painting) SetPainting(enabled);
                EditorGUILayout.Space();
                overlay = EditorGUILayout.Toggle("Show height overlay", overlay);
                using (new EditorGUI.DisabledScope(!overlay))
                {
                    opacity = EditorGUILayout.Slider("Overlay opacity", opacity, .1f, .9f);
                    labels = EditorGUILayout.Toggle("Show height numbers", labels);
                }
            }
            if (!Editable(plan)) SetPainting(false);
            var view = SceneView.lastActiveSceneView;
            if (view != null && !view.in2DMode && GUILayout.Button("Switch Scene view to 2D")) { view.in2DMode = true; view.Repaint(); }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Drag left mouse to paint. Shift-drag erases the selected layer. [ / ] changes brush size. Escape stops painting. Ctrl+Z undoes a whole stroke. Middle mouse / Alt still navigate.\n\n0 = base ground; each number is one tile of elevation. Blank = undecided. Paint land under water at its ground height.\n\nCliff face (C) and Transition (T) mark their intended tile footprints without replacing the height underneath. These are planning marks, not collision or built stairs.", MessageType.None);
            EditorGUILayout.LabelField(status, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
            if (GUI.changed) RepaintViews();
        }

        public static string Label(int value) => value > 0 ? "+" + value : value.ToString();
        public static Color ColorFor(int value)
        {
            if (value < 0) return Color.HSVToRGB(Mathf.Clamp(.54f - value * .055f, .54f, .76f), .65f, .87f);
            if (value == 0) return new Color(.34f, .74f, .45f);
            return Color.HSVToRGB(Mathf.Max(0, .16f - (value - 1) * .045f), .72f, .96f);
        }

        internal static HeightPlanTile TileFor(int value)
        {
            if (Tiles.TryGetValue(value, out var cached) && cached != null) return cached;
            string path = AssetRoot + "/Height_" + value + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<HeightPlanTile>(path);
            if (tile == null)
            {
                Directory.CreateDirectory(AssetRoot);
                tile = CreateInstance<HeightPlanTile>(); tile.height = value;
                tile.color = ColorFor(value); tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, path);
            }
            return Tiles[value] = tile;
        }

        internal static HeightPlanTile MarkerFor(HeightPlanKind value)
        {
            if (value == HeightPlanKind.Height) throw new ArgumentException("Choose a face or transition marker.");
            string path = AssetRoot + "/" + value + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<HeightPlanTile>(path);
            if (tile == null)
            {
                Directory.CreateDirectory(AssetRoot);
                tile = CreateInstance<HeightPlanTile>();
                tile.kind = value; tile.colliderType = Tile.ColliderType.None;
                tile.color = MarkerColor(value);
                AssetDatabase.CreateAsset(tile, path);
            }
            return tile;
        }

        static Color MarkerColor(HeightPlanKind value) => value == HeightPlanKind.CliffFace
            ? new Color(.72f, .72f, .78f) : new Color(.15f, .9f, 1f);

        internal static Tilemap CreateMarkings(Tilemap heightPlan)
        {
            if (!Editable(heightPlan) || heightPlan.name != PlanName)
                throw new InvalidOperationException("Choose an editable height plan.");
            var existing = heightPlan.transform.parent.GetComponentsInChildren<Tilemap>(true)
                .FirstOrDefault(m => m.name == MarkingsName && m.CompareTag("EditorOnly"));
            if (existing != null) return existing;
            var go = new GameObject(MarkingsName, typeof(Tilemap), typeof(TilemapRenderer));
            SceneManager.MoveGameObjectToScene(go, heightPlan.gameObject.scene);
            go.transform.SetParent(heightPlan.transform.parent, false);
            go.transform.localPosition = heightPlan.transform.localPosition;
            go.transform.localRotation = heightPlan.transform.localRotation;
            go.transform.localScale = heightPlan.transform.localScale;
            go.tag = "EditorOnly"; go.GetComponent<TilemapRenderer>().enabled = false;
            var map = go.GetComponent<Tilemap>(); map.tileAnchor = heightPlan.tileAnchor;
            Undo.RegisterCreatedObjectUndo(go, "Mark height plan");
            return map;
        }

        public static Tilemap CreatePlan()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Open your level in stopped Scene view.");
            var scene = SceneManager.GetActiveScene();
            var maps = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true)).ToArray();
            var existing = maps.FirstOrDefault(m => m.name == PlanName && m.CompareTag("EditorOnly"));
            if (existing != null) return existing; // Never reseed over the user's plan.
            var source = maps.Where(m => m.name == LowlandsElevationAuthoring.ElevationName)
                .OrderByDescending(m => m.GetUsedTilesCount()).FirstOrDefault();
            var grid = source != null ? source.layoutGrid : scene.GetRootGameObjects().Select(g => g.GetComponent<Grid>()).FirstOrDefault(g => g != null);
            if (grid == null) throw new InvalidOperationException("This scene needs a level Grid.");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Start height plan");
            var go = new GameObject(PlanName, typeof(Tilemap), typeof(TilemapRenderer));
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(source != null ? source.transform.parent : grid.transform, false);
            go.tag = "EditorOnly"; go.GetComponent<TilemapRenderer>().enabled = false;
            var map = go.GetComponent<Tilemap>();
            if (source != null)
            {
                go.transform.localPosition = source.transform.localPosition;
                go.transform.localRotation = source.transform.localRotation;
                go.transform.localScale = source.transform.localScale;
                map.tileAnchor = source.tileAnchor;
                var tiers = new[] { "Farm", "Ridge", "Enclosure" };
                var tops = tiers.Select(t => maps.FirstOrDefault(m => m.name == "Terrain - " + t + " tops")).ToArray();
                var faces = tiers.Select(t => maps.FirstOrDefault(m => m.name == "Terrain - " + t + " faces")).ToArray();
                var cells = new List<Vector3Int>(); var tiles = new List<TileBase>();
                foreach (var c in source.cellBounds.allPositionsWithin)
                {
                    var kind = LowlandsElevationAuthoring.ReadClassification(source, c);
                    if (kind == ElevationClass.Void || kind == ElevationClass.Unclassified) continue;
                    int h;
                    if (!LowlandsElevationAuthoring.TryHeight(kind, out h))
                    {
                        h = int.MinValue; int faceHeight = int.MinValue;
                        var point = source.GetCellCenterWorld(c);
                        for (int i = 0; i < tiers.Length; i++)
                        {
                            if (tops[i] != null && tops[i].HasTile(tops[i].WorldToCell(point))) h = i;
                            if (faces[i] != null && faces[i].HasTile(faces[i].WorldToCell(point))) faceHeight = i;
                        }
                        if (h == int.MinValue || faceHeight >= h) continue;
                    }
                    cells.Add(c); tiles.Add(TileFor(h));
                }
                map.SetTiles(cells.ToArray(), tiles.ToArray());
            }
            Undo.RegisterCreatedObjectUndo(go, "Start height plan"); Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            return map;
        }

        // Interpolate every crossed cell so fast drags cannot leave holes.
        internal static void PaintSegment(Tilemap map, Vector3Int from, Vector3Int to, int size, HeightPlanTile tile)
        {
            if (!Editable(map)) throw new InvalidOperationException("Choose an editable scene height plan.");
            if (tile != null && (map.name == PlanName) != (tile.kind == HeightPlanKind.Height))
                throw new InvalidOperationException("Height values and footprint markings belong on their own layers.");
            size = Mathf.Clamp(size, 1, 16);
            int dx = Mathf.Abs(to.x - from.x), dy = Mathf.Abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1, sy = from.y < to.y ? 1 : -1, error = dx - dy;
            while (true)
            {
                int start = -(size - 1) / 2;
                for (int y = start; y < start + size; y++) for (int x = start; x < start + size; x++)
                    map.SetTile(new Vector3Int(from.x + x, from.y + y, 0), tile);
                if (from.x == to.x && from.y == to.y) break;
                int twice = error * 2;
                if (twice > -dy) { error -= dy; from.x += sx; }
                if (twice < dx) { error += dx; from.y += sy; }
            }
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        }

        void FinishStroke()
        {
            if (undoGroup < 0) return;
            Undo.CollapseUndoOperations(undoGroup); undoGroup = -1;
            if (GUIUtility.hotControl == control) GUIUtility.hotControl = 0;
        }

        void SetPainting(bool value)
        {
            FinishStroke(); painting = value;
            if (value)
            {
                if (!ownsTool) previousTool = Tools.current;
                ownsTool = true; Tools.current = Tool.None; overlay = true;
            }
            else if (ownsTool)
            {
                if (Tools.current == Tool.None) Tools.current = previousTool;
                ownsTool = false;
            }
        }

        void DuringSceneGUI(SceneView view)
        {
            FindPlan();
            if (!Editable(plan) || !view.in2DMode) { FinishStroke(); return; }
            var e = Event.current;
            if (e.type == EventType.Repaint && overlay) DrawOverlay(view);
            if (!painting) return;
            control = GUIUtility.GetControlID("HeightBrush".GetHashCode(), FocusType.Passive);
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape) { SetPainting(false); e.Use(); RepaintViews(); return; }
                if (e.keyCode == KeyCode.LeftBracket || e.keyCode == KeyCode.RightBracket)
                { brushSize = Mathf.Clamp(brushSize + (e.keyCode == KeyCode.LeftBracket ? -1 : 1), 1, 16); e.Use(); RepaintViews(); }
            }
            if (e.rawType == EventType.MouseUp && undoGroup >= 0 && e.button == 0) { FinishStroke(); e.Use(); return; }
            if (e.type == EventType.MouseLeaveWindow || e.type == EventType.Ignore) { FinishStroke(); return; }
            if (e.alt || e.control || e.command || Tools.viewToolActive) { FinishStroke(); return; }
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(control);
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!new Plane(Vector3.forward, plan.transform.position).Raycast(ray, out float distance)) return;
            var cell = plan.WorldToCell(ray.GetPoint(distance)); cell.z = 0;
            if (e.type == EventType.Repaint)
            {
                int start = -(brushSize - 1) / 2;
                RectCorners(cell + new Vector3Int(start, start, 0), brushSize);
                var color = erase || e.shift ? Color.white : kind == HeightPlanKind.Height ? ColorFor(height) : MarkerColor(kind);
                var fill = color; fill.a = .18f;
                var old = Handles.zTest; Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                Handles.DrawSolidRectangleWithOutline(corners, fill, color); Handles.zTest = old;
            }
            if (e.button == 0 && e.type == EventType.MouseDown && HandleUtility.nearestControl == control)
            {
                strokeTile = erase || e.shift ? null : kind == HeightPlanKind.Height ? TileFor(height) : MarkerFor(kind);
                strokeSize = brushSize;
                Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Paint height plan");
                strokeMap = kind == HeightPlanKind.Height ? plan : (markings = CreateMarkings(plan));
                Undo.RegisterCompleteObjectUndo(strokeMap, "Paint height plan");
                GUIUtility.hotControl = control; previousCell = cell;
                PaintSegment(strokeMap, cell, cell, strokeSize, strokeTile); e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && undoGroup >= 0 && GUIUtility.hotControl == control)
            { PaintSegment(strokeMap, previousCell, cell, strokeSize, strokeTile); previousCell = cell; e.Use(); }
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.Used) view.Repaint();
        }

        void RectCorners(Vector3Int cell, int size)
        {
            corners[0] = plan.CellToWorld(cell);
            corners[1] = plan.CellToWorld(cell + new Vector3Int(size, 0, 0));
            corners[2] = plan.CellToWorld(cell + new Vector3Int(size, size, 0));
            corners[3] = plan.CellToWorld(cell + new Vector3Int(0, size, 0));
        }

        void DrawOverlay(SceneView view)
        {
            if (labelStyle == null) labelStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            var camera = view.camera;
            float depth = Vector3.Dot(plan.transform.position - camera.transform.position, camera.transform.forward);
            var a = plan.WorldToCell(camera.ViewportToWorldPoint(new Vector3(0, 0, depth)));
            var b = plan.WorldToCell(camera.ViewportToWorldPoint(new Vector3(1, 1, depth)));
            var bounds = plan.cellBounds;
            if (markings != null && markings.GetUsedTilesCount() > 0)
            {
                bounds.SetMinMax(Vector3Int.Min(bounds.min, markings.cellBounds.min), Vector3Int.Max(bounds.max, markings.cellBounds.max));
            }
            int x0 = Math.Max(Math.Min(a.x, b.x) - 1, bounds.xMin), x1 = Math.Min(Math.Max(a.x, b.x) + 2, bounds.xMax);
            int y0 = Math.Max(Math.Min(a.y, b.y) - 1, bounds.yMin), y1 = Math.Min(Math.Max(a.y, b.y) + 2, bounds.yMax);
            if ((long)(x1 - x0) * (y1 - y0) > 100000) return;
            bool showNumbers = labels && Vector2.Distance(HandleUtility.WorldToGUIPoint(plan.CellToWorld(Vector3Int.zero)), HandleUtility.WorldToGUIPoint(plan.CellToWorld(Vector3Int.right))) > 24;
            var old = Handles.zTest; Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            var oldColor = Handles.color;
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
            {
                var c = new Vector3Int(x, y, 0); var tile = plan.GetTile<HeightPlanTile>(c);
                RectCorners(c, 1);
                var marker = markings != null ? markings.GetTile<HeightPlanTile>(c) : null;
                if (tile != null)
                {
                    var color = ColorFor(tile.height); color.a = opacity;
                    Handles.DrawSolidRectangleWithOutline(corners, color, new Color(0, 0, 0, .12f));
                    if (showNumbers && marker == null) Handles.Label((corners[0] + corners[2]) * .5f, Label(tile.height), labelStyle);
                }
                if (marker != null)
                {
                    var color = MarkerColor(marker.kind); var fill = color; fill.a = opacity * .5f;
                    Handles.DrawSolidRectangleWithOutline(corners, fill, color);
                    Handles.color = color;
                    if (marker.kind == HeightPlanKind.CliffFace) Handles.DrawLine(corners[0], corners[2]);
                    else
                        for (int i = 1; i < 4; i++) Handles.DrawLine(Vector3.Lerp(corners[0], corners[3], i / 4f), Vector3.Lerp(corners[1], corners[2], i / 4f));
                    if (showNumbers) Handles.Label((corners[0] + corners[2]) * .5f, marker.kind == HeightPlanKind.CliffFace ? "C" : "T", labelStyle);
                }
            }
            Handles.zTest = old;
            Handles.color = oldColor;
        }
    }
}
