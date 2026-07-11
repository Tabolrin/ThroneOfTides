// Assets/_Game/2. Scripts/UI/PortInventoryView.cs
using System.Collections.Generic;
using TMPro;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using UnityEngine;
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

        // ── Init ───────────────────────────────────────────────────────────────

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
            foreach (Transform child in _inventoryContent)
                Destroy(child.gameObject);

            var groups = BuildCardGroups();

            foreach (var (card, ownedCount) in groups)
            {
                if (_activeFilter.HasValue && card.CardType != _activeFilter.Value)
                    continue;

                int  inDeckCount = _deckEditor.GetCountInDeck(card);
                bool canAdd      = _deckEditor.GetStorageUsed() + card.StorageCost <= _deckEditor.MaxStorage
                                   && inDeckCount < ownedCount;

                var panel   = Instantiate(_cardPanelPrefab, _inventoryContent);
                var cardRef = card; // capture for lambda

                panel.Setup(card, ownedCount, inDeckCount, canAdd,
                            () => OnAddCardRequested?.Invoke(cardRef));
            }
        }

        // Groups the flat collection list by unique card, sorted by type then name
        private List<(CardSO card, int count)> BuildCardGroups()
        {
            var counts = new Dictionary<CardSO, int>();
            foreach (var card in _inventory.Collection)
            {
                if (counts.ContainsKey(card)) counts[card]++;
                else counts[card] = 1;
            }

            var result = new List<(CardSO, int)>();
            foreach (var kvp in counts)
                result.Add((kvp.Key, kvp.Value));

            result.Sort((a, b) =>
            {
                int typeComp = a.Item1.CardType.CompareTo(b.Item1.CardType);
                return typeComp != 0 ? typeComp
                                     : string.Compare(a.Item1.Name, b.Item1.Name,
                                                      System.StringComparison.Ordinal);
            });

            return result;
        }
    }
}