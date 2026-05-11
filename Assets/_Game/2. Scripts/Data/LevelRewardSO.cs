using System;
using UnityEngine;
using System.Collections.Generic;

namespace ThroneOfTides.Data
{
    [Serializable]
    public struct MaterialReward
    {
        public int Rum;
        public int Shipwrecks;
    }

    [CreateAssetMenu(menuName = "ThroneOfTides/Data/LevelReward")]
    public class LevelRewardSO : ScriptableObject
    {
        [Header("Card Rewards")]
        // Guaranteed on win only
        [SerializeField] private List<CardSO> _rewardCards;

        [Header("Material Rewards - Win")]
        [SerializeField] private MaterialReward _highHPReward;
        [SerializeField] private MaterialReward _midHPReward;
        [SerializeField] private MaterialReward _lowHPReward;

        // HP thresholds matching GDD - High >20, Mid 10-20, Low <10
        private const int HighHPThreshold = 20;
        private const int LowHPThreshold  = 10;

        public IReadOnlyList<CardSO> RewardCards => _rewardCards.AsReadOnly();

        public MaterialReward GetMaterialReward(int remainingHP, bool isWin)
        {
            MaterialReward baseReward = remainingHP > HighHPThreshold ? _highHPReward :
                remainingHP >= LowHPThreshold  ? _midHPReward  :
                _lowHPReward;
            if (isWin) return baseReward;

            // Loss: 50% rounded down, no card rewards
            return new MaterialReward
            {
                Rum        = Mathf.FloorToInt(baseReward.Rum        * 0.5f),
                Shipwrecks = Mathf.FloorToInt(baseReward.Shipwrecks * 0.5f)
            };
        }
    }
}