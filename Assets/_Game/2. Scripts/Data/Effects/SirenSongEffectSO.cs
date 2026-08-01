using ThroneOfTides.Core;
using UnityEngine;

namespace ThroneOfTides.Data
{
    [CreateAssetMenu(menuName = "ThroneOfTides/Effects/SirenSong")]
    public class SirenSongEffectSO : ActionEffectSO<IStatusEffects>
    {
        protected override void Execute(IStatusEffects context)
        {
            // Marks next attack this turn as unblockable
            // Cleared at end of turn if no attack played
            context.SetSirenActive();
            GameDebug.Log("Siren Song - next attack is unblockable");
        }
    }
}