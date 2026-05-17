using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/DeadMansTurn")]
    public class DeadMansTurnEffectSO : ActionEffectSO
    {
        public override void Execute(ICardEffectContext context)
        {
            // Registers Dead Man's Turn as active - GameManager checks this
            // during enemy attack and prompts player to use or decline
            context.SetDeadMansTurnActive();
            Debug.Log("Dead Man's Turn - will prompt on next enemy attack");
        }
    }
}