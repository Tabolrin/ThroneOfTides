// Assets/_Game/2. Scripts/UI/CardCheatPanel.cs
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using ThroneOfTides.Systems;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    /// <summary>
    /// Playtest-only debug panel — toggled by a button, lists every card in the CardDatabaseSO
    /// registry in a scrollable list; clicking a name adds that card to the player's hand.
    /// Auto-hides outside debug builds, same convention as CheatsPanel.
    /// </summary>
    public class CardCheatPanel : MonoBehaviour
    {
        [Header("Toggle")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _toggleButton;

        [Header("List")]
        [Tooltip("Content transform of the ScrollRect — pooled row buttons are parented here.")]
        [SerializeField] private Transform _scrollContent;
        [SerializeField] private CardCheatEntryButton _entryPrefab;

        private GameState           _gameState;
        private IHandLayoutManager  _handLayout;
        private CardDatabaseSO      _cardDatabase;
        private int                 _maxHandSize;

        private ObjectPool<CardCheatEntryButton> _entryPool;
        private bool _listPopulated;

        public void Initialise(GameState gameState, IHandLayoutManager handLayout,
                               CardDatabaseSO cardDatabase, int maxHandSize)
        {
            _gameState    = gameState;
            _handLayout   = handLayout;
            _cardDatabase = cardDatabase;
            _maxHandSize  = maxHandSize;
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

            // Pools row buttons instead of Instantiate/Destroy — keeps toggling the panel
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
            // built once — first time the panel is opened — not rebuilt on every toggle.
            if (willShow && !_listPopulated)
                PopulateList();
        }

        private void PopulateList()
        {
            if (_cardDatabase == null || _scrollContent == null) return;

            foreach (var card in _cardDatabase.AllCards)
            {
                if (card == null) continue;

                var entry = _entryPool.Get();
                entry.transform.SetParent(_scrollContent, false);
                entry.Setup(card, AddCardToHand);
            }

            _listPopulated = true;
        }

        private void AddCardToHand(CardSO card)
        {
            if (_gameState == null || _handLayout == null || card == null) return;

            if (_gameState.PlayerHand.Count >= _maxHandSize)
            {
                GameDebug.Log($"[CardCheat] Cannot add {card.Name} — hand is full ({_maxHandSize}).");
                return;
            }

            _gameState.PlayerHand.AddCard(card, _maxHandSize);
            _handLayout.AddCardToPlayerHand(card);
            GameEventBus.FireCardDrawn(card);

            GameDebug.Log($"[CardCheat] Added {card.Name} to hand.");
        }
    }
}
