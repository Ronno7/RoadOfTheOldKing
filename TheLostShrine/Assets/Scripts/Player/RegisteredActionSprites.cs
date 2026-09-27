using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace TheLostShrine.Player
{
    // Full-canvas authored layers. All sprites share scale and foot origin; no grip tweening.
    [MovedFrom(true, "TheLostShrine.Player", "Assembly-CSharp", "RegisteredThrowSprites")]
    [CreateAssetMenu(menuName = "Road of the Old King/Registered Action Sprites")]
    public sealed class RegisteredActionSprites : ScriptableObject
    {
        [Serializable]
        public sealed class View
        {
            public string name;
            public Vector2 direction;
            [Tooltip("Complete prop balance point relative to the body foot pivot; visual only.")]
            public Vector2 freePropOffset;
            public float freePropScale = 1f;
            public int spinStartCel;
            public bool spinFlipX;
            public Sprite[] body = Array.Empty<Sprite>();
            [Tooltip("Optional whole-body poses when no weapon is held; shares the same cel phase and pivot.")]
            public Sprite[] unarmedBody = Array.Empty<Sprite>();
            public Sprite[] weapon = Array.Empty<Sprite>();
        }

        public View[] views = Array.Empty<View>();
        public int celCount = 8;
        [Tooltip("Authored exposures in seconds. Empty when a gameplay clock supplies cel indices (throw).")]
        public float[] exposures = Array.Empty<float>();
        [Tooltip("First contact and first recovery cel. Both -1 for actions without a melee contact window.")]
        public int contactCel = -1, recoveryCel = -1;
        [Tooltip("First held-weapon cel after confirmed arrival. -1 for actions without a catch.")]
        public int possessionCel = -1;
        public const int CanvasSize = 640;
        public const int PixelsPerUnit = 128;
        public static Vector2 GroundPivot => new Vector2(0.5f, 0.15f);

        public float Duration => CelStart(exposures.Length);
        public float CelStart(int cel)
        {
            float time = 0f;
            for (int i = 0; i < Mathf.Clamp(cel, 0, exposures.Length); i++) time += exposures[i];
            return time;
        }

        public int SampleAtSeconds(float seconds)
        {
            if (exposures == null || exposures.Length == 0) return -1;
            if (float.IsNaN(seconds) || seconds <= 0f) return 0;
            for (int i = 0; i < exposures.Length - 1; i++)
            {
                if (seconds + 0.000001f < exposures[i]) return i;
                seconds -= exposures[i];
            }
            return exposures.Length - 1;
        }

        // Samples the weapon's existing clock. No new timer, damage marker or balance change.
        // Preserve relative exposures within anticipation, contact and recovery separately.
        public int SampleMeleeCel(float progress, float contactStart, float contactEnd, bool holdContact = false)
        {
            if (contactCel <= 0 || recoveryCel <= contactCel || recoveryCel >= celCount ||
                exposures == null || exposures.Length != celCount ||
                !(contactStart > 0f && contactEnd > contactStart && contactEnd < 1f))
                throw new ArgumentException("A melee sequence requires valid contact/recovery cels and a gameplay contact window.");
            // A long simulation step can cross contact before hit pause is reported.
            // Hold the resolved contact drawing, even if progress already reached recovery.
            if (holdContact) return contactCel;
            if (float.IsNaN(progress)) return 0;
            progress = Mathf.Clamp01(progress);
            float start = CelStart(contactCel), end = CelStart(recoveryCel);
            float authoredTime = progress < contactStart ? start * progress / contactStart
                : progress < contactEnd ? Mathf.Lerp(start, end, (progress-contactStart)/(contactEnd-contactStart))
                : Mathf.Lerp(end, Duration, (progress-contactEnd)/(1f-contactEnd));
            return SampleAtSeconds(authoredTime);
        }

        public bool IsRegistered(out string error)
        {
            if (views == null || views.Length == 0)
            { error = "No views assigned."; return false; }
            if (celCount <= 0 || exposures == null || (exposures.Length != 0 && exposures.Length != celCount))
            { error = "Exposure count must match cels, or be empty for an external action clock."; return false; }
            foreach (float exposure in exposures)
                if (float.IsNaN(exposure) || float.IsInfinity(exposure) || exposure <= 0f)
                { error = "Cel exposures must be finite and positive."; return false; }
            if (possessionCel != -1 && (possessionCel <= 0 || possessionCel >= celCount || exposures.Length != celCount))
            { error = "Catch possession requires a preceding reach cel and authored exposures."; return false; }
            if ((contactCel != -1 || recoveryCel != -1) &&
                (exposures.Length != celCount || contactCel <= 0 || recoveryCel <= contactCel || recoveryCel >= celCount))
            { error = "Invalid contact/recovery cel markers."; return false; }
            foreach (var view in views)
            {
                if (view == null || view.body == null || view.weapon == null ||
                    view.body.Length != celCount || view.weapon.Length != celCount)
                { error = "Each view needs synchronized cels on every layer."; return false; }
                if (view.unarmedBody != null && view.unarmedBody.Length != 0 && view.unarmedBody.Length != celCount)
                { error = "An unarmed variant must cover the complete cel track."; return false; }
                foreach (var layer in new[] { view.body, view.weapon, view.unarmedBody ?? Array.Empty<Sprite>() })
                    foreach (var sprite in layer)
                        if (sprite == null || sprite.rect.size != Vector2.one * CanvasSize ||
                            sprite.pixelsPerUnit != PixelsPerUnit ||
                            (sprite.pivot - GroundPivot * CanvasSize).sqrMagnitude > 0.01f)
                        { error = "Every cel must be 640x640 at 128 PPU with pivot (320,96)."; return false; }
            }
            error = null;
            return true;
        }
    }
}
