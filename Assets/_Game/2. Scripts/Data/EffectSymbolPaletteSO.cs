// Assets/_Game/2. Scripts/Data/EffectSymbolPaletteSO.cs
using System.Collections.Generic;
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    // Icon lookup for ActiveEffectsBar/ShipStatusIndicator — one shared asset maps each
    // ShipStatusType to the sprite its badge/world indicator should show, plus a short
    // hover-tooltip description for the badge.
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/EffectSymbolPalette")]
    public class EffectSymbolPaletteSO : ScriptableObject
    {
        [System.Serializable]
        public class IconEntry
        {
            public ShipStatusType Type;
            public Sprite         Icon;
            [TextArea]
            public string         Description;
        }

        [SerializeField] private List<IconEntry> _icons = new List<IconEntry>();

        public Sprite GetSprite(ShipStatusType type)
        {
            var entry = _icons.Find(e => e.Type == type);
            return entry?.Icon;
        }

        public string GetDescription(ShipStatusType type)
        {
            var entry = _icons.Find(e => e.Type == type);
            return entry?.Description;
        }
    }
}
