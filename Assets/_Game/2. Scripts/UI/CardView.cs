using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class CardView : MonoBehaviour,
        IPointerClickHandler
    {
        [SerializeField] private Image           _cardArt;
        [SerializeField] private Image           _cardBack;
        [SerializeField] private Image           _cardFrame;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _damageLabel;
        [SerializeField] private GameObject      _damageBadge;
        [SerializeField] private GameObject      _cardFront;
        [SerializeField] private Animator        _animator;

        // Assigned by HandLayoutManager - opens inspect overlay on right click
        public static CardInspectView InspectView;

        public CardSO CardData { get; private set; }

        public void Setup(CardSO card)
        {
            CardData = card;

            _cardFront.SetActive(true);
            if (_cardBack != null)
                _cardBack.gameObject.SetActive(false);

            _nameLabel.text = card.Name;

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

            // Frame color represents card type
            if (_cardFrame != null)
            {
                _cardFrame.color = card.CardType switch
                {
                    CardType.Weapon => new Color(0.3f, 0.5f, 1f),
                    CardType.Combo  => new Color(1f, 0.85f, 0f),
                    CardType.Action => new Color(0.3f, 0.8f, 0.4f),
                    CardType.DOT    => new Color(0.8f, 0.3f, 0.3f),
                    _               => Color.white
                };
            }
        }

        public void SetFaceDown()
        {
            CardData = null;
            _cardFront.SetActive(false);
            if (_cardBack != null)
                _cardBack.gameObject.SetActive(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Right click opens inspect view - only for face-up player cards
            if (eventData.button == PointerEventData.InputButton.Right
                && CardData != null
                && InspectView != null)
            {
                InspectView.Show(CardData);
            }
        }
    }
}