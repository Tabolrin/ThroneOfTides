// Assets/_Game/2. Scripts/Data/Effects/RumEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/Rum")]
    public class RumEffectSO : ActionEffectSO
    {
        [SerializeField] private int _healAmount = 5;

        public override void Execute(ICardEffectContext context)
        {
            context.HealPlayer(_healAmount);
            Debug.Log($"Rum — healed {_healAmount} HP");
        }
    }
}