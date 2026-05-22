namespace ThroneOfTides.Core
{
    [System.Serializable]
    public struct DotEffect
    {
        public DamageTarget Target;
        public int          DamagePerTurn;
        public int          TurnsRemaining;

        public DotEffect(DamageTarget target, int damagePerTurn, int turnsRemaining)
        {
            Target         = target;
            DamagePerTurn  = damagePerTurn;
            TurnsRemaining = turnsRemaining;
        }
    }
}