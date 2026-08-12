// Assets/_Game/2. Scripts/UI/PortInventoryView.cs
using System.Collections.Generic;
using TMPro;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // Displays the player's card collection as a scrollable grid.
    // Filter buttons by CardType are wired by PortManager.
    public class PortInventoryView : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private Transform        _inventoryContent;
        [SerializeField] private PortInventoryCard _cardPanelPrefab;

        [Header("Preview")]
        // Shared instance living on the root Canvas (not nested in this view's ScrollRect) so a
        // preview configured larger than the scroll viewport is never clipped by it.
        [SerializeField] private CardPreviewTooltip _previewTooltip;

        [Header("Filter")]
        [SerializeField] private Button _filterAll;
        [SerializeField] private Button _filterWeapon;
        [SerializeField] private Button _filterCombo;
        [SerializeField] private Button _filterAction;
        [SerializeField] private Button _filterDOT;
        [SerializeField] private Button _filterReaction;

        // Fired when the player requests to add a card to the deck
        public System.Action<CardSO> OnAddCardRequested;

        private PlayerInventory _inventory;
        private PortDeckEditor  _deckEditor;
        private CardType?       _activeFilter = null;

        private ObjectPool<PortInventoryCard> _cardPool;

        // ── Init ───────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Pools inventory card panels instead of Instantiate/Destroy per refresh — Refresh()
            // rebuilds the whole grid on every filter click and every add-to-deck.
            _cardPool = new ObjectPool<PortInventoryCard>(
                createFunc: () => Instantiate(_cardPanelPrefab),
                actionOnGet: card => card.gameObject.SetActive(true),
                actionOnRelease: card =>
                {
                    card.gameObject.SetActive(false);
                    card.transform.SetParent(transform, false);
                },
                actionOnDestroy: card => Destroy(card.gameObject),
                collectionCheck: false);
        }

        public void Initialise(PlayerInventory inventory, PortDeckEditor deckEditor)
        {
            _inventory  = inventory;
            _deckEditor = deckEditor;

            WireFilterButtons();
            Refresh();
        }

        private void WireFilterButtons()
        {
            if (_filterAll)      _filterAll.onClick.AddListener(() => SetFilter(null));
            if (_filterWeapon)   _filterWeapon.onClick.AddListener(() => SetFilter(CardType.Weapon));
            if (_filterCombo)    _filterCombo.onClick.AddListener(() => SetFilter(CardType.Combo));
            if (_filterAction)   _filterAction.onClick.AddListener(() => SetFilter(CardType.Action));
            if (_filterDOT)      _filterDOT.onClick.AddListener(() => SetFilter(CardType.DOT));
            if (_filterReaction) _filterReaction.onClick.AddListener(() => SetFilter(CardType.Reaction));
        }

        // ── Filter ─────────────────────────────────────────────────────────────

        public void SetFilter(CardType? type)
        {
            _activeFilter = type;
            Refresh();
        }

        // ── Refresh ────────────────────────────────────────────────────────────

        public void Refresh()
        {
            // Snapshot first — releasing reparents each card out of _inventoryContent
            // immediately, which would corrupt a live `foreach (Transform child in ...)`.
            var existing = new List<PortInventoryCard>(
                _inventoryContent.GetComponentsInChildren<PortInventoryCard>(true));
            foreach (var card in existing)
                _cardPool.Release(card);

            var unlocked = BuildUnlockedCardList();

            foreach (var card in unlocked)
            {
                if (_activeFilter.HasValue && card.CardType != _activeFilter.Value)
                    continue;

                // Ownership is per card type (unlocked or not), not per copy — how many copies
                // of an owned card you may run is governed by CardSO.MaxCopiesInDeck (bounded
                // only by storage for most cards; a few singleton cards cap much lower).
                int  inDeckCount = _deckEditor.GetCountInDeck(card);
                bool canAdd      = _deckEditor.GetStorageUsed() + card.StorageCost <= _deckEditor.MaxStorage
                                   && inDeckCount < card.MaxCopiesInDeck;

                var panel   = _cardPool.Get();
                panel.transform.SetParent(_inventoryContent, false);
                var cardRef = card; // capture for lambda

                panel.Setup(card, card.MaxCopiesInDeck, inDeckCount, canAdd,
                            () => OnAddCardRequested?.Invoke(cardRef), _previewTooltip);
            }
        }

        // Unique unlocked cards, sorted by type then name — a card's presence in the collection
        // means it's unlocked; how many times it happens to appear there is not meaningful.
        private List<CardSO> BuildUnlockedCardList()
        {
            var unique = new List<CardSO>();
            foreach (var card in _inventory.Collection)
                if (card != null && !unique.Contains(card))
                    unique.Add(card);

            unique.Sort((a, b) =>
            {
                int typeComp = a.CardType.CompareTo(b.CardType);
                return typeComp != 0 ? typeComp
                                     : string.Compare(a.Name, b.Name, System.StringComparison.Ordinal);
            });

            return unique;
        }
    }
}