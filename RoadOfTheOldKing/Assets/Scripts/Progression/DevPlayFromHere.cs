#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RoadOfTheOldKing.Progression
{
    // The Scene view's "Play from here": one Play session on a throwaway save, starting at a chosen spot. The real save
    // is only read, to copy it. SessionState survives the domain reload on entering Play and ends with the editor.
    public static class DevPlayFromHere
    {
        public const string SaveKey = "RoadOfTheOldKing.PlayFromHere";
        private const string ActiveState = "PlayFromHere.Active", PlacedState = "PlayFromHere.Placed", SpotState = "PlayFromHere.Spot";

        public static bool IsActive => SessionState.GetBool(ActiveState, false);

        public static void Arm(Vector2 spot)
        {
            SessionState.SetBool(ActiveState, true);
            SessionState.SetBool(PlacedState, false);
            SessionState.SetVector3(SpotState, spot);
        }

        public static void Disarm()
        {
            SessionState.EraseBool(ActiveState);
            SessionState.EraseBool(PlacedState);
            SessionState.EraseVector3(SpotState);
        }

        // The spot applies to the first scene only; a later scene exit arrives as normal.
        public static bool TakeSpot(out Vector2 spot)
        {
            spot = Vector2.zero;
            if (!IsActive || SessionState.GetBool(PlacedState, false)) return false;
            SessionState.SetBool(PlacedState, true);
            spot = SessionState.GetVector3(SpotState, Vector3.zero);
            return true;
        }
    }
}
#endif
