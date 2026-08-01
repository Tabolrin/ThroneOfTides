// Assets/_Game/2. Scripts/Core/EffectRole.cs
namespace ThroneOfTides.Core
{
    // Stable AI-facing archetype for an action effect, used by CaptainSO to weight cards by
    // role instead of switching on individual CardIds — a new Action card just needs its
    // ActionEffectSO to declare a Role, and every captain's existing per-role weight applies
    // automatically.
    public enum EffectRole
    {
        None,
        Intel,
        Disrupt,
        Draw,
        Heal,
        Defense,
    }
}
