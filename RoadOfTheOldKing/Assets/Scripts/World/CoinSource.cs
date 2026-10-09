using System.Collections.Generic;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // One authored source, not one roll per collider/blade. Keep this parent active when the pot breaks.
    public sealed class CoinSource : MonoBehaviour, IProgressParticipant, IResetOnRest
    {
        [SerializeField] private string sourceId;
        [Tooltip("0 rolls the ordinary prop table; a positive amount is a fixed route cache.")]
        [SerializeField, Min(0)] private int guaranteedCoins;
        [SerializeField] private Breakable breakable;
        [SerializeField] private GameObject coinVisual;
        [SerializeField] private string requiredMilestone;
        [SerializeField] private ParticleSystem breakFeedback;
        private readonly List<RaycastHit2D> accessHits = new List<RaycastHit2D>(8);
        public string SourceId => sourceId;
        public bool IsRevealed => breakable == null || breakable.IsBroken;
        private void OnEnable() { if (breakable != null) breakable.Broken += OnBroken; }
        private void OnDisable() { if (breakable != null) breakable.Broken -= OnBroken; }
        private void OnBroken(CombatHit hit)
        {
            var game = GameSession.Instance;
            if (game == null) return;
            game.Rewards.RevealCoinSource(sourceId, guaranteedCoins);
            Refresh(game.Progress);
            if (breakFeedback != null) {
                breakFeedback.Play();
                for (int i = 0; i < 6; i++) breakFeedback.Emit(new ParticleSystem.EmitParams {
                    position = transform.position,
                    velocity = (Vector3)(hit.Direction * 1.2f + new Vector2(Random.Range(-2f, 2f), Random.Range(1f, 3f))),
                    startLifetime = Random.Range(.35f, .6f), startSize = Random.Range(2, 4) / 16f,
                    startColor = i % 2 == 0 ? new Color(.7f, .4f, .22f) : new Color(.87f, .65f, .4f),
                    rotation = Random.Range(0f, 180f)
                }, 1);
            }
        }
        public bool TryCollect(PlayerHealth player)
        {
            var game = GameSession.Instance;
            if (!isActiveAndEnabled || game == null || game.IsLoading || player == null || !player.IsAlive ||
                !IsRevealed || Vector2.Distance(player.transform.position, transform.position) > .9f ||
                (!string.IsNullOrEmpty(requiredMilestone) && !game.Progress.Has(requiredMilestone)) ||
                !WorldAccess.IsClear(player, transform, accessHits)) return false;
            if (game.Rewards.RevealCoinSource(sourceId, guaranteedCoins) == null) return false;
            bool awarded = game.Rewards.TryCollectCoinSource(sourceId);
            Refresh(game.Progress);
            return awarded;
        }
        private void Refresh(ProgressState state)
        {
            var record = state.FindCoinSource(sourceId);
            if (coinVisual != null) coinVisual.SetActive(record == null ? breakable == null : !record.collected && record.amount > 0);
        }
        public void CaptureProgress(ProgressState state) { }
        public void ResetOnRest()
        {
            if (breakFeedback != null) breakFeedback.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void RestoreProgress(ProgressState state)
        {
            if (breakable != null && state.FindCoinSource(sourceId) != null) breakable.RestoreBrokenState();
            Refresh(state);
        }
    }
}
