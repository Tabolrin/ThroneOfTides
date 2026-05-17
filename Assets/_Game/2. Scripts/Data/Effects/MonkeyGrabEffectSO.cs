using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/MonkeyGrab")]
    public class MonkeyGrabEffectSO : ActionEffectSO
    {
        public override void Execute(ICardEffectContext context)
        {
            context.StealFromEnemyHand();
            Debug.Log("Monkey Grab - stole 1 card from enemy hand");
        }
    }
}