// Assets/_Game/2. Scripts/Data/CardTypePaletteSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Data/CardTypePalette")]
    public class CardTypePaletteSO : ScriptableObject
    {
        [System.Serializable]
        public class CardTypeVisuals
        {
            [Tooltip("Background color of the type banner strip at the top of the card")]
            public Color  BannerColor = Color.white;
            [Tooltip("Icon displayed inside the type banner — represents the card type category")]
            public Sprite TypeSymbol;
        }

        [Header("Visuals per Card Type")]
        public CardTypeVisuals WeaponVisuals   = new CardTypeVisuals { BannerColor = new Color(0.20f, 0.40f, 0.90f) };
        public CardTypeVisuals ComboVisuals    = new CardTypeVisuals { BannerColor = new Color(0.90f, 0.75f, 0.00f) };
        public CardTypeVisuals ActionVisuals   = new CardTypeVisuals { BannerColor = new Color(0.20f, 0.70f, 0.30f) };
        public CardTypeVisuals DOTVisuals      = new CardTypeVisuals { BannerColor = new Color(0.70f, 0.20f, 0.20f) };
        public CardTypeVisuals ReactionVisuals = new CardTypeVisuals { BannerColor = new Color(0.45f, 0.10f, 0.65f) };
        public CardTypeVisuals DefaultVisuals  = new CardTypeVisuals { BannerColor = Color.grey };

        // ── Queries ────────────────────────────────────────────────────────────

        public CardTypeVisuals GetVisuals(CardType type) => type switch
        {
            CardType.Weapon   => WeaponVisuals,
            CardType.Combo    => ComboVisuals,
            CardType.Action   => ActionVisuals,
            CardType.DOT      => DOTVisuals,
            CardType.Reaction => ReactionVisuals,
            _                 => DefaultVisuals
        };

        // Returns BannerColor — used by PortCardRow and PortInventoryCard type bands
        public Color GetColor(CardType type) => GetVisuals(type).BannerColor;
    }
}