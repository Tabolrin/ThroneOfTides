using System;
using UnityEngine;

namespace ThroneOfTides.Core
{
    /// <summary>
    /// Everything a spawned card-play effect prefab needs to position and orient itself.
    /// Passed once via ICardPlayEffect.Initialize. Effects that need to animate between ships
    /// (e.g. a mana-pull arc) can use both anchors without the spawner needing any per-effect
    /// special-case knowledge.
    /// </summary>
    public readonly struct CardEffectSpawnContext
    {
        public readonly Transform CasterAnchor;
        public readonly Transform OpponentAnchor;
        public readonly RectTransform GameCanvas;
        public readonly Camera GameCamera;
        public readonly ICard Card;

        /// <summary>
        /// The absolute side that played the card (always Player or Enemy — never Any).
        /// Lets an effect script make absolute-side decisions (e.g. mirroring a sprite)
        /// in addition to the already-relative-resolved CasterAnchor/OpponentAnchor transforms.
        /// </summary>
        public readonly CardCasterFilter Caster;

        /// <summary>
        /// Plays this entry's configured SFX cue at the given world position. Self-driving
        /// effects call this themselves at the moment that actually matters (e.g. on impact)
        /// instead of the spawner firing it immediately at spawn time, which would desync
        /// audio from a multi-beat sequence.
        /// </summary>
        public readonly Action<Vector3> PlaySfx;

        public CardEffectSpawnContext(
            Transform casterAnchor,
            Transform opponentAnchor,
            RectTransform gameCanvas,
            Camera gameCamera,
            ICard card,
            CardCasterFilter caster,
            Action<Vector3> playSfx)
        {
            CasterAnchor = casterAnchor;
            OpponentAnchor = opponentAnchor;
            GameCanvas = gameCanvas;
            GameCamera = gameCamera;
            Card = card;
            Caster = caster;
            PlaySfx = playSfx;
        }
    }
}
