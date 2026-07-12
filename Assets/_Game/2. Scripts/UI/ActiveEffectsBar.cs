// Assets/_Game/2. Scripts/UI/ActiveEffectsBar.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    // Shows reaction charges (player side only — reactions are player-only under current rules)
    // and active per-ship status icons (Gunpowder/Whirlpool/Hail Storm/High Spirits/Siren Song),
    // each with a numeric badge. One instance per ship — set Side to which ship this represents.
    // Mana display lives on GameHUD instead (see its HP-bottle-style fill meter).
    public class ActiveEffectsBar : MonoBehaviour
    {
        [Header("Side")]
        [Tooltip("Which ship this bar displays status for. Reaction charges only ever show on the Player instance.")]
        [SerializeField] private DamageTarget _side = DamageTarget.Player;

        [Header("Reactions — Dead Man's Turn")]
        [SerializeField] private GameObject      _dmtRoot;
        [SerializeField] private TextMeshProUGUI _dmtChargesLabel;

        [Header("Reactions — Counter Gale")]
        [SerializeField] private GameObject      _bfbRoot;
        [SerializeField] private TextMeshProUGUI _bfbChargesLabel;

        [Header("Status Icons")]
        [Tooltip("Maps each ShipStatusType to its icon sprite.")]
        [SerializeField] private EffectSymbolPaletteSO _symbolPalette;
        [Tooltip("Prefab instantiated per active status (icon + count badge).")]
        [SerializeField] private EffectIconView _iconPrefab;
        [Tooltip("Parent the icon row instantiates under.")]
        [SerializeField] private Transform _iconContainer;

        private int _dmtCharges;
        private int _bfbCharges;
        private readonly Dictionary<ShipStatusType, EffectIconView> _activeIcons = new Dictionary<ShipStatusType, EffectIconView>();

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
                _bfbCharges = charges;
                Refresh(_bfbRoot, _bfbChargesLabel, _bfbCharges);
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
                _bfbCharges = Mathf.Max(0, _bfbCharges - 1);
                Refresh(_bfbRoot, _bfbChargesLabel, _bfbCharges);
            }
        }

        private void OnShipStatusCountChanged(ShipStatusType type, DamageTarget ship, int count)
        {
            if (ship != _side) return;

            if (count <= 0)
            {
                if (_activeIcons.TryGetValue(type, out var existing))
                {
                    if (existing != null) Destroy(existing.gameObject);
                    _activeIcons.Remove(type);
                }
                return;
            }

            if (_activeIcons.TryGetValue(type, out var icon))
            {
                icon.SetCount(count);
                return;
            }

            if (_iconPrefab == null || _iconContainer == null) return;

            var instance = Instantiate(_iconPrefab, _iconContainer);
            instance.Setup(_symbolPalette != null ? _symbolPalette.GetIcon(type) : null, count);
            _activeIcons[type] = instance;
        }

        private void OnMatchEnd()
        {
            _dmtCharges = 0;
            _bfbCharges = 0;

            foreach (var icon in _activeIcons.Values)
                if (icon != null) Destroy(icon.gameObject);
            _activeIcons.Clear();
        }

        private static void Refresh(GameObject root, TextMeshProUGUI label, int charges)
        {
            if (root  != null) root.SetActive(charges > 0);
            if (label != null) label.text = charges > 1 ? $"×{charges}" : string.Empty;
        }
    }
}
