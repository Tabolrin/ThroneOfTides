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

        [Header("Cost per level (index 0 = cost to reach level 1)")]
        public int[] RumCostPerLevel        = { 5,  10, 20 };
        public int[] ShipwreckCostPerLevel  = { 2,  4,  8  };

        // ── Queries ───────────────────────────────────────────────────────────

        // Total additional value accumulated at the given level
        public int GetValueAtLevel(int level)
        {
            int total = 0;
            for (int i = 0; i < Mathf.Min(level, ValuePerLevel.Length); i++)
                total += ValuePerLevel[i];
            return total;
        }

        public int GetRumCostToLevel(int currentLevel)
        {
            if (IsMaxed(currentLevel) || currentLevel >= RumCostPerLevel.Length) return 0;
            return RumCostPerLevel[currentLevel];
        }

        public int GetShipwreckCostToLevel(int currentLevel)
        {
            if (IsMaxed(currentLevel) || currentLevel >= ShipwreckCostPerLevel.Length) return 0;
            return ShipwreckCostPerLevel[currentLevel];
        }

        public bool IsMaxed(int currentLevel) => currentLevel >= MaxLevel;
    }
}