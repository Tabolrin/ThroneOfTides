using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Player")]
        public int   StartingHP       = 30;
        public int   MaxHandSize      = 5;

        [Header("Deck")]
        public int   LowDeckThreshold = 3;

        [Header("Layout")]
        public float CardSpacing      = 1.0f;

        [Header("Enemy")]
        public float EnemyThinkTimeMin = 0.8f;
        public float EnemyThinkTimeMax = 2.2f;
    }
}