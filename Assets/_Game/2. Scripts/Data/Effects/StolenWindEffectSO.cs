// Assets/_Game/2. Scripts/Data/Effects/StolenWindEffectSO.cs
using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/StolenWind")]
    public class StolenWindEffectSO : ActionEffectSO<IManaEffects>
    {
        [SerializeField] private int _manaToSteal = 1;

        protected override void Execute(IManaEffects context)
        {
            // HP cost is paid by CombatResolver before Execute is called,
            // using CardSO.HPCost — no HP deduction here
            context.StealEnemyMana(_manaToSteal);
            GameDebug.Log($"Stolen Wind — stole {_manaToSteal} mana from enemy");
        }
    }
}