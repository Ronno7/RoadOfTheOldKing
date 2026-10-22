using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace RoadOfTheOldKing.Editor
{
    // Borderless Game view covering the main monitor; the same shortcut closes it.
    public static class FullscreenGameView
    {
        private static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        private static readonly PropertyInfo ShowToolbar =
            GameViewType?.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        [MenuItem("Road of the Old King/Fullscreen Game View %#&f")]
        public static void Toggle()
        {
            if (GameViewType == null)
            {
                Debug.LogError("Fullscreen Game View: UnityEditor.GameView was not found in this Unity version.");
                return;
            }

            // Identify our popups by their hidden toolbar: Windows clamps the size to the work area, and
            // domain reloads clear static fields while the popup survives. Close every copy found.
            bool closed = false;
            foreach (var view in Resources.FindObjectsOfTypeAll(GameViewType))
            {
                if (view is EditorWindow window && !window.docked && ShowToolbar != null && !(bool)ShowToolbar.GetValue(window))
                {
                    window.Close();
                    closed = true;
                }
            }
            if (closed) return;

            var game = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);
            ShowToolbar?.SetValue(game, false);
            game.ShowPopup();
            game.position = MainScreenRect();
            game.Focus();
            CoverTaskbar();
        }

        private static Rect MainScreenRect()
        {
            // Editor window positions are in points, so undo Windows display scaling.
            float scale = EditorGUIUtility.pixelsPerPoint;
            Resolution desktop = Screen.currentResolution;
            return new Rect(0f, 0f, desktop.width / scale, desktop.height / scale);
        }

#if UNITY_EDITOR_WIN
        // Unity fits popups to the work area, leaving the taskbar strip uncovered. Resizing the native
        // window to the whole monitor bypasses that, and Windows then treats it as fullscreen.
        private static void CoverTaskbar()
        {
            IntPtr hwnd = GetActiveWindow();
            if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out NativeRect rect)) return;

            int width = GetSystemMetrics(0), height = GetSystemMetrics(1);
            // Only touch the window just shown at the monitor origin, never the main editor window.
            if (rect.Left != 0 || rect.Top != 0 || rect.Right - rect.Left != width) return;
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, width, height, 0x0040 /* SWP_SHOWWINDOW */);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
#else
        private static void CoverTaskbar() { }
#endif
    }
}
