using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/ReconParrot")]
    public class ReconParrotEffectSO : ActionEffectSO<IHandEffects>
    {
        [SerializeField] private int _cardsToReveal = 3;

        protected override void Execute(IHandEffects context)
        {
            var enemyHand   = context.GetEnemyHand();
            int revealCount = Mathf.Min(_cardsToReveal, enemyHand.Count);

            // TODO - wire ReconParrot reveal UI when inspect system is extended
            for (int i = 0; i < revealCount; i++)
                GameDebug.Log($"Recon Parrot revealed: {enemyHand[i].Name}");
        }
    }
}