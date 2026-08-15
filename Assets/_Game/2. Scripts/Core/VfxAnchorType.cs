namespace ThroneOfTides.Core
{
    /// <summary>
    /// The kind of world-space anchor point a VFXSpawnPosition marker represents on a ship.
    /// Lives in Core (rather than nested in VFXSpawnPosition under Systems) so both the Data
    /// layer (CardPresentationEntry) and the Systems layer (ShipVfxAnchors, VFXSpawnPosition)
    /// can reference it without creating a circular assembly dependency, since Systems already
    /// depends on Data.
    /// </summary>
    /// <remarks>
    /// Enum order must stay ShipHit, ShipDeck, ShipFront, SeaSurfaceLeft, Sky, SeaSurfaceRight,
    /// SeaSurfaceFarLeft - existing VFXSpawnPosition components already placed in scenes
    /// serialize this as a plain int index. New values must always be appended at the end, never
    /// inserted. (SeaSurface was renamed to SeaSurfaceLeft in place - same ordinal value, so
    /// existing scene data still resolves correctly; only the C# symbol name changed.)
    /// </remarks>
    public enum VfxAnchorType
    {
        ShipHit,
        ShipDeck,
        ShipFront,
        SeaSurfaceLeft,

        /// High above the ship - for effects that strike or fall from overhead (Lightning, Hail Storm).
        Sky,

        /// Right-side mirror of SeaSurfaceLeft. Mostly used as a movement-end target for VFX
        /// (e.g. a thrown/traveling effect that should land past the ship on its right side),
        /// but not exclusively - usable anywhere a right-side sea-level anchor makes sense.
        SeaSurfaceRight,

        /// Further out to the left than SeaSurfaceLeft - for effects that need more travel
        /// distance/room than the near-left anchor gives (e.g. a longer wind-up before closing
        /// in on the ship).
        SeaSurfaceFarLeft
    }
}
