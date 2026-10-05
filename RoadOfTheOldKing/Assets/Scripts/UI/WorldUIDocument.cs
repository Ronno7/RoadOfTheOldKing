using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // A small world-space UI Toolkit document: one UXML tree placed in the scene and sorted like a
    // sprite by the 2D Renderer. It needs an explicit sorting layer: on Default it draws beneath the
    // ground. Owners create it as a child, move it with Place and toggle it with Show; it only presents.
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument))]
    public sealed class WorldUIDocument : MonoBehaviour
    {
        public const string SortingLayer = "Player";

        private UIDocument document;
        private UIRenderer uiRenderer;
        private int sortingOrder;
        private bool visible = true;

        public VisualElement Root => document.rootVisualElement;
        public T Q<T>(string name) where T : VisualElement => Root.Q<T>(name);

        public static WorldUIDocument Create(string name, Transform owner, PanelSettings panel, VisualTreeAsset layout, int sortingOrder, Pivot pivot)
        {
            var child = new GameObject(name) { layer = owner.gameObject.layer };
            child.SetActive(false);
            child.transform.SetParent(owner, false);
            var document = child.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = layout;
            // Size to the content and pivot on its bounds, so Place puts e.g. a prompt's bottom centre on the point.
            document.worldSpaceSizeMode = UIDocument.WorldSpaceSizeMode.Dynamic;
            document.pivotReferenceSize = PivotReferenceSize.BoundingBox;
            document.pivot = pivot;
            var world = child.AddComponent<WorldUIDocument>();
            world.document = document;
            world.sortingOrder = sortingOrder;
            child.SetActive(true);
            world.ApplySorting();
            return world;
        }

        // Puts the pivot on a world point, upright and unscaled whatever the owner does.
        public void Place(Vector3 world) => transform.SetPositionAndRotation(world, Quaternion.identity);

        // Hidden, not display:none: an empty layout leaves the document without its world transform,
        // and the first frame after showing again drew at 1 world unit per panel pixel.
        public void Show(bool show)
        {
            if (visible == show) return;
            visible = show;
            ApplyVisibility();
        }

        // A disabled document drops its tree (Show may run during teardown); re-enabling clones a new
        // one, so owners that cache queried elements assume their GameObject is never toggled.
        private void ApplyVisibility()
        {
            var root = document != null ? document.rootVisualElement : null;
            if (root != null) root.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        private void OnEnable() { if (document != null) ApplyVisibility(); }

        // The document adds its renderer itself, possibly after Create returns.
        private void LateUpdate() { if (uiRenderer == null) ApplySorting(); }

        private void ApplySorting()
        {
            uiRenderer = GetComponent<UIRenderer>();
            if (uiRenderer == null) return;
            uiRenderer.sortingLayerName = SortingLayer;
            uiRenderer.sortingOrder = sortingOrder;
        }
    }
}
