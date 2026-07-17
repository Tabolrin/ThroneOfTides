// Assets/_Game/2. Scripts/Data/Effects/CounterGaleEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/CounterGale")]
    public class CounterGaleEffectSO : ActionEffectSO
    {
        public override void Execute(ICardEffectContext context)
        {
            context.AddCounterGaleCharge();
            Debug.Log("Counter Gale — reaction charge added");
        }
    }
}
