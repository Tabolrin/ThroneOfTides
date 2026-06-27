// Assets/_Game/2. Scripts/Data/Effects/StolenWindEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/StolenWind")]
    public class StolenWindEffectSO : ActionEffectSO
    {
        [SerializeField] private int _manaToSteal = 1;

        public override void Execute(ICardEffectContext context)
        {
            // HP cost is paid by CombatResolver before Execute is called,
            // using CardSO.HPCost — no HP deduction here
            context.StealEnemyMana(_manaToSteal);
            Debug.Log($"Stolen Wind — stole {_manaToSteal} mana from enemy");
        }
    }
}