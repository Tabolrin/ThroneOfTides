using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/ReconParrot")]
    public class ReconParrotEffectSO : ActionEffectSO
    {
        [SerializeField] private int _cardsToReveal = 3;

        public override void Execute(ICardEffectContext context)
        {
            // UI layer subscribes to display revealed cards - wired in future step
            var enemyHand = context.GetEnemyHand();
            Debug.Log($"Recon Parrot - enemy has {enemyHand.Count} cards");
        }
    }
}