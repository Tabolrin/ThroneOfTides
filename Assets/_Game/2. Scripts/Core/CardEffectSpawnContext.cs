using System;
using UnityEngine;
using UnityEngine.UI;

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

        /// <summary>
        /// Persistent scene-level ParticleSystem reused by effects that need one (e.g. Siren
        /// Song's music notes) instead of spawning/destroying their own instance every play.
        /// Null for effects that don't need it.
        /// </summary>
        public readonly ParticleSystem MusicNoteParticles;

        /// <summary>Persistent scene-level ParticleSystem for Lightning's strike burst.</summary>
        public readonly ParticleSystem LightningStrikeParticles;

        /// <summary>Persistent full-screen scene Image used for Lightning's whiteout flash.</summary>
        public readonly Image WhiteoutImage;

        /// <summary>Persistent scene-level ParticleSystem for Hail Storm's falling hail.</summary>
        public readonly ParticleSystem HailParticles;

        public CardEffectSpawnContext(
            Transform casterAnchor,
            Transform opponentAnchor,
            RectTransform gameCanvas,
            Camera gameCamera,
            ICard card,
            CardCasterFilter caster,
            Action<Vector3> playSfx,
            ParticleSystem musicNoteParticles = null,
            ParticleSystem lightningStrikeParticles = null,
            Image whiteoutImage = null,
            ParticleSystem hailParticles = null)
        {
            CasterAnchor = casterAnchor;
            OpponentAnchor = opponentAnchor;
            GameCanvas = gameCanvas;
            GameCamera = gameCamera;
            Card = card;
            Caster = caster;
            PlaySfx = playSfx;
            MusicNoteParticles = musicNoteParticles;
            LightningStrikeParticles = lightningStrikeParticles;
            WhiteoutImage = whiteoutImage;
            HailParticles = hailParticles;
        }
    }
}
