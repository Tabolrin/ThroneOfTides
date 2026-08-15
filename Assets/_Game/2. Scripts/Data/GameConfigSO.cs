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
        [Tooltip("Absolute ceiling a hand can still be pushed past MaxHandSize by a card's own guaranteed bonus draw/gain (e.g. Treasure Chest, Monkey Grab) - never bypassed entirely like the old 'ignore hand limit' behavior did. Once a hand is at this size, further bonus cards are skipped and logged instead of added.")]
        public int   BonusMaxHandSize    = 6;

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