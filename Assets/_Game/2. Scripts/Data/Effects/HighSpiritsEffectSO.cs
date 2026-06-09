// Assets/_Game/2. Scripts/Data/Effects/HighSpiritsEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/HighSpirits")]
    public class HighSpiritsEffectSO : ActionEffectSO
    {
        [SerializeField] private int _healAmount = 5;

        public override void Execute(ICardEffectContext context)
        {
            context.HealPlayer(_healAmount);
            Debug.Log($"High Spirits — healed {_healAmount} HP");
        }
    }
}