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
    ///
    /// Values are explicit (not implicit-by-position) because CardSO._id serializes the
    /// underlying int, not the name — removing or reordering an unnumbered entry would silently
    /// renumber every entry after it and reinterpret existing CardSO assets as the wrong card.
    /// RamTheHull, BoardingParty, and StolenWind (11, 12, 19) were removed once obsoleted and are
    /// deliberately left as numeric gaps rather than reused, for the same reason.
    /// </summary>
    public enum CardId
    {
        None = 0,

        // Weapons
        Pistol = 1,
        Cannonball = 2,
        ChainShot = 3,
        WhaleRam = 4,
        HailStorm = 5,
        GunpowderBarrel = 6,
        Torch = 7,
        Whirlpool = 8,
        Lightning = 9,
        Kraken = 10,

        // Actions
        ReconParrot = 13,
        TreasureChest = 14,
        HighSpirits = 15,
        LockersReturn = 16,
        MonkeyGrab = 17,
        Rum = 18,
        EssencePlunder = 20,
        SirenSong = 21,

        // Reactions
        DeadMansTurn = 22,
        CounterGale = 23,

        // Appended at the end (not inserted alphabetically/by category) so it doesn't shift the
        // int values of every card defined after it — those ints are already serialized into
        // existing CardSO assets as _id.
        TidalWave = 24
    }
}
