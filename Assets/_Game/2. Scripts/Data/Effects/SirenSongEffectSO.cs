using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/SirenSong")]
    public class SirenSongEffectSO : ActionEffectSO
    {
        public override void Execute(ICardEffectContext context)
        {
            // Marks next attack this turn as unblockable
            // Cleared at end of turn if no attack played
            context.SetSirenActive();
            Debug.Log("Siren Song - next attack is unblockable");
        }
    }
}