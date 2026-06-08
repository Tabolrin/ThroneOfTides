// Assets/_Game/2. Scripts/Data/Effects/TreasureChestEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/TreasureChest")]
    public class TreasureChestEffectSO : ActionEffectSO
    {
        [SerializeField] private int _cardsToReturn = 3;

        public override void Execute(ICardEffectContext context)
        {
            // Step 1 — return cards from original deck snapshot into runtime deck
            context.ReturnFromSnapshot(_cardsToReturn);
            Debug.Log($"Treasure Chest — returned {_cardsToReturn} cards from original deck");

            // Step 2 — 50/50 chance of +1 max mana
            if (UnityEngine.Random.value >= 0.5f)
            {
                context.AddPlayerMaxMana(1);
                Debug.Log("Treasure Chest — coin toss won: +1 max mana");
            }
            else
            {
                Debug.Log("Treasure Chest — coin toss lost: no mana bonus");
            }

            // Step 3 — draw 1 (secondary draw — does not consume turn draw)
            // TODO: route through TurnCoordinator.TryDrawCardSecondary
            // Needs an event or callback added to ICardEffectContext
            Debug.Log("Treasure Chest — secondary draw pending wiring");
        }
    }
}