// Assets/_Game/2. Scripts/Data/Effects/TreasureChestEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/TreasureChest")]
    public class TreasureChestEffectSO : ActionEffectSO
    {
        [SerializeField] private int _cardsToDraw = 3;

        public override void Execute(ICardEffectContext context)
        {
            // Draw 3 cards (secondary draws — do not consume the turn's normal draw allowance)
            int drawn = 0;
            for (int i = 0; i < _cardsToDraw; i++)
            {
                if (context.DrawOneCard()) drawn++;
            }
            Debug.Log($"Treasure Chest — drew {drawn} card(s)");

            // TODO: prompt player to discard the same number of cards they drew
            // Requires a discard-choice UI pass (player selects which cards to put back)
        }
    }
}