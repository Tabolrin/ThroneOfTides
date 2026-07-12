namespace ThroneOfTides.Core
{
    [System.Serializable]
    public struct DotEffect
    {
        public DamageTarget Target;
        public int          DamagePerTurn;
        public int          TurnsRemaining;

        /// Which card/status this DOT came from (e.g. Whirlpool vs Hail Storm) — lets the
        /// ship-status indicator system show the correct icon instead of a generic "DOT" mark.
        public ShipStatusType Source;

        public DotEffect(DamageTarget target, int damagePerTurn, int turnsRemaining, ShipStatusType source = ShipStatusType.None)
        {
            Target         = target;
            DamagePerTurn  = damagePerTurn;
            TurnsRemaining = turnsRemaining;
            Source         = source;
        }
    }
}