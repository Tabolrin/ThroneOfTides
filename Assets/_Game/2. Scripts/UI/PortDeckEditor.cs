// Assets/_Game/2. Scripts/UI/PortDeckEditor.cs
using System.Collections.Generic;
using System.Linq;
using TMPro;
using ThroneOfTides.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // Runtime deck editor — mirrors DeckBuilderWindow but as in-game UI.
    // Modifies the DeckDefinitionSO directly; changes persist in Editor.
    // TODO: JSON serialization needed for build persistence (post-vertical-slice).
    public class PortDeckEditor : MonoBehaviour
    {
        [Header("Deck List")]
        [SerializeField] private Transform      _deckContent;
        [SerializeField] private PortCardRow    _cardRowPrefab;
        [SerializeField] private CardTypePaletteSO _palette;

        [Header("Storage Bar")]
        [SerializeField] private TextMeshProUGUI _storageLabel;
        [SerializeField] private Slider          _storageBar;
        [SerializeField] private Image           _storageBarFill;
        private static readonly Color StorageOk   = new Color(0.25f, 0.75f, 0.35f);
        private static readonly Color StorageFull = new Color(0.85f, 0.30f, 0.25f);

        [Header("Footer")]
        [SerializeField] private TextMeshProUGUI _cardCountLabel;
        [SerializeField] private Button          _saveButton;

        // Fired when the save button is pressed — PortManager handles actual save
        public System.Action OnSaveRequested;

        private DeckDefinitionSO _deck;
        public  int              MaxStorage { get; private set; }

        // ── Init ───────────────────────────────────────────────────────────────

        public void Initialise(DeckDefinitionSO deck, int maxStorage)
        {
            _deck      = deck;
            MaxStorage = maxStorage;

            _saveButton.onClick.AddListener(() => OnSaveRequested?.Invoke());

            Refresh();
        }

        // Called by PortManager when storage upgrade is purchased
        public void UpdateMaxStorage(int newMax)
        {
            MaxStorage = newMax;
            Refresh();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        // Returns false if storage cap would be exceeded
        public bool TryAddCard(CardSO card)
        {
            if (GetStorageUsed() + card.StorageCost > MaxStorage) return false;

            int idx = _deck.Cards.FindIndex(e => e.Card == card);
            if (idx >= 0)
            {
                var entry = _deck.Cards[idx];
                _deck.Cards[idx] = new DeckDefinitionSO.CardEntry
                    { Card = card, Count = entry.Count + 1 };
            }
            else
            {
                _deck.Cards.Add(new DeckDefinitionSO.CardEntry { Card = card, Count = 1 });
            }

            Refresh();
            return true;
        }

        public void RemoveCard(CardSO card)
        {
            int idx = _deck.Cards.FindIndex(e => e.Card == card);
            if (idx < 0) return;

            var entry = _deck.Cards[idx];
            if (entry.Count <= 1)
                _deck.Cards.RemoveAt(idx);
            else
                _deck.Cards[idx] = new DeckDefinitionSO.CardEntry
                    { Card = card, Count = entry.Count - 1 };

            Refresh();
        }

        public int GetCountInDeck(CardSO card)
        {
            int idx = _deck.Cards.FindIndex(e => e.Card == card);
            return idx >= 0 ? _deck.Cards[idx].Count : 0;
        }

        public int GetStorageUsed()
        {
            int used = 0;
            foreach (var entry in _deck.Cards)
                if (entry.Card != null) used += entry.Card.StorageCost * entry.Count;
            return used;
        }

        // ── Refresh ────────────────────────────────────────────────────────────

        private void Refresh()
        {
            ClearRows();
            BuildRows();
            UpdateStorageDisplay();
            UpdateCardCount();
        }

        private void ClearRows()
        {
            foreach (Transform child in _deckContent)
                Destroy(child.gameObject);
        }

        private void BuildRows()
        {
            // Sort by type then name for readability
            var sorted = new List<DeckDefinitionSO.CardEntry>(_deck.Cards);
            sorted.RemoveAll(e => e.Card == null);
            sorted.Sort((a, b) =>
            {
                int typeComp = a.Card.CardType.CompareTo(b.Card.CardType);
                return typeComp != 0 ? typeComp
                                     : string.Compare(a.Card.Name, b.Card.Name,
                                                      System.StringComparison.Ordinal);
            });

            foreach (var entry in sorted)
            {
                var cardRef = entry.Card;
                for (int i = 0; i < entry.Count; i++)
                {
                    var row = Instantiate(_cardRowPrefab, _deckContent);
                    row.Setup(cardRef, _palette, () => RemoveCard(cardRef));
                }
            }
        }

        private void UpdateStorageDisplay()
        {
            int used = GetStorageUsed();

            if (_storageLabel != null)
                _storageLabel.text = $"Storage: {used} / {MaxStorage}";

            if (_storageBar != null)
            {
                _storageBar.minValue = 0;
                _storageBar.maxValue = MaxStorage;
                _storageBar.value    = used;
            }

            if (_storageBarFill != null)
                _storageBarFill.color = used >= MaxStorage ? StorageFull : StorageOk;
        }

        private void UpdateCardCount()
        {
            if (_cardCountLabel == null) return;
            int total = _deck.Cards.Where(e => e.Card != null).Sum(e => e.Count);
            _cardCountLabel.text = $"{total} cards";
        }
    }
}