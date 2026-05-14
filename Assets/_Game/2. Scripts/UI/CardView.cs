using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Image           _cardArt;
        [SerializeField] private Image           _cardTypeSymbol;
        [SerializeField] private Image           _cardBack;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _damageLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private GameObject      _damageBadge;
        [SerializeField] private Animator        _animator;

        public CardSO CardData { get; private set; }

        public void Setup(CardSO card)
        {
            CardData = card;

            // Hide card back, show card front
            if (_cardBack != null)
                _cardBack.gameObject.SetActive(false);

            _nameLabel.text        = card.Name;
            _descriptionLabel.text = card.Description;

            // Only show damage badge if card deals damage
            bool hasDamage = card.Damage > 0 ||
                             card.CardType == CardType.Combo ||
                             card.CardType == CardType.DOT;
            _damageBadge.SetActive(hasDamage);

            if (hasDamage)
                _damageLabel.text = card.CardType == CardType.DOT
                    ? $"{card.DotDamagePerTurn}x{card.DotDuration}"
                    : card.Damage.ToString();

            if (card.Art != null)
                _cardArt.sprite = card.Art;

            if (card.CardTypeSymbol != null)
                _cardTypeSymbol.sprite = card.CardTypeSymbol;
        }

        public void SetFaceDown()
        {
            CardData = null;

            // Show card back only
            if (_cardBack != null)
                _cardBack.gameObject.SetActive(true);

            _nameLabel.text        = "";
            _descriptionLabel.text = "";
            _damageBadge.SetActive(false);

            if (_cardArt != null)
                _cardArt.sprite = null;
        }
    }
}