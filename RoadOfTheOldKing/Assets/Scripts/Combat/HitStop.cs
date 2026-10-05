using System.Collections;
using UnityEngine;

namespace RoadOfTheOldKing.Combat
{
    // Brief global freeze-frame for the heaviest impacts (killing blows). Ordinary hits pause only the
    // swing's own clock. Runs only from normal speed, so pause menus and slow-motion previews are never
    // overridden, and a persistent runner guarantees time is restored.
    public sealed class HitStop : MonoBehaviour
    {
        private static HitStop runner;
        private bool active;
        private System.IDisposable pause;

        public static void Freeze(float seconds)
        {
            if (seconds <= 0f || !Application.isPlaying || !Mathf.Approximately(Time.timeScale, 1f))
                return;
            if (runner == null)
            {
                var go = new GameObject("Hit Stop") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                runner = go.AddComponent<HitStop>();
            }
            if (!runner.active)
                runner.StartCoroutine(runner.Run(seconds));
        }

        private IEnumerator Run(float seconds)
        {
            active = true;
            pause = RoadOfTheOldKing.UI.SimulationPause.Acquire();
            yield return new WaitForSecondsRealtime(seconds);
            Restore();
        }

        private void Restore()
        {
            pause?.Dispose();
            pause = null;
            active = false;
        }

        private void OnDisable() => Restore();
    }
}
