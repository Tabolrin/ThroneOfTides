namespace ThroneOfTides.Core
{
    /// <summary>
    /// Stable identity for a specific card, independent of its display name (CardSO.Name).
    /// Renaming a card's flavor text/display name in the Inspector never breaks logic that
    /// keys off this enum — unlike the string comparisons (card.Name == "...") this replaces,
    /// which silently stop matching (no compile error, no runtime error) the moment a card is
    /// renamed. Add a new value here whenever a card needs to be identified by code rather than
    /// by its generic CardType/StatusType (e.g. Kraken's negate-only-by-another-Kraken rule,
    /// Torch's combo-follow-up AI weighting, per-reaction charge routing).
    /// </summary>
    public enum CardId
    {
        None,

        // Weapons
        Pistol,
        Cannonball,
        ChainShot,
        WhaleRam,
        HailStorm,
        GunpowderBarrel,
        Torch,
        Whirlpool,
        Lightning,
        Kraken,

        // Cut/unused — kept so old references (AI weight table) don't dangle. Do not remove.
        RamTheHull,
        BoardingParty,

        // Actions
        ReconParrot,
        TreasureChest,
        HighSpirits,
        LockersReturn,
        MonkeyGrab,
        Rum,
        StolenWind,
        EssencePlunder,
        SirenSong,

        // Reactions
        DeadMansTurn,
        CounterGale
    }
}
