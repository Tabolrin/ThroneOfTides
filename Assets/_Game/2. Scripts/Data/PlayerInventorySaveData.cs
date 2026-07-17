// Assets/_Game/2. Scripts/Data/PlayerInventorySaveData.cs
using System;
using System.Collections.Generic;
using ThroneOfTides.Core;

namespace ThroneOfTides.Data
{
    /// <summary>Plain snapshot of PlayerInventory persisted to disk by PlayerInventory.Save/LoadFromDisk.</summary>
    [Serializable]
    public class PlayerInventorySaveData
    {
        public int Coins;
        public int HullReinforcementLevel;
        public int ExpandedCargoHoldLevel;
        public int ManaCrystalLevel;
        public List<CardId> CollectionCardIds = new List<CardId>();
    }
}
