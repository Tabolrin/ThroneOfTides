// Assets/_Game/2. Scripts/Data/UpgradeSO.cs
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/Upgrade")]
    public class UpgradeSO : ScriptableObject
    {
        [Header("Identity")]
        public Core.UpgradeType Type;
        public string           DisplayName;
        [TextArea(2, 4)]
        public string           Description;
        public Sprite           Icon;

        [Header("Levels")]
        public int MaxLevel = 3;

        // Value added per upgrade level — index 0 is the value gained at level 1
        public int[] ValuePerLevel = { 1, 1, 1 };

        [Header("Coin cost per level (index 0 = cost to reach level 1)")]
        public int[] CoinCostPerLevel = { 50, 100, 200 };

        // ── Queries ───────────────────────────────────────────────────────────

        public int GetValueAtLevel(int level)
        {
            int total = 0;
            for (int i = 0; i < Mathf.Min(level, ValuePerLevel.Length); i++)
                total += ValuePerLevel[i];
            return total;
        }

        public int GetCoinCostToLevel(int currentLevel)
        {
            if (IsMaxed(currentLevel) || currentLevel >= CoinCostPerLevel.Length) return 0;
            return CoinCostPerLevel[currentLevel];
        }

        public bool IsMaxed(int currentLevel) => currentLevel >= MaxLevel;
    }
}