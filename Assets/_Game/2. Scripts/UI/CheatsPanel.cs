// Assets/_Game/2. Scripts/UI/CheatsPanel.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Systems;

namespace ThroneOfTides.UI
{
    /// <summary>
    /// Playtest-only debug panel — toggled by a button, grants HP/mana to either side on demand.
    /// Auto-hides outside debug builds. To add a new cheat later: add a button in the scene,
    /// wire its OnClick to a new GameState.Cheat* method (or a new method here if it needs the
    /// shared amount field), and call RefreshAfterCheat() — no other changes needed.
    /// </summary>
    public class CheatsPanel : MonoBehaviour
    {
        [Header("Toggle")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _toggleButton;

        [Header("Shared Amount")]
        [Tooltip("How much HP/mana each button below grants per click.")]
        [SerializeField] private TMP_InputField _amountField;

        [Header("Cheats — Add")]
        [SerializeField] private Button _addPlayerHPButton;
        [SerializeField] private Button _addEnemyHPButton;
        [SerializeField] private Button _addPlayerManaButton;
        [SerializeField] private Button _addEnemyManaButton;

        [Header("Cheats — Set Max")]
        [Tooltip("Sets the ceiling itself to the amount field — current value is only pulled down if it now exceeds the new max. Click the matching Add button afterward to fill it back up.")]
        [SerializeField] private Button _setPlayerMaxHPButton;
        [SerializeField] private Button _setEnemyMaxHPButton;
        [SerializeField] private Button _setPlayerMaxManaButton;
        [SerializeField] private Button _setEnemyMaxManaButton;

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
