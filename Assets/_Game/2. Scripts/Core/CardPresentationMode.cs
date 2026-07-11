namespace ThroneOfTides.Core
{
    /// <summary>
    /// The two authoring shapes a card-play presentation entry can take.
    /// </summary>
    public enum CardPresentationMode
    {
        /// A single self-contained world-space sprite prefab (optionally with its own
        /// animation/tween logic via ICardPlayEffect).
        WorldSpriteOnly,

        /// A UI-canvas sprite prefab paired with a separate world-space particle prefab.
        UiSpriteWithWorldParticle
    }
}
