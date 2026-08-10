// Assets/_Game/2. Scripts/Data/GameplaySettingsSaveData.cs
using System;

namespace ThroneOfTides.Data
{
    /// <summary>Plain snapshot of GameplaySettingsSO persisted to disk by its Save/LoadFromDisk.</summary>
    [Serializable]
    public class GameplaySettingsSaveData
    {
        public bool  RequireClickToDismissEnemyCard;
        public float EnemyCardAutoDismissDuration;
    }
}
