// Assets/_Game/2. Scripts/Core/ReactionType.cs
namespace ThroneOfTides.Core
{
    // Distinguishes reaction card subtypes within the Reaction CardType.
    // Used by GameEventBus and ActiveEffectsBar to track charges per reaction.
    public enum ReactionType { DeadMansTurn, BloodForBlood }
}