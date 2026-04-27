using UnityEngine;
using ThroneOfTides.UI;

namespace ThroneOfTides.Systems
{
    public class PlayZoneHandler : MonoBehaviour
    {
        // OnTriggerEnter2D requires a Rigidbody2D on the card (Kinematic)
        private void OnTriggerEnter2D(Collider2D other)
        {
            CardView card = other.GetComponent<CardView>();
            if (card == null) return;

            Debug.Log($"Card played: {card.CardData.Name}");
            // Full card play logic added in Step 7
        }
    }
}