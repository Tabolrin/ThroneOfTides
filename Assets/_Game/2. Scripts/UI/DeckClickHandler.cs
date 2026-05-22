using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThroneOfTides.UI
{
    // Player clicks the deck visual to draw a card
    // Attach to DeckImage - requires Image component for raycasting
    [RequireComponent(typeof(Image))]
    public class DeckClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public static System.Action OnDeckClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            OnDeckClicked?.Invoke();
        }
    }
}