namespace ThroneOfTides.Core
{
    /// <summary>
    /// A persistent per-ship status that gets a world-space indicator sprite (via
    /// ShipStatusIndicator) and a HUD badge (via ActiveEffectsBar), both driven by the single
    /// GameEventBus.OnShipStatusCountChanged event. Add a new value here to introduce a new
    /// status without touching either presentation script.
    /// </summary>
    public enum ShipStatusType
    {
        None,
        Gunpowder,
        Whirlpool,
        HailStorm,
        HighSpirits,
        SirenSong
    }
}
