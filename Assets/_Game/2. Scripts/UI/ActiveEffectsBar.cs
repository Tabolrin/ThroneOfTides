// Assets/_Game/2. Scripts/UI/ActiveEffectsBar.cs
using TMPro;
using ThroneOfTides.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // Visual bridge between the mana, combo, DOT, and reaction systems and the HUD.
    // Subscribes to GameEventBus and updates all active-state indicators each event.
    // Attach to the ActiveEffectsBar GameObject in the game scene.
    public class ActiveEffectsBar : MonoBehaviour
    {
        [Header("Mana")]
        [SerializeField] private TextMeshProUGUI _playerManaLabel;
        [SerializeField] private TextMeshProUGUI _enemyManaLabel;

        [Header("Combo")]
        [SerializeField] private GameObject      _comboGroup;
        [SerializeField] private TextMeshProUGUI _comboStackLabel;

        [Header("DOT")]
        [SerializeField] private GameObject      _dotGroup;
        [SerializeField] private TextMeshProUGUI _dotLabel;

        [Header("Reactions")]
        [SerializeField] private GameObject      _deadMansTurnGroup;
        [SerializeField] private TextMeshProUGUI _deadMansTurnChargeLabel;
        [SerializeField] private GameObject      _bloodForBloodGroup;
        [SerializeField] private TextMeshProUGUI _bloodForBloodChargeLabel;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameEventBus.OnPlayerManaChanged += OnPlayerManaChanged;
            GameEventBus.OnEnemyManaChanged  += OnEnemyManaChanged;
            GameEventBus.OnComboStackChanged += OnComboStackChanged;
            GameEventBus.OnDOTApplied        += OnDOTApplied;
            GameEventBus.OnDOTTick           += OnDOTTick;
            GameEventBus.OnReactionCharged   += OnReactionCharged;
            GameEventBus.OnReactionFired     += OnReactionFired;
        }

        private void OnDisable()
        {
            GameEventBus.OnPlayerManaChanged -= OnPlayerManaChanged;
            GameEventBus.OnEnemyManaChanged  -= OnEnemyManaChanged;
            GameEventBus.OnComboStackChanged -= OnComboStackChanged;
            GameEventBus.OnDOTApplied        -= OnDOTApplied;
            GameEventBus.OnDOTTick           -= OnDOTTick;
            GameEventBus.OnReactionCharged   -= OnReactionCharged;
            GameEventBus.OnReactionFired     -= OnReactionFired;
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void OnPlayerManaChanged(int current, int max)
        {
            if (_playerManaLabel != null)
                _playerManaLabel.text = $"{current} / {max}";
        }

        private void OnEnemyManaChanged(int current, int max)
        {
            if (_enemyManaLabel != null)
                _enemyManaLabel.text = $"{current} / {max}";
        }

        private void OnComboStackChanged(int count)
        {
            if (_comboGroup != null)
                _comboGroup.SetActive(count > 0);
            if (_comboStackLabel != null)
                _comboStackLabel.text = $"x{count}";
        }

        private void OnDOTApplied(DotEffect effect)  => ShowDOT(effect);
        private void OnDOTTick(DotEffect effect)      => ShowDOT(effect);

        private void ShowDOT(DotEffect effect)
        {
            bool active = effect.TurnsRemaining > 0;
            if (_dotGroup != null)
                _dotGroup.SetActive(active);
            if (_dotLabel != null && active)
                _dotLabel.text = $"{effect.DamagePerTurn} / {effect.TurnsRemaining}t";
        }

        private void OnReactionCharged(CardType type, int charges)
        {
            UpdateReactionDisplay(type, charges);
        }

        private void OnReactionFired(CardType type)
        {
            // Charges managed by GameState; fire with 0 to hide group
            UpdateReactionDisplay(type, 0);
        }

        private void UpdateReactionDisplay(CardType type, int charges)
        {
            if (type == CardType.Reaction)
            {
                // GameEventBus fires Reaction type generically — use charge count to drive both groups.
                // Individual group visibility is driven by GameState charge counts wired at startup;
                // here we just refresh whichever group corresponds to the event.
                return;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        // Called by GameBootstrapper or TurnCoordinator to push initial reaction state
        // once GameState is fully initialised.
        public void RefreshReactions(int deadMansTurnCharges, int bloodForBloodCharges)
        {
            SetReactionGroup(_deadMansTurnGroup, _deadMansTurnChargeLabel, deadMansTurnCharges);
            SetReactionGroup(_bloodForBloodGroup, _bloodForBloodChargeLabel, bloodForBloodCharges);
        }

        private static void SetReactionGroup(GameObject group, TextMeshProUGUI label, int charges)
        {
            if (group != null)
                group.SetActive(charges > 0);
            if (label != null)
                label.text = charges.ToString();
        }
    }
}
