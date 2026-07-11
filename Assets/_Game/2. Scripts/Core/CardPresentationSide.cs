namespace ThroneOfTides.Core
{
    /// <summary>
    /// Identifies a side relative to whoever played a card, rather than an absolute
    /// Player/Enemy identity. A presentation entry authored as "spawn on the Caster's ship"
    /// works correctly whether the player or the enemy plays that card.
    /// </summary>
    public enum CardPresentationSide
    {
        Caster,
        Opponent
    }
}
