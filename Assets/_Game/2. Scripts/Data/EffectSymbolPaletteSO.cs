// Assets/_Game/2. Scripts/Data/EffectSymbolPaletteSO.cs
using System.Collections.Generic;
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    // Icon lookup for ActiveEffectsBar/ShipStatusIndicator — one shared asset maps each
    // ShipStatusType to the sprite its badge/world indicator should show.
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/EffectSymbolPalette")]
    public class EffectSymbolPaletteSO : ScriptableObject
    {
        [System.Serializable]
        public class IconEntry
        {
            public ShipStatusType Type;
            public Sprite         Icon;
        }

        [SerializeField] private List<IconEntry> _icons = new List<IconEntry>();

        public Sprite GetSprite(ShipStatusType type)
        {
            var entry = _icons.Find(e => e.Type == type);
            return entry?.Icon;
        }
    }
}
