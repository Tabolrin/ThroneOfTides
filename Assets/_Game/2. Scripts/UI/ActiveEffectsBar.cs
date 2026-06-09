// Assets/_Game/2. Scripts/UI/ActiveEffectsBar.cs
using TMPro;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.UI
{
    public class ActiveEffectsBar : MonoBehaviour
    {
        [Header("Mana")]
        [SerializeField] private TextMeshProUGUI _manaLabel;

        [Header("Reactions — Dead Man's Turn")]
        [SerializeField] private GameObject      _dmtRoot;
        [SerializeField] private TextMeshProUGUI _dmtChargesLabel;

        [Header("Reactions — Blood for Blood")]
        [SerializeField] private GameObject      _bfbRoot;
        [SerializeField] private TextMeshProUGUI _bfbChargesLabel;

        [Header("Combo Stack")]
        [SerializeField] private GameObject      _comboRoot;
        [SerializeField] private TextMeshProUGUI _comboStackLabel;

        private int _dmtCharges = 0;
        private int _bfbCharges = 0;

        private void OnEnable()
        {
            GameEventBus.OnPlayerManaChanged += OnManaChanged;
            GameEventBus.OnReactionCharged   += OnReactionCharged;
            GameEventBus.OnReactionFired     += OnReactionFired;
            GameEventBus.OnComboStackChanged += OnComboStackChanged;
            GameEventBus.OnMatchWin          += OnMatchEnd;
            GameEventBus.OnMatchLoss         += OnMatchEnd;
        }

        private void OnDisable()
        {
            GameEventBus.OnPlayerManaChanged -= OnManaChanged;
            GameEventBus.OnReactionCharged   -= OnReactionCharged;
            GameEventBus.OnReactionFired     -= OnReactionFired;
            GameEventBus.OnComboStackChanged -= OnComboStackChanged;
            GameEventBus.OnMatchWin          -= OnMatchEnd;
            GameEventBus.OnMatchLoss         -= OnMatchEnd;
        }

        private void OnManaChanged(int current, int max)
        {
            if (_manaLabel != null)
                _manaLabel.text = $"{current} / {max}";
        }

        private void OnReactionCharged(ReactionType type, int charges)
        {
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

        private void OnComboStackChanged(int count)
        {
            if (_comboRoot != null)
                _comboRoot.SetActive(count > 0);
            if (_comboStackLabel != null)
                _comboStackLabel.text = $"×{count}";
        }

        private void OnMatchEnd()
        {
            _dmtCharges = 0;
            _bfbCharges = 0;
        }

        private static void Refresh(GameObject root, TextMeshProUGUI label, int charges)
        {
            if (root  != null) root.SetActive(charges > 0);
            if (label != null) label.text = charges > 1 ? $"×{charges}" : string.Empty;
        }
    }
}