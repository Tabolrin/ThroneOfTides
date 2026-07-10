// Assets/_Game/2. Scripts/Data/LevelRewardSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/LevelReward")]
    public class LevelRewardSO : ScriptableObject
    {
        [Header("Card Rewards")]
        // Guaranteed on win only
        [SerializeField] private List<CardSO> _rewardCards;

        [Header("Coin Rewards — Win")]
        [SerializeField] private int _highHPCoinReward = 30;
        [SerializeField] private int _midHPCoinReward  = 20;
        [SerializeField] private int _lowHPCoinReward  = 10;

        // HP thresholds — High >20, Mid 10-20, Low <10
        private const int HighHPThreshold = 20;
        private const int LowHPThreshold  = 10;

        public IReadOnlyList<CardSO> RewardCards => _rewardCards.AsReadOnly();

        public int GetCoinReward(int remainingHP, bool isWin)
        {
            int baseReward = remainingHP > HighHPThreshold ? _highHPCoinReward :
                remainingHP >= LowHPThreshold  ? _midHPCoinReward  :
                _lowHPCoinReward;

            // Loss: 50% rounded down, no card rewards
            return isWin ? baseReward : Mathf.FloorToInt(baseReward * 0.5f);
        }
    }
}