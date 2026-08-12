// Assets/_Game/2. Scripts/Data/GameConfigSO.cs
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Player")]
        public int   StartingHP          = 30;
        public int   MaxHandSize         = 4;

        [Header("Mana")]
        public int   StartingMaxMana     = 3;

        [Header("Deck")]
        public int   LowDeckThreshold   = 3;
        // Base deck slot capacity before storage upgrades
        public int   BaseStorageCapacity = 20;

        [Header("Layout")]
        public float CardSpacing         = 1.0f;

        [Header("Enemy")]
        public float EnemyThinkTimeMin   = 0.8f;
        public float EnemyThinkTimeMax   = 2.2f;
    }
}