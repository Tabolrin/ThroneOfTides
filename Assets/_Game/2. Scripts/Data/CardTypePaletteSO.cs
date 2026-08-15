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
            [Tooltip("Type band color used only by PortCardRow's deck-list row. Card views (CardView, CardInspectView) and PortInventoryCard no longer tint anything with this - they rely on TypeSymbol/the type-name label instead.")]
            public Color  BannerColor = Color.white;
            [Tooltip("Icon displayed inside the type banner - represents the card type category")]
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

        // Returns BannerColor - used only by PortCardRow's deck-list type band
        public Color GetColor(CardType type) => GetVisuals(type).BannerColor;
    }
}