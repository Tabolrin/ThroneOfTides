// Assets/_Game/2. Scripts/Systems/MatchRewardGranter.cs
using System.Collections.Generic;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    public readonly struct MatchRewardResult
    {
        public readonly int                  Coins;
        public readonly IReadOnlyList<CardSO> RewardCards;

        public MatchRewardResult(int coins, IReadOnlyList<CardSO> rewardCards)
        {
            Coins       = coins;
            RewardCards = rewardCards;
        }
    }

    // Computes and grants match-end rewards (coins, cards, progression). Pulled out of
    // ResultsPanel so the reward calculation/mutation is a plain, testable service rather than
    // living inside a MonoBehaviour whose job should just be displaying the result.
    public static class MatchRewardGranter
    {
        public static MatchRewardResult GrantWin(
            ProgressionSO progression, PlayerInventory inventory,
            LevelRewardSO reward, int levelIndex, int playerHP)
        {
            progression.SetLevelBeaten(levelIndex);

            var rewardCards = new List<CardSO>(reward.RewardCards);
            inventory.AddCards(rewardCards);

            int coins = reward.GetCoinReward(playerHP, isWin: true);
            inventory.AddCoins(coins);

            return new MatchRewardResult(coins, rewardCards);
        }

        public static MatchRewardResult GrantLoss(
            PlayerInventory inventory, LevelRewardSO reward, int playerHP)
        {
            // Loss — coins only at 50%, no card reward.
            int coins = reward.GetCoinReward(playerHP, isWin: false);
            inventory.AddCoins(coins);

            return new MatchRewardResult(coins, System.Array.Empty<CardSO>());
        }
    }
}
