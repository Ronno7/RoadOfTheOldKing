using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // The attack warning, drawn on the ground (see EnemyAttackGround.shader), each shape following the attack until it locks.
    // Ghost Steps: after-images of the enemy along a charge's path and a crescent for a swing. Chalk: a lane with arrows and a
    // wedge. FeedbackSettings picks between them. A heavy enemy always draws its own ring or wedge, with cracks.
    // It reads the brain and drives nothing.
    [DisallowMultipleComponent]
    public sealed class EnemyAttackGround : MonoBehaviour
    {
        // On the ground, over every painted detail, under props and bodies.
        private const string GroundLayer = "Ground";
        private const int GroundOrder = 90;
        // How far out the streaks of a draw run; the pull itself reaches anything it can see.
        private const float DrawStreakReach = 18f;
        // An after-image about this far apart, however long the charge, and no more than these.
        private const float GhostSpacing = 3f;
        private const int MaxGhosts = 3, TrailGhosts = 2;
        // The trail behind a charger, in world units and how solid.
        private const float TrailGap = 1f, TrailAlpha = .5f;
        private static readonly Color Pastel = new Color32(236, 222, 190, 255), PastelHit = new Color32(216, 134, 138, 255),
            PastelFlash = new Color32(250, 243, 228, 255), PastelCut = new Color32(112, 94, 104, 255),
            GhostPale = new Color32(214, 228, 244, 255), GhostHot = new Color32(250, 150, 110, 255);
        private static readonly int CenterId = Shader.PropertyToID("_Center"), RadiusId = Shader.PropertyToID("_Radius"),
            WindupId = Shader.PropertyToID("_Windup"), ImpactId = Shader.PropertyToID("_Impact"), DrawId = Shader.PropertyToID("_Draw"),
            ArcId = Shader.PropertyToID("_Arc"), FacingId = Shader.PropertyToID("_Facing"), LockedId = Shader.PropertyToID("_Locked"),
            LaneId = Shader.PropertyToID("_Lane"), DirectionId = Shader.PropertyToID("_Direction"), SweepId = Shader.PropertyToID("_Sweep"),
            HeavyId = Shader.PropertyToID("_Heavy"), GhostId = Shader.PropertyToID("_Ghost"), WarnId = Shader.PropertyToID("_Warn"),
            HotId = Shader.PropertyToID("_Hot"), FlashId = Shader.PropertyToID("_Flash"), CrackId = Shader.PropertyToID("_Crack");
        private static Material ghostMaterial;

        [SerializeField] private Shader shader;
        [SerializeField] private Shader ghostShader;
        [Tooltip("How long the marked ground glows and fades after the blow lands. A lane waits for the charge to finish first.")]
        [SerializeField, Min(.1f)] private float afterglow = .6f;
        [Tooltip("The full drawing for a boss: a thick rim, sun wheel notches and cracks.")]
        [SerializeField] private bool heavy;

        private IEnemyAttackShape enemy;
        private EnemyAnimationSet animations;
        private Transform quad;
        private MeshRenderer view;
        private Material material;
        private SpriteRenderer[] ghosts;
        private Sprite ghostPose;
        private Vector2 laneOrigin, laneDirection;
        private float sinceImpact = -1f, strikeSeconds, fadeSeconds, laneLength, laneTravel;

        private void Awake()
        {
            enemy = GetComponent<IEnemyAttackShape>();
            if (shader == null || enemy == null) { enabled = false; return; }
            var melee = GetComponent<MeleeSpriteView>();
            var predator = GetComponent<PredatorSpriteView>();
            animations = melee != null ? melee.Animations : predator != null ? predator.Animations : null;
            material = new Material(shader);
            material.SetFloat(HeavyId, heavy ? 1f : 0f);
            if (!heavy)
            {
                material.SetColor(WarnId, Pastel);
                material.SetColor(HotId, PastelHit);
                material.SetColor(FlashId, PastelFlash);
                material.SetColor(CrackId, PastelCut);
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Attack ground";
            Destroy(go.GetComponent<Collider>());
            quad = go.transform;
            // Not parented: the ground stays marked while the body moves on.
            view = go.GetComponent<MeshRenderer>();
            view.sharedMaterial = material;
            view.sortingLayerName = GroundLayer;
            view.sortingOrder = GroundOrder;
            view.enabled = false;
        }

        private void OnDisable()
        {
            sinceImpact = -1f;
            if (view != null) view.enabled = false;
            HideGhosts(0);
        }

        private void OnDestroy()
        {
            if (quad != null) Destroy(quad.gameObject);
            if (material != null) Destroy(material);
            if (ghosts != null)
                foreach (var ghost in ghosts)
                    if (ghost != null) Destroy(ghost.gameObject);
        }

        private void LateUpdate()
        {
            bool windup = enemy.IsWindingUp;
            if (windup)
            {
                sinceImpact = -1f;
                Place(enemy.AttackOrigin);
            }
            else if (enemy.IsStriking && sinceImpact < 0f)
            {
                sinceImpact = 0f;
                strikeSeconds = Mathf.Max(.01f, enemy.StrikeSeconds);
                fadeSeconds = enemy.DrawnLaneLength > 0f ? strikeSeconds + afterglow : afterglow;
                Place(enemy.AttackOrigin);
            }
            else if (sinceImpact >= 0f)
            {
                sinceImpact += Time.deltaTime;
                if (sinceImpact >= fadeSeconds) sinceImpact = -1f;
            }
            bool showing = windup || sinceImpact >= 0f;
            view.enabled = showing;
            if (!showing) { HideGhosts(0); return; }
            bool ghostStyle = !heavy && FeedbackSettings.AttackWarnings == AttackWarningStyle.GhostSteps;
            float windupProgress = windup ? enemy.WindupProgress : 1f;
            float sweep = sinceImpact >= 0f ? Mathf.Clamp01(sinceImpact / strikeSeconds) : 0f;
            material.SetFloat(GhostId, ghostStyle ? 1f : 0f);
            material.SetFloat(DrawId, enemy.IsDrawing ? DrawStreakReach : 0f);
            material.SetFloat(LockedId, enemy.IsAttackLocked ? 1f : 0f);
            material.SetFloat(WindupId, windupProgress);
            // Slightly past zero the moment it lands, since zero means not yet.
            material.SetFloat(ImpactId, sinceImpact >= 0f ? Mathf.Max(.001f, sinceImpact / fadeSeconds) : 0f);
            material.SetFloat(SweepId, sweep);
            if (ghostStyle && laneLength > 0f) DrawGhosts(windupProgress, sweep);
            else HideGhosts(0);
        }

        private void Place(Vector2 origin)
        {
            Vector2 direction = enemy.AttackDirection;
            laneLength = enemy.DrawnLaneLength;
            material.SetVector(CenterId, origin);
            material.SetVector(DirectionId, direction);
            material.SetFloat(LaneId, laneLength);
            if (laneLength > 0f)
            {
                laneOrigin = origin;
                laneDirection = direction;
                laneTravel = enemy.DrawnLaneTravel;
                ghostPose = animations != null ? animations.StridePose(direction) : null;
                float width = enemy.DrawnLaneWidth, laneSize = laneLength + width + .25f;
                quad.position = origin + direction * (laneLength * .5f);
                quad.localScale = new Vector3(laneSize, laneSize, 1f);
                material.SetFloat(RadiusId, width * .5f);
                return;
            }
            float size = (enemy.IsDrawing ? DrawStreakReach : enemy.AttackReach) * 2f + .25f;
            quad.position = origin;
            quad.localScale = new Vector3(size, size, 1f);
            material.SetFloat(RadiusId, enemy.AttackReach);
            // In turns of a circle: half the arc either side of the facing, and where the facing points.
            material.SetFloat(ArcId, Mathf.Min(enemy.AttackArc, 360f) / 720f);
            material.SetFloat(FacingId, Mathf.Atan2(direction.y, direction.x) / (Mathf.PI * 2f) + .5f);
        }

        // Stops along the path appear as the windup reaches them, warm at the lock, and go as the charger passes; the last
        // is where it will stop, so it's the most solid.
        private void DrawGhosts(float windupProgress, float sweep)
        {
            if (ghostPose == null || ghostShader == null) { HideGhosts(0); return; }
            EnsureGhosts();
            bool landed = sinceImpact >= 0f;
            bool committed = landed || enemy.IsAttackLocked;
            Color tint = committed ? GhostHot : GhostPale;
            int stops = Mathf.Clamp(Mathf.RoundToInt(laneTravel / GhostSpacing), 1, MaxGhosts);
            int used = 0;
            for (int i = 1; i <= stops; i++)
            {
                float at = laneTravel * i / stops;
                bool shown = landed ? at >= sweep * laneTravel : at <= windupProgress * laneLength + .5f;
                if (!shown) continue;
                float alpha = landed ? .6f : committed ? .5f : .3f;
                if (i == stops && !landed) alpha += .2f;
                ShowGhost(used++, at, tint, alpha);
            }
            if (landed && sweep < 1f)
                for (int i = 1; i <= TrailGhosts; i++)
                {
                    float at = sweep * laneTravel - TrailGap * i;
                    if (at > 0f) ShowGhost(used++, at, material.GetColor(FlashId), TrailAlpha / i);
                }
            HideGhosts(used);
        }

        private void ShowGhost(int index, float at, Color tint, float alpha)
        {
            var ghost = ghosts[index];
            ghost.sprite = ghostPose;
            ghost.color = new Color(tint.r, tint.g, tint.b, alpha);
            ghost.transform.position = laneOrigin + laneDirection * at;
            ghost.enabled = true;
        }

        private void HideGhosts(int from)
        {
            if (ghosts == null) return;
            for (int i = from; i < ghosts.Length; i++) ghosts[i].enabled = false;
        }

        private void EnsureGhosts()
        {
            if (ghosts != null) return;
            if (ghostMaterial == null) ghostMaterial = new Material(ghostShader);
            ghosts = new SpriteRenderer[MaxGhosts + TrailGhosts];
            for (int i = 0; i < ghosts.Length; i++)
            {
                var ghost = new GameObject("Attack ghost").AddComponent<SpriteRenderer>();
                ghost.sharedMaterial = ghostMaterial;
                ghost.sortingLayerName = GroundLayer;
                ghost.sortingOrder = GroundOrder + 1;
                ghost.enabled = false;
                ghosts[i] = ghost;
            }
        }
    }
}
