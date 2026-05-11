using UnityEngine;
using UnityEngine.EventSystems;
using ThroneOfTides.Core;

namespace ThroneOfTides.UI
{
    public class PlayZoneHandler : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            CardView card = eventData.pointerDrag?.GetComponent<CardView>();
            if (card == null || card.CardData == null) return;

            Debug.Log($"Card played: {card.CardData.Name}");
            GameEventBus.FireCardPlayed(card.CardData);
        }
    }
}