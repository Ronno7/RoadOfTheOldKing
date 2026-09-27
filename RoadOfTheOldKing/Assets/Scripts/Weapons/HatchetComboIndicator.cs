using UnityEngine;

namespace TheLostShrine.Weapons
{
    // A view of combo state; it does not control attack timing or progression.
    [DisallowMultipleComponent, RequireComponent(typeof(HatchetWeapon))]
    public sealed class HatchetComboIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] pips = new SpriteRenderer[0];
        private HatchetWeapon weapon;

        private void Awake() => weapon = GetComponent<HatchetWeapon>();

        private void LateUpdate()
        {
            int step = weapon.ComboStep;
            float opacity = weapon.State == HatchetState.LightChop ? 1f
                : Mathf.Clamp01(weapon.ComboTimeRemaining / 0.15f);
            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] == null)
                    continue;
                pips[i].enabled = step > 0;
                bool current = i == step - 1;
                Color color = current ? new Color(1f, 0.96f, 0.72f, opacity)
                    : i < step ? new Color(0.95f, 0.68f, 0.25f, opacity * 0.8f)
                    : new Color(0.25f, 0.3f, 0.32f, opacity * 0.6f);
                pips[i].color = color;
                float size = current && weapon.State == HatchetState.LightChop
                    ? Mathf.Lerp(1.35f, 1f, weapon.AttackProgress) : 1f;
                pips[i].transform.localScale = Vector3.one * (0.08f * size);
            }
        }

        private void OnDisable()
        {
            if (pips == null)
                return;
            foreach (var pip in pips)
                if (pip != null)
                    pip.enabled = false;
        }
    }
}
