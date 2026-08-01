using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/MonkeyGrab")]
    public class MonkeyGrabEffectSO : ActionEffectSO<IHandEffects>
    {
        protected override void Execute(IHandEffects context)
        {
            context.StealFromEnemyHand();
            GameDebug.Log("Monkey Grab - stole 1 card from enemy hand");
        }
    }
}