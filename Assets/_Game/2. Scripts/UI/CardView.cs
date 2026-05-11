using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class CardView : MonoBehaviour
    {
<<<<<<< HEAD
        [SerializeField] private Image            _cardBackground;
        [SerializeField] private Image            _cardArt;
        [SerializeField] private TextMeshProUGUI  _nameLabel;
        [SerializeField] private TextMeshProUGUI  _damageLabel;

        public CardSO       CardData { get; private set; }
        public RectTransform Rect    { get; private set; }

        private void Awake()
        {
            Rect = (RectTransform)transform;
        }

        public void Setup(CardSO card)
        {
            CardData              = card;
            _nameLabel.text       = card.Name;
            _damageLabel.text     = card.Damage.ToString();
            _cardArt.sprite       = card.Art;
            _cardBackground.color = card.CardType == CardType.Combo
                ? new Color(1f, 0.85f, 0f)
                : new Color(0.3f, 0.5f, 1f);
<<<<<<< HEAD

            if (_animator != null && card.CardArtAnimator != null)
                _animator.runtimeAnimatorController = card.CardArtAnimator;
=======
        [SerializeField] private Image           _cardBackground;
        [SerializeField] private Image           _cardArt;
        [SerializeField] private Image           _cardTypeSymbol;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _damageLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private GameObject      _damageBadge;
        [SerializeField] private Animator        _animator;

        public CardSO CardData { get; private set; }

        public void Setup(CardSO card)
        {
            CardData = card;

            _nameLabel.text        = card.Name;
            _descriptionLabel.text = card.Description;

            // Only show damage badge if card deals damage
            bool hasDamage = card.Damage > 0 || card.CardType == CardType.Combo || card.CardType == CardType.DOT;
            _damageBadge.SetActive(hasDamage);
            if (hasDamage)
                _damageLabel.text = card.CardType == CardType.DOT
                    ? $"{card.DotDamagePerTurn}x{card.DotDuration}"
                    : card.Damage.ToString();

            if (card.Art != null)
                _cardArt.sprite = card.Art;

            if (card.CardTypeSymbol != null)
                _cardTypeSymbol.sprite = card.CardTypeSymbol;

            _cardBackground.color = card.CardType switch
            {
                CardType.Weapon => new Color(0.3f, 0.5f, 1f),
                CardType.Combo  => new Color(1f, 0.85f, 0f),
                CardType.Action => new Color(0.3f, 0.8f, 0.4f),
                CardType.DOT    => new Color(0.8f, 0.3f, 0.3f),
                _               => Color.white
            };
>>>>>>> parent of d5a8aee (Merge branch 'claude/lucid-williams-9bfc13' into Tests)
=======
>>>>>>> parent of 9c6ae3c (Merge branch 'claude/lucid-williams-9bfc13' into Tests)
        }

        public void SetFaceDown()
        {
            _nameLabel.text        = "";
            _descriptionLabel.text = "";
            _damageBadge.SetActive(false);
            _cardBackground.color  = new Color(0.2f, 0.2f, 0.3f);
        }
    }
}