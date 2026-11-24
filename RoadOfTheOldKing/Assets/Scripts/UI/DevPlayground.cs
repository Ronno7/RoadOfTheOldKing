#if UNITY_EDITOR || DEVELOPMENT_BUILD || ROTOK_DEV_TOOLS
using System.Collections;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    // The editor-only playground (Scenes/Development/Playground, outside the build; user, 18 Nov: "a scene to just play
    // around in, no real detail but a playground for development testing, player movement testing, combat testing").
    // Its GameSession keeps its own save slot; on arrival the hero gets the axe, Recall and the rope kit, so everything
    // can be tried at once. Resting at its fire brings the enemies back.
    public sealed class DevPlayground : MonoBehaviour
    {
        private IEnumerator Start()
        {
            while (DevRuntimeCommands.Instance == null || GameSession.Instance == null || GameSession.Instance.IsLoading ||
                   !DevRuntimeCommands.Instance.Alive) yield return null;
            var commands = DevRuntimeCommands.Instance;
            var progress = GameSession.Instance.Progress;
            if (!progress.recallUnlocked) commands.UnlockRecall(); // equips the axe first
            else if (!progress.hasAxe) commands.GiveAxe();
            if (!progress.OwnsTool(WorldTool.RopeKit)) commands.GiveTool(WorldTool.RopeKit);
        }
    }
}
#endif
