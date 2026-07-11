using System;

namespace ThroneOfTides.Core
{
    /// <summary>
    /// Implement this on a card-play effect prefab's root component when the effect drives its
    /// own animation/tween sequence ("sprite-only prefab with its own logic"). The spawner calls
    /// Initialize once, then waits for Completed before destroying the instance, so each card's
    /// effect script can differ arbitrarily in how it plays without the spawner needing to know.
    /// Prefabs that do NOT implement this are destroyed after a fixed lifetime instead.
    /// </summary>
    public interface ICardPlayEffect
    {
        void Initialize(CardEffectSpawnContext context);

        event Action Completed;
    }
}
