using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    // Abstract base for all action card effects
    // To add a new action: subclass, create asset, assign to CardSO.ActionEffect
    public abstract class ActionEffectSO : ScriptableObject
    {
        public abstract void Execute(ICardEffectContext context);
    }
}