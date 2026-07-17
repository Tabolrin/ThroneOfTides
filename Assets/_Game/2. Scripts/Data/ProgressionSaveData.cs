// Assets/_Game/2. Scripts/Data/ProgressionSaveData.cs
using System;

namespace ThroneOfTides.Data
{
    /// <summary>Plain snapshot of ProgressionSO persisted to disk by ProgressionSO.Save/LoadFromDisk.</summary>
    [Serializable]
    public class ProgressionSaveData
    {
        public bool Level1Beaten;
        public bool Level2Beaten;
        public bool Level3Beaten;
    }
}
