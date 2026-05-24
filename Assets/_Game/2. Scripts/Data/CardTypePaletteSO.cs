using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/CardTypePalette")]
    public class CardTypePaletteSO : ScriptableObject
    {
        [Header("Card Frame Colours")]
        public Color WeaponColor = new Color(0.3f, 0.5f, 1f);
        public Color ComboColor  = new Color(1f, 0.85f, 0f);
        public Color ActionColor = new Color(0.3f, 0.8f, 0.4f);
        public Color DotColor    = new Color(0.8f, 0.3f, 0.3f);
        public Color DefaultColor = Color.white;

        public Color GetColor(CardType type) => type switch
        {
            CardType.Weapon => WeaponColor,
            CardType.Combo  => ComboColor,
            CardType.Action => ActionColor,
            CardType.DOT    => DotColor,
            _               => DefaultColor
        };
    }
}