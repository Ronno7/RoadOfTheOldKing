using UnityEngine;

namespace TheLostShrine.Player
{
    [CreateAssetMenu(menuName = "Road of the Old King/Registered Aim Gaits")]
    public sealed class RegisteredAimSprites : ScriptableObject
    {
        [Min(.1f), Tooltip("Actual ground distance for one complete left/right stride. Shared phase across gaits.")]
        public float strideLength = 1.5f;
        public RegisteredActionSprites forward, backward, left, right;

        public RegisteredActionSprites.View FindView(Vector2 facing, AimGait gait)
        {
            var action = gait == AimGait.Forward ? forward : gait == AimGait.Backward ? backward
                : gait == AimGait.Left ? left : right;
            if (action == null) return null;
            foreach (var view in action.views)
                if (Vector2.Dot(view.direction, facing) > .99f) return view;
            return null; // Missing gaits never reverse or mirror another drawing.
        }

        public bool IsRegistered(out string error)
        {
            if (float.IsNaN(strideLength) || float.IsInfinity(strideLength) || strideLength < .1f)
            { error = "Aim stride length must be finite and at least 0.1 units."; return false; }
            bool any = false;
            foreach (var action in new[] { forward, backward, left, right })
            {
                if (action == null) continue;
                any = true;
                if (!action.IsRegistered(out error)) return false;
                if (action.contactCel != -1 || action.possessionCel != -1)
                { error = "A gait cannot contain combat ownership markers."; return false; }
                for (int i = 0; i < action.views.Length; i++)
                {
                    var d = action.views[i].direction;
                    if (d != Vector2.right && d != Vector2.up && d != Vector2.left && d != Vector2.down)
                    { error = "Aim proof views must have explicit cardinal directions."; return false; }
                    for (int j = 0; j < i; j++)
                        if (action.views[j].direction == d)
                        { error = "A gait has duplicate facing views."; return false; }
                }
            }
            error = any ? null : "No aim gait drawings assigned.";
            return any;
        }
    }
}
