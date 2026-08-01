// Assets/_Game/2. Scripts/Data/Effects/DeadMansTurnEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/DeadMansTurn")]
    public class DeadMansTurnEffectSO : ActionEffectSO<IReactionChargeEffects>
    {
        protected override void Execute(IReactionChargeEffects context)
        {
            context.AddDeadMansTurnCharge();
            GameDebug.Log("Dead Man's Turn — reaction charge added");
        }
    }
}