// Assets/_Game/2. Scripts/Data/Effects/DeadMansTurnEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/DeadMansTurn")]
    public class DeadMansTurnEffectSO : ActionEffectSO
    {
        public override void Execute(ICardEffectContext context)
        {
            context.AddDeadMansTurnCharge();
            Debug.Log("Dead Man's Turn — reaction charge added");
        }
    }
}