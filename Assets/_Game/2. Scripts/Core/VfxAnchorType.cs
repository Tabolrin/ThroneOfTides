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
    /// Enum order must stay ShipHit, ShipDeck, ShipFront, SeaSurface, Sky — existing
    /// VFXSpawnPosition components already placed in scenes serialize this as a plain int index.
    /// New values must always be appended at the end, never inserted.
    /// </remarks>
    public enum VfxAnchorType
    {
        ShipHit,
        ShipDeck,
        ShipFront,
        SeaSurface,

        /// High above the ship — for effects that strike or fall from overhead (Lightning, Hail Storm).
        Sky
    }
}
