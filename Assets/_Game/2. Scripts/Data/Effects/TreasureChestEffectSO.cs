// Assets/_Game/2. Scripts/Data/Effects/TreasureChestEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/TreasureChest")]
    public class TreasureChestEffectSO : ActionEffectSO
    {
        [SerializeField] private int _cardsToDraw = 2;
        [SerializeField] private int _minCoinReward = 10;
        [SerializeField] private int _maxCoinReward = 15;

        public override void Execute(ICardEffectContext context)
        {
            // Deliberately no discard/deck replenishment here (that's Locker's Return's job) —
            // Treasure Chest is purely a coin + card-advantage + mana-chance payoff.
            if (_maxCoinReward > 0)
            {
                int coinReward = UnityEngine.Random.Range(_minCoinReward, _maxCoinReward + 1);
                context.AddCoins(coinReward);
                GameDebug.Log($"Treasure Chest — found {coinReward} coins");
                if (context.Caster == DamageTarget.Player)
                    context.LogNote($"Found {coinReward} coins.");
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

            // ignoreHandLimit: true — these draws are a guaranteed part of the card's payoff,
            // not a normal draw, so they shouldn't silently fizzle just because the hand was
            // already full.
            for (int i = 0; i < _cardsToDraw; i++)
                context.DrawOneCard(ignoreHandLimit: true);
        }
    }
}