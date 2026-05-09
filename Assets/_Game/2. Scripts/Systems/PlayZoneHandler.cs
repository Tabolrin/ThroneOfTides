using UnityEngine;
using UnityEngine.EventSystems;
using ThroneOfTides.Data;

namespace ThroneOfTides.UI
{
    // Receives cards dropped into the play zone
    public class PlayZoneHandler : MonoBehaviour, IDropHandler
    {
        // Fired when a valid card is dropped - GameManager subscribes
        public static System.Action<CardSO> OnCardPlayed;

        public void OnDrop(PointerEventData eventData)
        {
            CardView card = eventData.pointerDrag?.GetComponent<CardView>();
            if (card == null || card.CardData == null) return;

            Debug.Log($"Card played: {card.CardData.Name}");
            OnCardPlayed?.Invoke(card.CardData);
        }
    }
}