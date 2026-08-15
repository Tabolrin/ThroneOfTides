using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    // Abstract base for all action card effects
    // To add a new action: subclass, create asset, assign to CardSO.ActionEffect
    public abstract class ActionEffectSO : ScriptableObject
    {
        [Tooltip("AI-facing archetype used by CaptainSO to weight this card - see EffectRole.")]
        [SerializeField] private EffectRole _role = EffectRole.None;
        public EffectRole Role => _role;

        public abstract void Execute(ICardEffectContext context);
    }

    // Narrows Execute() to just the capability interface(s) an effect actually needs (e.g.
    // ActionEffectSO<IHealEffects>) instead of the full ICardEffectContext - see the segregated
    // interfaces in Core/ICardEffectContext.cs. Effects that are genuinely multi-concern (Tidal
    // Wave, Treasure Chest, High Spirits) subclass the non-generic ActionEffectSO directly.
    public abstract class ActionEffectSO<TContext> : ActionEffectSO where TContext : class
    {
        public sealed override void Execute(ICardEffectContext context) => Execute((TContext)context);
        protected abstract void Execute(TContext context);
    }
}