using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/DeadMansTurn")]
    public class DeadMansTurnEffectSO : ActionEffectSO
    {
        public override void Execute(ICardEffectContext context)
        {
            // Stub - QTE to negate incoming attack, deferred post-prototype
            Debug.Log("Dead Man's Turn - QTE not yet implemented");
        }
    }
}