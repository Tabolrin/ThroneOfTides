using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class CardView : MonoBehaviour
    {
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