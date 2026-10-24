// Assets/_Game/2. Scripts/UI/CardView.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class CardView : MonoBehaviour
    {
        // ── Card structure ─────────────────────────────────────────────────────
        [Header("Structure")]
        [SerializeField] private GameObject _cardFront;
        [SerializeField] private Image      _cardBack;

        // ── Type banner (top strip) ────────────────────────────────────────────
        // Banner background color no longer varies per type - only the symbol icon does.
        [Header("Type Banner")]
        [SerializeField] private Image _typeSymbolIcon;

        // ── Identity ───────────────────────────────────────────────────────────
        [Header("Card Identity")]
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private Image           _cardArt;

        // ── Cost banner badges (bottom strip) ──────────────────────────────────
        // Mana: always visible - player must always see the mana cost
        [Header("Cost Banner - Mana")]
        [SerializeField] private GameObject      _manaCostBadge;
        [SerializeField] private TextMeshProUGUI _manaCostLabel;

        // HP cost: hidden on most cards - only shown when HPCost > 0
        [Header("Cost Banner - HP Cost")]
        [SerializeField] private GameObject      _hpCostBadge;
        [SerializeField] private TextMeshProUGUI _hpCostLabel;

        // Damage: hidden on 0-damage non-DOT non-Combo cards
        [Header("Cost Banner - Damage")]
        [SerializeField] private GameObject      _damageBadge;
        [SerializeField] private TextMeshProUGUI _damageLabel;

        // ── Data ───────────────────────────────────────────────────────────────
        [Header("Data")]
        [SerializeField] private CardTypePaletteSO _palette;

        // Persists the random vertical hand offset across layout passes
        [HideInInspector] public float HandYOffset;

        public CardSO CardData      { get; private set; }
        public bool   WasPlayed     { get; set; }
        public bool   IsBeingPlayed { get; set; }

        // Wired by whoever spawns this CardView (HandLayoutManager) instead of reaching into a
        // static CardInspectController.Instance singleton.
        public System.Action<CardView> OnInspectRequested;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameEventBus.OnCardCommitted    += OnCardCommitted;
            GameEventBus.OnCardPlayAccepted += OnCardPlayAccepted;
        }

        private void OnDisable()
        {
            GameEventBus.OnCardCommitted    -= OnCardCommitted;
            GameEventBus.OnCardPlayAccepted -= OnCardPlayAccepted;
        }

        // Fires the instant the play is committed (mana spent) - before any target-selection
        // prompt - so the drag handler knows to release this card instead of snapping it back
        // to hand while the prompt is still pending an answer.
        private void OnCardCommitted(ICard card)
        {
            if (card as CardSO == CardData && IsBeingPlayed)
                WasPlayed = true;
        }

        private void OnCardPlayAccepted(ICard card, DamageTarget? selectedTarget)
        {
            if (card as CardSO == CardData && IsBeingPlayed)
            {
                WasPlayed     = true;
                IsBeingPlayed = false;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void Setup(CardSO card)
        {
            CardData      = card;
            WasPlayed     = false;
            IsBeingPlayed = false;

            _cardFront.SetActive(true);
            if (_cardBack != null) _cardBack.gameObject.SetActive(false);

            _nameLabel.text = card.Name;
            if (_descriptionLabel != null) _descriptionLabel.text = card.Description;

            if (_cardArt != null && card.Art != null)
                _cardArt.sprite = card.Art;

            ApplyTypeVisuals(card);
            SetupCostBanner(card);
        }

        public void SetFaceDown(CardSO card)
        {
            CardData      = card;
            WasPlayed     = false;
            IsBeingPlayed = false;
            _cardFront.SetActive(false);
            if (_cardBack != null) _cardBack.gameObject.SetActive(true);
        }

        // ── Private setup ──────────────────────────────────────────────────────

        private void ApplyTypeVisuals(CardSO card)
        {
            if (_palette == null) return;

            var visuals = _palette.GetVisuals(card.CardType);

            if (_typeSymbolIcon != null)
            {
                // Hide cleanly when no symbol is assigned yet - avoids a broken
                // white square and lets the placeholder state look intentional
                bool hasSymbol = visuals.TypeSymbol != null;
                _typeSymbolIcon.gameObject.SetActive(hasSymbol);
                if (hasSymbol) _typeSymbolIcon.sprite = visuals.TypeSymbol;
            }
        }

        // Shows the card's real cost while a surcharge (Dead Man's Turn) is pending - the base cost
        // plus the extra, in red - and the normal cost again once it's 0. Called by
        // HandLayoutManager for every card in the player's hand whenever the surcharge or the
        // hand changes.
        private static readonly Color ManaSurchargeColor = new Color(0.9f, 0.25f, 0.2f);
        private Color _manaCostDefaultColor;
        private bool  _manaCostColorCaptured;

        public void SetManaSurcharge(int surcharge)
        {
            if (_manaCostLabel == null || CardData == null) return;

            if (!_manaCostColorCaptured)
            {
                _manaCostDefaultColor  = _manaCostLabel.color;
                _manaCostColorCaptured = true;
            }

            bool raised = surcharge > 0;
            _manaCostLabel.text  = (CardData.ManaCost + (raised ? surcharge : 0)).ToString();
            _manaCostLabel.color = raised ? ManaSurchargeColor : _manaCostDefaultColor;
        }

        private void SetupCostBanner(CardSO card)
        {
            // ── Mana ──────────────────────────────────────────────────────────
            if (_manaCostBadge != null) _manaCostBadge.SetActive(true);
            if (_manaCostLabel != null)
            {
                _manaCostLabel.text = card.ManaCost.ToString();
                // Pooled views get reused (enemy reveals, previews) - drop any surcharge red a
                // previous player-hand card left behind.
                if (_manaCostColorCaptured) _manaCostLabel.color = _manaCostDefaultColor;
            }

            // ── HP cost ───────────────────────────────────────────────────────
            bool hasHPCost = card.HPCost > 0;
            if (_hpCostBadge != null) _hpCostBadge.SetActive(hasHPCost);
            if (hasHPCost && _hpCostLabel != null)
                _hpCostLabel.text = $"-{card.HPCost}";

            // ── Damage ────────────────────────────────────────────────────────
            // DOT cards show damage-per-turn × duration rather than flat damage
            bool hasDamage = card.Damage > 0         ||
                             card.CardType == CardType.Combo ||
                             card.CardType == CardType.DOT;

            if (_damageBadge != null) _damageBadge.SetActive(hasDamage);
            if (hasDamage && _damageLabel != null)
                _damageLabel.text = card.CardType == CardType.DOT
                    ? $"{card.DotDamagePerTurn}\u00D7{card.DotDuration}"
                    : card.Damage.ToString();
        }
    }
}