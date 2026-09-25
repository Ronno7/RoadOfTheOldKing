using UnityEngine;

namespace TheLostShrine.Combat
{
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable))]
    public sealed class WoodTargetFeedback : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Sprite chipSprite;
        [SerializeField] private AudioClip impactSound;
        private Damageable health;
        private AudioSource audioSource;
        private SpriteRenderer[] chips;
        private SpriteRenderer targetSprite;
        private Color restColor;
        private Vector3[] velocities;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private float age = 1f;
        private float direction;
        private float strength;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            restPosition = visual.localPosition;
            restRotation = visual.localRotation;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.3f;
            chips = new SpriteRenderer[5];
            velocities = new Vector3[chips.Length];
            targetSprite = visual.GetComponentInChildren<SpriteRenderer>();
            restColor = targetSprite.color;
            for (int i = 0; i < chips.Length; i++)
            {
                var chip = new GameObject("Wood chip");
                chip.transform.SetParent(transform, false);
                chips[i] = chip.AddComponent<SpriteRenderer>();
                chips[i].sprite = chipSprite;
                chips[i].color = i % 2 == 0 ? new Color(0.85f, 0.67f, 0.43f) : new Color(0.58f, 0.39f, 0.23f);
                chips[i].sortingLayerID = targetSprite.sortingLayerID;
                chips[i].sortingOrder = targetSprite.sortingOrder + 2;
                chip.transform.localScale = new Vector3(0.035f + i * 0.006f, 0.045f, 1f);
                chips[i].enabled = false;
            }
        }

        private void OnEnable() => health.HitReceived += OnHit;
        private void OnDisable()
        {
            health.HitReceived -= OnHit;
            if (visual != null)
            {
                visual.localPosition = restPosition;
                visual.localRotation = restRotation;
            }
            if (chips != null) foreach (var chip in chips) chip.enabled = false;
            if (targetSprite != null) targetSprite.color = restColor;
            age = 1f;
        }

        private void OnHit(CombatHit hit)
        {
            age = 0f;
            direction = hit.Direction.x >= 0f ? 1f : -1f;
            strength = hit.Damage > 10 ? 1.3f : 1f;
            if (impactSound != null)
            {
                audioSource.pitch = hit.Damage > 10 ? 0.92f : 1f;
                audioSource.PlayOneShot(impactSound);
            }
            Vector3 impact = (Vector3)(hit.ImpactPoint ?? (Vector2)transform.position) + Vector3.up * 0.65f;
            for (int i = 0; i < chips.Length; i++)
            {
                chips[i].enabled = true;
                chips[i].transform.position = impact;
                velocities[i] = new Vector3(direction * (0.45f + i * 0.23f), 0.8f + (i % 3) * 0.45f, 0f);
            }
        }

        private void LateUpdate() => Tick(Time.deltaTime);

        private void Tick(float deltaTime)
        {
            age += deltaTime;
            targetSprite.color = health.IsAlive ? restColor : restColor * new Color(.55f, .55f, .55f, .6f);
            // Briefly hold compression, then settle around the unchanged collision footprint.
            float t = Mathf.Max(0f, age - 0.045f);
            float recoil = age < 0.045f ? 1f : Mathf.Cos(t * 25f) * Mathf.Exp(-t * 18f);
            if (age > 0.32f) recoil = 0f;
            visual.localPosition = restPosition + Vector3.right * (direction * 0.065f * strength * recoil);
            visual.localRotation = restRotation * Quaternion.Euler(0f, 0f, -direction * 5f * strength * recoil);
            for (int i = 0; i < chips.Length; i++)
            {
                if (age >= 0.25f) { chips[i].enabled = false; continue; }
                if (age <= 0.045f) continue;
                velocities[i] += Vector3.down * (8f * deltaTime);
                chips[i].transform.position += velocities[i] * deltaTime;
                chips[i].transform.Rotate(0f, 0f, (i % 2 == 0 ? 1f : -1f) * 480f * deltaTime);
            }
        }
    }
}
