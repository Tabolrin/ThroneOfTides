// Assets/_Game/2. Scripts/UI/ActiveEffectsBar.cs
using System.Collections.Generic;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.UI
{
    // Shows only the currently-active per-ship status badges (Gunpowder/Whirlpool/Hail
    // Storm/High Spirits/Siren Song) plus reaction charge badges (Dead Man's Turn/Counter
    // Gale) for one ship. Badges are dynamically instantiated from a single shared prefab
    // when an effect becomes active and destroyed when it clears — nothing sits pre-placed
    // in the scene. One instance per ship — set Side to which ship this represents.
    public class ActiveEffectsBar : MonoBehaviour
    {
        [Header("Side")]
        [Tooltip("Which ship this bar displays status/reactions for.")]
        [SerializeField] private DamageTarget _side = DamageTarget.Player;

        [Header("Layout")]
        [Tooltip("Row that holds dynamically spawned status-effect badges (Gunpowder, Whirlpool, Hail Storm, High Spirits, Siren Song).")]
        [SerializeField] private RectTransform _effectsContainer;
        [Tooltip("Row that holds dynamically spawned reaction-charge badges (Dead Man's Turn, Counter Gale).")]
        [SerializeField] private RectTransform _reactionsContainer;

        [Header("Data")]
        [SerializeField] private EffectBadgeView       _badgePrefab;
        [SerializeField] private EffectSymbolPaletteSO _palette;

        [Header("Reaction Icons")]
        [Tooltip("Reactions aren't ShipStatusTypes, so they don't come from the palette — assign their icons directly.")]
        [SerializeField] private Sprite _deadMansTurnIcon;
        [SerializeField] private Sprite _counterGaleIcon;

        private readonly Dictionary<ShipStatusType, EffectBadgeView> _activeStatusBadges =
            new Dictionary<ShipStatusType, EffectBadgeView>();
        private readonly Dictionary<ReactionType, EffectBadgeView> _activeReactionBadges =
            new Dictionary<ReactionType, EffectBadgeView>();
        private readonly Dictionary<ReactionType, int> _reactionCharges =
            new Dictionary<ReactionType, int>();

        private void OnEnable()
        {
            GameEventBus.OnShipStatusCountChanged += OnShipStatusCountChanged;
            GameEventBus.OnReactionCharged        += OnReactionCharged;
            GameEventBus.OnReactionFired          += OnReactionFired;
            GameEventBus.OnMatchWin               += OnMatchEnd;
            GameEventBus.OnMatchLoss              += OnMatchEnd;
        }

        private void OnDisable()
        {
            GameEventBus.OnShipStatusCountChanged -= OnShipStatusCountChanged;
            GameEventBus.OnReactionCharged        -= OnReactionCharged;
            GameEventBus.OnReactionFired          -= OnReactionFired;
            GameEventBus.OnMatchWin               -= OnMatchEnd;
            GameEventBus.OnMatchLoss              -= OnMatchEnd;
        }

        // ── Status Effects ───────────────────────────────────────────────────

        private void OnShipStatusCountChanged(ShipStatusType type, DamageTarget ship, int count)
        {
            if (ship != _side) return;

            // Siren Song is a pending-effect badge — presence only, no count shown.
            int? displayCount = type == ShipStatusType.SirenSong ? (int?)null : count;

            SetBadge(_activeStatusBadges, type, count > 0, _effectsContainer,
                _palette != null ? _palette.GetSprite(type) : null, displayCount);
        }

        // ── Reaction Charges ─────────────────────────────────────────────────

        private void OnReactionCharged(ReactionType type, DamageTarget side, int charges)
        {
            if (side != _side) return;
            _reactionCharges[type] = charges;
            RefreshReactionBadge(type, charges);
        }

        private void OnReactionFired(ReactionType type, DamageTarget side)
        {
            if (side != _side) return;

            _reactionCharges.TryGetValue(type, out int current);
            int remaining = Mathf.Max(0, current - 1);
            _reactionCharges[type] = remaining;
            RefreshReactionBadge(type, remaining);
        }

        private void RefreshReactionBadge(ReactionType type, int charges)
        {
            Sprite icon = type == ReactionType.DeadMansTurn ? _deadMansTurnIcon : _counterGaleIcon;
            SetBadge(_activeReactionBadges, type, charges > 0, _reactionsContainer, icon, charges);
        }

        // ── Shared badge create/update/destroy ───────────────────────────────

        private void SetBadge<T>(Dictionary<T, EffectBadgeView> active, T key, bool shouldBeActive,
            RectTransform container, Sprite icon, int? count)
        {
            bool exists = active.TryGetValue(key, out EffectBadgeView badge);

            if (!shouldBeActive)
            {
                if (exists)
                {
                    Destroy(badge.gameObject);
                    active.Remove(key);
                }
                return;
            }

            if (!exists)
            {
                if (_badgePrefab == null || container == null) return;
                badge = Instantiate(_badgePrefab, container);
                active[key] = badge;
            }

            badge.Setup(icon, count);
        }

        private void OnMatchEnd()
        {
            foreach (var badge in _activeStatusBadges.Values)
                if (badge != null) Destroy(badge.gameObject);
            _activeStatusBadges.Clear();

            foreach (var badge in _activeReactionBadges.Values)
                if (badge != null) Destroy(badge.gameObject);
            _activeReactionBadges.Clear();
            _reactionCharges.Clear();
        }
    }
}
