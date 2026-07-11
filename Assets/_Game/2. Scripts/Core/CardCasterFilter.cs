namespace ThroneOfTides.Core
{
    /// <summary>
    /// Gates whether a CardPresentationEntry fires based on the absolute identity of
    /// whoever played the card. Distinct from CardPresentationSide, which is relative
    /// (Caster/Opponent) and used for anchor resolution rather than filtering.
    /// </summary>
    public enum CardCasterFilter
    {
        Any,
        Player,
        Enemy
    }
}
