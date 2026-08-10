// Assets/_Game/2. Scripts/Data/Effects/TreasureChestEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/TreasureChest")]
    public class TreasureChestEffectSO : ActionEffectSO
    {
        [SerializeField] private int _cardsToReturn = 3;
        [SerializeField] private int _minCoinReward = 10;
        [SerializeField] private int _maxCoinReward = 15;

        public override void Execute(ICardEffectContext context)
        {
            context.ReturnFromSnapshot(_cardsToReturn);
            GameDebug.Log($"Treasure Chest — returned {_cardsToReturn} cards from original deck");

            if (_maxCoinReward > 0)
            {
                int coinReward = UnityEngine.Random.Range(_minCoinReward, _maxCoinReward + 1);
                context.AddCoins(coinReward);
                GameDebug.Log($"Treasure Chest — found {coinReward} coins");
            }

            if (UnityEngine.Random.value >= 0.5f)
            {
                context.AddPlayerMaxMana(1);
                GameDebug.Log("Treasure Chest — coin toss won: +1 max mana");
            }
            else
            {
                GameDebug.Log("Treasure Chest — coin toss lost: no mana bonus");
            }

            context.DrawOneCard();
        }
    }
}