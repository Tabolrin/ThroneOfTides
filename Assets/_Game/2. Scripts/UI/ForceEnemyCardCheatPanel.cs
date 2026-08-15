// Assets/_Game/2. Scripts/UI/ForceEnemyCardCheatPanel.cs
using System.Collections.Generic;
using TMPro;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using ThroneOfTides.Systems;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    /// <summary>
    /// Playtest-only debug panel - toggled by a button, lists every non-Reaction card in the
    /// CardDatabaseSO registry in a scrollable, searchable list; clicking a name forces the
    /// enemy to play that exact card as its very next play (see TurnCoordinator.ForceEnemyToPlayCard).
    /// Mirrors CardCheatPanel's layout/behavior - same row prefab, same search-filter approach.
    /// Reaction cards (Dead Man's Turn/Counter Gale) are excluded: they're charged when drawn,
    /// not played as an action, so "playing" one here has no defined meaning.
    /// Auto-hides outside debug builds, same convention as CardCheatPanel/CheatsPanel.
    /// </summary>
    public class ForceEnemyCardCheatPanel : MonoBehaviour
    {
        [Header("Toggle")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _toggleButton;

        [Header("List")]
        [Tooltip("Content transform of the ScrollRect - pooled row buttons are parented here.")]
        [SerializeField] private Transform _scrollContent;
        [SerializeField] private CardCheatEntryButton _entryPrefab;

        [Header("Search")]
        [Tooltip("Filters the list below by card name as you type - optional, leave unset to skip filtering entirely.")]
        [SerializeField] private TMP_InputField _searchField;

        private TurnCoordinator _turnCoordinator;
        private CardDatabaseSO  _cardDatabase;

        private ObjectPool<CardCheatEntryButton> _entryPool;
        private bool _listPopulated;

        // Rows are pooled/created once and never destroyed - filtering just shows/hides them,
        // so this tracks which CardSO each spawned row currently represents.
        private readonly List<(CardSO card, CardCheatEntryButton entry)> _spawnedEntries = new();

        public void Initialise(TurnCoordinator turnCoordinator, CardDatabaseSO cardDatabase)
        {
            _turnCoordinator = turnCoordinator;
            _cardDatabase    = cardDatabase;
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
            if (_searchField != null) _searchField.onValueChanged.AddListener(ApplySearchFilter);

            // Pools row buttons instead of Instantiate/Destroy - keeps toggling the panel
            // cheap even if the card registry grows large.
            _entryPool = new ObjectPool<CardCheatEntryButton>(
                createFunc: () => Instantiate(_entryPrefab),
                actionOnGet: entry => entry.gameObject.SetActive(true),
                actionOnRelease: entry =>
                {
                    entry.gameObject.SetActive(false);
                    entry.transform.SetParent(transform, false);
                },
                actionOnDestroy: entry => Destroy(entry.gameObject),
                collectionCheck: false);
        }

        private void TogglePanel()
        {
            if (_panelRoot == null) return;

            bool willShow = !_panelRoot.activeSelf;
            _panelRoot.SetActive(willShow);

            // The registry's contents don't change at runtime, so the list only needs to be
            // built once - first time the panel is opened - not rebuilt on every toggle.
            if (willShow && !_listPopulated)
                PopulateList();
        }

        private void PopulateList()
        {
            if (_cardDatabase == null || _scrollContent == null) return;

            foreach (var card in _cardDatabase.AllCards)
            {
                if (card == null || card.CardType == CardType.Reaction) continue;

                var entry = _entryPool.Get();
                entry.transform.SetParent(_scrollContent, false);
                entry.Setup(card, ForceEnemyToPlay);
                _spawnedEntries.Add((card, entry));
            }

            _listPopulated = true;
        }

        // Rows are never destroyed once spawned - filtering just shows/hides them by whether
        // the card's name contains the search text (case-insensitive), so re-opening the panel
        // or clearing the search always sees the full list again.
        private void ApplySearchFilter(string search)
        {
            bool hasFilter = !string.IsNullOrWhiteSpace(search);

            foreach (var (card, entry) in _spawnedEntries)
            {
                bool matches = !hasFilter || card.Name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
                entry.gameObject.SetActive(matches);
            }
        }

        private void ForceEnemyToPlay(CardSO card)
        {
            if (_turnCoordinator == null || card == null) return;
            _turnCoordinator.ForceEnemyToPlayCard(card);
        }
    }
}
