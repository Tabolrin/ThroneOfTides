using System;
using System.Collections.Generic;
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

        /// <summary>Persistent scene-level ParticleSystem for Gunpowder Barrel's dust trail.</summary>
        public readonly ParticleSystem GunpowderDustParticles;

        /// <summary>
        /// Reveals the opponent ship's persistent world-space status indicator for the given
        /// type immediately (e.g. the Gunpowder-barrel decoration) — for thrown-projectile
        /// effects that want the reveal timed to their own impact frame instead of firing the
        /// instant GameState registers the effect. No-op if no such indicator is placed.
        /// </summary>
        public readonly Action<ShipStatusType> RevealOpponentStatusIndicator;

        /// <summary>
        /// True if the opponent ship's status indicator for the given type is currently visible
        /// (e.g. Torch checking whether Gunpowder is active before deciding to explode).
        /// </summary>
        public readonly Func<ShipStatusType, bool> IsOpponentStatusVisible;

        /// <summary>
        /// Resolves an anchor point on whichever ship was this play's explicit target (e.g. Tidal
        /// Wave's player-chosen ship) — for self-driving effects that need a second point beyond
        /// their own spawn position, such as animating a projectile from spawn to a hit point.
        /// Null if this card has no explicit target.
        /// </summary>
        public readonly Func<VfxAnchorType, Transform> GetExplicitTargetAnchor;

        /// <summary>
        /// Resolves an anchor point of any VfxAnchorType on the opponent's ship, independent of
        /// the entry's own PositionType — for always-targets-opponent effects (e.g. Whale Ram)
        /// that spawn at one anchor (say, SeaSurface) but need to travel to a different one
        /// (ShipHit) without requiring target selection. Unlike GetExplicitTargetAnchor, this is
        /// always available since it doesn't depend on the player having chosen a target.
        /// </summary>
        public readonly Func<VfxAnchorType, Transform> GetOpponentAnchor;

        /// <summary>
        /// Starts holding the explicit target ship's Gunpowder sprite exactly as it currently
        /// looks, ignoring the game state's instant clear, until EndExplicitTargetGunpowderHold
        /// is called — for effects (e.g. Tidal Wave) that want the powdered-look-to-clean swap
        /// timed to their own sequence instead of snapping the moment the card resolves.
        /// No-op if this card has no explicit target or that ship has no Gunpowder visual.
        /// </summary>
        public readonly Action BeginExplicitTargetGunpowderHold;

        /// <summary>Releases a hold started by BeginExplicitTargetGunpowderHold, applying the real current state.</summary>
        public readonly Action EndExplicitTargetGunpowderHold;

        /// <summary>
        /// Generic anchor resolver — given a side (Caster/Opponent/ExplicitTarget) and an anchor
        /// type, returns that ship's Transform for it (null for ExplicitTarget if this play had
        /// no chosen target). Lets a VFX controller expose its own Inspector-configurable
        /// start/end points instead of being hardcoded to whatever the CardPresentationEntry's
        /// own AnchorSide/PositionType happened to spawn it at.
        /// </summary>
        public readonly Func<CardPresentationSide, VfxAnchorType, Transform> GetAnchor;

        /// <summary>
        /// Resolves an anchor point on the PLAYER's ship specifically, regardless of which side
        /// actually cast the card — for effects meant to appear "in the space between the two
        /// ships" (e.g. Treasure Chest's coin burst), which should stay pinned to the player's
        /// side rather than flipping to the enemy's when the enemy plays the card.
        /// </summary>
        public readonly Func<VfxAnchorType, Transform> GetPlayerAnchor;

        /// <summary>
        /// Shows the enemy-hand-reveal modal (Recon Parrot) with the given cards, invoking the
        /// callback once the player dismisses it. Kept as a generic delegate (not a direct
        /// EnemyHandRevealPanel reference) since Core cannot depend on the UI assembly — see
        /// CardPresentationPlayer for the concrete wiring.
        /// </summary>
        public readonly Action<IReadOnlyList<ICard>, Action> ShowEnemyHandReveal;

        /// <summary>
        /// Suppresses the next generic "+N mana" popup that OnPlayerManaChanged/OnEnemyManaChanged
        /// would otherwise fire the instant CombatResolver's synchronous mana change actually
        /// happens — for effects (e.g. Essence Plunder) that want to show their own "+N" popup
        /// timed to their own animation instead of the instant the real state changes.
        /// </summary>
        public readonly Action SuppressManaGainPopup;

        /// <summary>Spawns a deferred "+N" mana popup at the given world position, through the same queue as every other floating number.</summary>
        public readonly Action<int, Vector3> SpawnManaGainedNumber;

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
            ParticleSystem hailParticles = null,
            Action<ShipStatusType> revealOpponentStatusIndicator = null,
            Func<ShipStatusType, bool> isOpponentStatusVisible = null,
            ParticleSystem gunpowderDustParticles = null,
            Func<VfxAnchorType, Transform> getExplicitTargetAnchor = null,
            Action beginExplicitTargetGunpowderHold = null,
            Action endExplicitTargetGunpowderHold = null,
            Func<VfxAnchorType, Transform> getOpponentAnchor = null,
            Func<CardPresentationSide, VfxAnchorType, Transform> getAnchor = null,
            Func<VfxAnchorType, Transform> getPlayerAnchor = null,
            Action<IReadOnlyList<ICard>, Action> showEnemyHandReveal = null,
            Action suppressManaGainPopup = null,
            Action<int, Vector3> spawnManaGainedNumber = null)
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
            RevealOpponentStatusIndicator = revealOpponentStatusIndicator;
            IsOpponentStatusVisible = isOpponentStatusVisible;
            GunpowderDustParticles = gunpowderDustParticles;
            GetExplicitTargetAnchor = getExplicitTargetAnchor;
            BeginExplicitTargetGunpowderHold = beginExplicitTargetGunpowderHold;
            EndExplicitTargetGunpowderHold = endExplicitTargetGunpowderHold;
            GetOpponentAnchor = getOpponentAnchor;
            GetAnchor = getAnchor;
            GetPlayerAnchor = getPlayerAnchor;
            ShowEnemyHandReveal = showEnemyHandReveal;
            SuppressManaGainPopup = suppressManaGainPopup;
            SpawnManaGainedNumber = spawnManaGainedNumber;
        }
    }
}
