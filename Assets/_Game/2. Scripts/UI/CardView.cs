// Assets/_Game/2. Scripts/UI/CardView.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Core Visuals")]
        [SerializeField] private Image           _cardArt;
        [SerializeField] private Image           _cardBack;
        [SerializeField] private Image           _cardFrame;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private GameObject      _cardFront;
        [SerializeField] private Animator        _animator;
        [SerializeField] private CardTypePaletteSO _palette;

        [Header("Combat Badge")]
        [SerializeField] private GameObject      _damageBadge;
        [SerializeField] private TextMeshProUGUI _damageLabel;

        [Header("Mana Cost Badge")]
        // Always visible — shows mana cost even when 0
        [SerializeField] private GameObject      _manaCostBadge;
        [SerializeField] private TextMeshProUGUI _manaCostLabel;

        [Header("HP Cost Badge")]
        // Only visible when HPCost > 0 (Ram the Hull, Stolen Wind)
        [SerializeField] private GameObject      _hpCostBadge;
        [SerializeField] private TextMeshProUGUI _hpCostLabel;

        [Header("Tags")]
        // Parent container — child Images are spawned here at runtime
        [SerializeField] private Transform       _tagsContainer;
        [SerializeField] private Image           _tagIconPrefab;

        // Stores the randomised vertical offset for this card's hand position
        // Set once on spawn, read by HandLayoutManager during layout refresh
        [HideInInspector] public float HandYOffset;

        public CardSO CardData      { get; private set; }
        public bool   WasPlayed     { get; set; }
        public bool   IsBeingPlayed { get; set; }

        private void OnEnable()  => GameEventBus.OnCardPlayAccepted += OnCardPlayAccepted;
        private void OnDisable() => GameEventBus.OnCardPlayAccepted -= OnCardPlayAccepted;

        private void OnCardPlayAccepted(ICard card)
        {
            if (card as CardSO == CardData && IsBeingPlayed)
            {
                WasPlayed     = true;
                IsBeingPlayed = false;
            }
        }

        public void Setup(CardSO card)
        {
            CardData = card;

            _cardFront.SetActive(true);
            if (_cardBack != null)
                _cardBack.gameObject.SetActive(false);

            _nameLabel.text = card.Name;

            SetupDamageBadge(card);
            SetupManaBadge(card);
            SetupHPCostBadge(card);
            SetupTags(card);

            if (card.Art != null)
                _cardArt.sprite = card.Art;

            if (_cardFrame != null && _palette != null)
                _cardFrame.color = _palette.GetColor(card.CardType);
        }

        public void SetFaceDown(CardSO card)
        {
            CardData = card;
            _cardFront.SetActive(false);
            if (_cardBack != null)
                _cardBack.gameObject.SetActive(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right
                && CardData != null
                && CardInspectController.Instance != null)
            {
                CardInspectController.Instance.Show(this);
            }
        }

        // ── Badge Setup ─────────────────────────────────────────────────────

        private void SetupDamageBadge(CardSO card)
        {
            bool hasDamage = card.Damage > 0         ||
                             card.CardType == CardType.Combo ||
                             card.CardType == CardType.DOT;

            if (_damageBadge != null)
                _damageBadge.SetActive(hasDamage);

            if (hasDamage && _damageLabel != null)
                _damageLabel.text = card.CardType == CardType.DOT
                    ? $"{card.DotDamagePerTurn}×{card.DotDuration}"
                    : card.Damage.ToString();
        }

        private void SetupManaBadge(CardSO card)
        {
            if (_manaCostBadge == null) return;
            _manaCostBadge.SetActive(true);
            if (_manaCostLabel != null)
                _manaCostLabel.text = card.ManaCost.ToString();
        }

        private void SetupHPCostBadge(CardSO card)
        {
            if (_hpCostBadge == null) return;
            bool hasHPCost = card.HPCost > 0;
            _hpCostBadge.SetActive(hasHPCost);
            if (hasHPCost && _hpCostLabel != null)
                _hpCostLabel.text = $"-{card.HPCost}";
        }

        private void SetupTags(CardSO card)
        {
            if (_tagsContainer == null || _tagIconPrefab == null) return;

            // Clear existing tag icons
            foreach (Transform child in _tagsContainer)
                Destroy(child.gameObject);

            // Spawn one icon per tag
            foreach (var tag in card.Tags)
            {
                if (tag == null || tag.Symbol == null) continue;
                var icon = Instantiate(_tagIconPrefab, _tagsContainer);
                icon.sprite  = tag.Symbol;
                icon.name    = tag.TagName;
                // Tooltip on hover — wire when TooltipController is extended
                var tooltipTrigger = icon.GetComponent<TagTooltipTrigger>();
                if (tooltipTrigger != null)
                    tooltipTrigger.Tag = tag;
            }
        }
    }
}