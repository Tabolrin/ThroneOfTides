using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/HighSpirits")]
    public class HighSpiritsEffectSO : ActionEffectSO
    {
        [SerializeField] private int _healAmount = 10;

        public override void Execute(ICardEffectContext context)
        {
            context.HealPlayer(_healAmount);
        }
    }
}