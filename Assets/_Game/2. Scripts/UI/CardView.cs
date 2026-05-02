using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _cardBackground;
        [SerializeField] private SpriteRenderer _cardArt;
        [SerializeField] private TMP_Text       _nameLabel;
        [SerializeField] private TMP_Text       _damageLabel;

        public CardSO CardData { get; private set; }

        public void Setup(CardSO card)
        {
            CardData               = card;
            _nameLabel.text        = card.Name;
            _damageLabel.text      = card.Damage.ToString();
            _cardArt.sprite        = card.Art;
            _cardBackground.color  = card.CardType == CardType.Combo
                ? new Color(1f, 0.85f, 0f)
                : new Color(0.3f, 0.5f, 1f);
        }

        public Vector3 GetWorldPosition() => transform.position;
    }
}