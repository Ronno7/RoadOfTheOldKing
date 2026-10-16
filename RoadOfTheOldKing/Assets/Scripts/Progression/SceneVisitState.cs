using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoadOfTheOldKing.Progression
{
    // Short-lived clearing state, separate from permanent rewards and the save file.
    public interface ISceneVisitParticipant
    {
        object CaptureVisitState();
        void RestoreVisitState(object state);
    }

    internal sealed class SceneVisitState
    {
        private readonly Dictionary<string, Dictionary<string, object>> scenes = new();

        internal void Capture()
        {
            var state = new Dictionary<string, object>();
            foreach (var participant in GameSession.Participants<ISceneVisitParticipant>())
            {
                var component = (MonoBehaviour)participant;
                if (!component.isActiveAndEnabled) continue;
                var snapshot = participant.CaptureVisitState();
                if (snapshot != null) state.Add(Key(component), snapshot);
            }
            scenes[SceneManager.GetActiveScene().path] = state;
        }

        internal void Restore()
        {
            if (!scenes.TryGetValue(SceneManager.GetActiveScene().path, out var state)) return;
            foreach (var participant in GameSession.Participants<ISceneVisitParticipant>())
            {
                var component = (MonoBehaviour)participant;
                if (component.isActiveAndEnabled && state.TryGetValue(Key(component), out var snapshot))
                    participant.RestoreVisitState(snapshot);
            }
        }

        internal void Clear() => scenes.Clear();

        // Authored hierarchy identity is sufficient for one running session. Same-name ordinals
        // disambiguate prefab instances without depending on unrelated roots (the session moves
        // to DontDestroyOnLoad). No Unity object references or instance IDs survive in these keys.
        private static string Key(MonoBehaviour component)
        {
            var key = new StringBuilder(component.GetType().FullName);
            for (var t = component.transform; t != null; t = t.parent)
            {
                int ordinal = 0;
                if (t.parent != null)
                {
                    for (int i = 0; i < t.GetSiblingIndex(); i++)
                        if (t.parent.GetChild(i).name == t.name) ordinal++;
                }
                else
                {
                    foreach (var root in t.gameObject.scene.GetRootGameObjects())
                    {
                        if (root.transform == t) break;
                        if (root.name == t.name) ordinal++;
                    }
                }
                key.Append('|').Append(t.name.Length).Append(':').Append(t.name).Append('#').Append(ordinal);
            }
            return key.ToString();
        }
    }
}
