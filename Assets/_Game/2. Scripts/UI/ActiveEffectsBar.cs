// Assets/_Game/2. Scripts/UI/ActiveEffectsBar.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.UI
{
    // Shows reaction charges (player side only — reactions are player-only under current rules)
    // and active per-ship status badges (Gunpowder/Whirlpool/Hail Storm/High Spirits/Siren Song).
    // Unlike a dynamic instantiate-a-prefab system, this drives a fixed set of pre-placed badge
    // GameObjects authored per status type — toggling visibility and updating a count label rather
    // than spawning/destroying instances. One instance per ship — set Side to which ship this
    // represents, and assign a Badges entry per status type you've placed in the scene.
    public class ActiveEffectsBar : MonoBehaviour
    {
        [Serializable]
        public class StatusBadge
        {
            [Tooltip("Which status this badge represents.")]
            public ShipStatusType Type;
            [Tooltip("Root GameObject to show/hide for this badge.")]
            public GameObject Root;
            [Tooltip("Count label shown on the badge (e.g. stack count or turns remaining).")]
            public TextMeshProUGUI CountLabel;
        }

        [Header("Side")]
        [Tooltip("Which ship this bar displays status for. Reaction charges only ever show on the Player instance.")]
        [SerializeField] private DamageTarget _side = DamageTarget.Player;

        [Header("Reactions — Dead Man's Turn")]
        [SerializeField] private GameObject      _dmtRoot;
        [SerializeField] private TextMeshProUGUI _dmtChargesLabel;

        [Header("Reactions — Counter Gale")]
        [SerializeField] private GameObject      _counterGaleRoot;
        [SerializeField] private TextMeshProUGUI _counterGaleChargesLabel;

        [Header("Status Badges")]
        [Tooltip("One entry per pre-placed status badge in the scene.")]
        [SerializeField] private List<StatusBadge> _badges = new List<StatusBadge>();

        private int _dmtCharges;
        private int _counterGaleCharges;

        private void OnEnable()
        {
            GameEventBus.OnReactionCharged        += OnReactionCharged;
            GameEventBus.OnReactionFired          += OnReactionFired;
            GameEventBus.OnShipStatusCountChanged += OnShipStatusCountChanged;
            GameEventBus.OnMatchWin               += OnMatchEnd;
            GameEventBus.OnMatchLoss              += OnMatchEnd;
        }

        private void OnDisable()
        {
            GameEventBus.OnReactionCharged        -= OnReactionCharged;
            GameEventBus.OnReactionFired          -= OnReactionFired;
            GameEventBus.OnShipStatusCountChanged -= OnShipStatusCountChanged;
            GameEventBus.OnMatchWin               -= OnMatchEnd;
            GameEventBus.OnMatchLoss              -= OnMatchEnd;
        }

        private void OnReactionCharged(ReactionType type, int charges)
        {
            if (_side != DamageTarget.Player) return;

            if (type == ReactionType.DeadMansTurn)
            {
                _dmtCharges = charges;
                Refresh(_dmtRoot, _dmtChargesLabel, _dmtCharges);
            }
            else
            {
                _counterGaleCharges = charges;
                Refresh(_counterGaleRoot, _counterGaleChargesLabel, _counterGaleCharges);
            }
        }

        private void OnReactionFired(ReactionType type)
        {
            if (_side != DamageTarget.Player) return;

            if (type == ReactionType.DeadMansTurn)
            {
                _dmtCharges = Mathf.Max(0, _dmtCharges - 1);
                Refresh(_dmtRoot, _dmtChargesLabel, _dmtCharges);
            }
            else
            {
                _counterGaleCharges = Mathf.Max(0, _counterGaleCharges - 1);
                Refresh(_counterGaleRoot, _counterGaleChargesLabel, _counterGaleCharges);
            }
        }

        private void OnShipStatusCountChanged(ShipStatusType type, DamageTarget ship, int count)
        {
            if (ship != _side) return;

            var badge = _badges.Find(b => b.Type == type);
            if (badge == null) return;

            Refresh(badge.Root, badge.CountLabel, count);
        }

        private void OnMatchEnd()
        {
            _dmtCharges = 0;
            _counterGaleCharges = 0;

            foreach (var badge in _badges)
                Refresh(badge.Root, badge.CountLabel, 0);
        }

        private static void Refresh(GameObject root, TextMeshProUGUI label, int charges)
        {
            if (root  != null) root.SetActive(charges > 0);
            if (label != null) label.text = charges > 0 ? $"×{charges}" : string.Empty;
        }
    }
}
