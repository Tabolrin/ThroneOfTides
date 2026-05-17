using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/HighSpirits")]
    public class HighSpiritsEffectSO : ActionEffectSO
    {
        // GDD: heals 5 HP
        [SerializeField] private int _healAmount = 5;

        public override void Execute(ICardEffectContext context)
        {
            context.HealPlayer(_healAmount);
            Debug.Log($"High Spirits - healed {_healAmount} HP");
        }
    }
}