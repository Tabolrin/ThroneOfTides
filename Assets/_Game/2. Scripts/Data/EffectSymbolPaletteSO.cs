// Assets/_Game/2. Scripts/Data/EffectSymbolPaletteSO.cs
using System;
using System.Collections.Generic;
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    /// <summary>
    /// Maps each ShipStatusType to the icon sprite used for both the world-space
    /// ShipStatusIndicator and the ActiveEffectsBar badge. One shared asset for the whole game.
    /// </summary>
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/EffectSymbolPalette")]
    public class EffectSymbolPaletteSO : ScriptableObject
    {
        [Serializable]
        public class StatusIconEntry
        {
            [Tooltip("Which status this icon represents.")]
            public ShipStatusType Type;

            [Tooltip("Icon sprite shown on the world-space ship indicator and the Active Effects Bar badge.")]
            public Sprite Icon;
        }

        [Tooltip("One entry per ShipStatusType that should have a visible icon.")]
        [SerializeField] private List<StatusIconEntry> _icons = new List<StatusIconEntry>();

        public Sprite GetIcon(ShipStatusType type)
        {
            foreach (var entry in _icons)
            {
                if (entry.Type == type) return entry.Icon;
            }
            return null;
        }
    }
}
