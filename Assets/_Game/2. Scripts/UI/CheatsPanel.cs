// Assets/_Game/2. Scripts/UI/CheatsPanel.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Systems;

namespace ThroneOfTides.UI
{
    /// <summary>
    /// Playtest-only debug panel - toggled by a button, grants HP/mana to either side on demand.
    /// Auto-hides outside debug builds. To add a new cheat later: add a button in the scene,
    /// wire its OnClick to a new GameState.Cheat* method (or a new method here if it needs the
    /// shared amount field), and call RefreshAfterCheat() - no other changes needed.
    /// </summary>
    public class CheatsPanel : MonoBehaviour
    {
        [Header("Toggle")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _toggleButton;

        [Header("Shared Amount")]
        [Tooltip("How much HP/mana each button below grants per click.")]
        [SerializeField] private TMP_InputField _amountField;

        [Header("Cheats - Add")]
        [SerializeField] private Button _addPlayerHPButton;
        [SerializeField] private Button _addEnemyHPButton;
        [SerializeField] private Button _addPlayerManaButton;
        [SerializeField] private Button _addEnemyManaButton;

        [Header("Cheats - Set Max")]
        [Tooltip("Sets the ceiling itself to the amount field - current value is only pulled down if it now exceeds the new max. Click the matching Add button afterward to fill it back up.")]
        [SerializeField] private Button _setPlayerMaxHPButton;
        [SerializeField] private Button _setEnemyMaxHPButton;
        [SerializeField] private Button _setPlayerMaxManaButton;
        [SerializeField] private Button _setEnemyMaxManaButton;

        [Header("Cheats - Presentation")]
        [Tooltip("Sets both sides' HP and Mana (current and max) to 999 - for screenshots/demos where combat math shouldn't get in the way. Ignores the shared Amount field. Purely an in-memory GameState change like every other cheat here - never touches PlayerInventory, so the next match still starts at normal HP/mana regardless of upgrade levels.")]
        [SerializeField] private Button _presentationModeButton;

        private GameState _gameState;
        private Action _onChanged;

        public void Initialise(GameState gameState, Action onChanged)
        {
            _gameState = gameState;
            _onChanged = onChanged;
        }

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
                return;
            }

            if (_panelRoot != null) _panelRoot.SetActive(false);
            if (_toggleButton != null) _toggleButton.onClick.AddListener(TogglePanel);

            if (_addPlayerHPButton   != null) _addPlayerHPButton.onClick.AddListener(() => Apply(_gameState.CheatAddPlayerHP));
            if (_addEnemyHPButton    != null) _addEnemyHPButton.onClick.AddListener(() => Apply(_gameState.CheatAddEnemyHP));
            if (_addPlayerManaButton != null) _addPlayerManaButton.onClick.AddListener(() => Apply(_gameState.CheatAddPlayerMana));
            if (_addEnemyManaButton  != null) _addEnemyManaButton.onClick.AddListener(() => Apply(_gameState.CheatAddEnemyMana));

            if (_setPlayerMaxHPButton   != null) _setPlayerMaxHPButton.onClick.AddListener(() => Apply(_gameState.CheatSetPlayerMaxHP));
            if (_setEnemyMaxHPButton    != null) _setEnemyMaxHPButton.onClick.AddListener(() => Apply(_gameState.CheatSetEnemyMaxHP));
            if (_setPlayerMaxManaButton != null) _setPlayerMaxManaButton.onClick.AddListener(() => Apply(_gameState.CheatSetPlayerMaxMana));
            if (_setEnemyMaxManaButton  != null) _setEnemyMaxManaButton.onClick.AddListener(() => Apply(_gameState.CheatSetEnemyMaxMana));

            if (_presentationModeButton != null) _presentationModeButton.onClick.AddListener(ApplyPresentationMode);
        }

        // Maxes out and fully fills both sides' HP and Mana - reuses the same per-side cheat
        // methods every other button here does (Set Max then Add, so the fill actually reaches
        // the new ceiling), just for both sides and both stats in one click. Nothing here writes
        // to PlayerInventory - GameState is a fresh in-memory object built fresh at the start of
        // every match from PlayerInventory's saved upgrade levels, never the other way around -
        // so this can never leak into a future match's starting stats.
        private void ApplyPresentationMode()
        {
            if (_gameState == null) return;

            const int amount = 999;

            _gameState.CheatSetPlayerMaxHP(amount);
            _gameState.CheatAddPlayerHP(amount);
            _gameState.CheatSetEnemyMaxHP(amount);
            _gameState.CheatAddEnemyHP(amount);
            _gameState.CheatSetPlayerMaxMana(amount);
            _gameState.CheatAddPlayerMana(amount);
            _gameState.CheatSetEnemyMaxMana(amount);
            _gameState.CheatAddEnemyMana(amount);

            _onChanged?.Invoke();
        }

        private void TogglePanel()
        {
            if (_panelRoot != null) _panelRoot.SetActive(!_panelRoot.activeSelf);
        }

        private void Apply(Action<int> cheat)
        {
            if (_gameState == null) return;

            if (_amountField == null || !int.TryParse(_amountField.text, out int amount))
                amount = 10;

            cheat(amount);
            _onChanged?.Invoke();
        }
    }
}
