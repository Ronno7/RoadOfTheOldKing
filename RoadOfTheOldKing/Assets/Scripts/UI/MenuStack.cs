using System.Collections.Generic;
using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    // A modal menu that receives menu input while it is on top of the stack.
    public interface IModalMenu
    {
        // Escape: step back a page, or close.
        void Back();
        void Navigate(int direction);
        void Submit();
    }

    // The single owner of open modal menus. Menu input (Escape, arrows, Enter) goes to the top menu;
    // the pause menu opens only when nothing else is open. Menus push themselves when shown and
    // remove themselves when hidden.
    public static class MenuStack
    {
        private static readonly List<IModalMenu> stack = new List<IModalMenu>();

        public static IModalMenu Top => stack.Count > 0 ? stack[stack.Count - 1] : null;
        public static bool IsAnyOpen => stack.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => stack.Clear();

        public static void Push(IModalMenu menu)
        {
            if (menu == null) return;
            stack.Remove(menu);
            stack.Add(menu);
        }

        public static void Remove(IModalMenu menu) => stack.Remove(menu);
        public static bool Contains(IModalMenu menu) => stack.Contains(menu);
    }
}
