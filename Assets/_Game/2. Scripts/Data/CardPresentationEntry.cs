// Assets/_Game/2. Scripts/Data/CardPresentationEntry.cs
using System;
using MoreMountains.Tools;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Data
{
    /// <summary>
    /// An SFX cue: plays one specific song from a shared MMSMPlaylist as a one-shot through
    /// MMSoundManager. Leaving Playlist unset means no SFX for the entry. Leaving Song Name
    /// blank picks a random song from the whole playlist instead — useful only if the playlist
    /// is dedicated to this one effect rather than shared across many cards.
    /// </summary>
    [Serializable]
    public class CardSfxCue
    {
        [Tooltip("The shared SFX playlist to pull a clip from.")]
        [SerializeField] private MMSMPlaylist _playlist;

        [Tooltip("Name of the specific song within the Playlist to play (matches that song's Name field). Leave blank to pick a random song from the whole playlist instead — only sensible for a playlist dedicated to one effect.")]
        [SerializeField] private string _songName;

        [Tooltip("Multiplies the song's own configured volume.")]
        [SerializeField] private float _volumeMultiplier = 1f;

        [Tooltip("Multiplies the song's own configured pitch.")]
        [SerializeField] private float _pitchMultiplier = 1f;

        public MMSMPlaylist Playlist => _playlist;
        public string SongName => _songName;
        public float VolumeMultiplier => _volumeMultiplier;
        public float PitchMultiplier => _pitchMultiplier;
    }

    /// <summary>
    /// One spawn instruction for a card's play presentation: what to spawn, where, for which
    /// caster, and what SFX to play alongside it. A card can carry several of these — e.g. one
    /// per caster side, or several anchors for effects that touch both ships.
    /// </summary>
    [Serializable]
    public class CardPresentationEntry
    {
        [Tooltip("Which side must have played the card for this entry to trigger.")]
        [SerializeField] private CardCasterFilter _casterFilter = CardCasterFilter.Any;

        [Tooltip("World-space sprite-only prefab with its own logic, or a paired UI sprite + world particle.")]
        [SerializeField] private CardPresentationMode _presentationMode = CardPresentationMode.WorldSpriteOnly;

        [Tooltip("Whose ship to spawn on, relative to whoever played the card.")]
        [SerializeField] private CardPresentationSide _anchorSide = CardPresentationSide.Caster;

        [Tooltip("Which marker on that ship to spawn at.")]
        [SerializeField] private VfxAnchorType _positionType = VfxAnchorType.ShipHit;

        [Tooltip("Root prefab for this entry. Used in both presentation modes.")]
        [SerializeField] private GameObject _spritePrefab;

        [Tooltip("World-space particle prefab. Only used when Presentation Mode is Ui Sprite With World Particle.")]
        [SerializeField] private GameObject _worldParticlePrefab;

        [Tooltip("Sound played alongside this entry's spawn. Leave Playlist unset for no sound.")]
        [SerializeField] private CardSfxCue _sfx = new CardSfxCue();

        [Tooltip("Seconds before auto-destroying the spawned prefab(s). Ignored if Sprite Prefab implements ICardPlayEffect — that signals completion itself instead.")]
        [SerializeField] private float _lifetime = 2f;

        public CardCasterFilter CasterFilter => _casterFilter;
        public CardPresentationMode PresentationMode => _presentationMode;
        public CardPresentationSide AnchorSide => _anchorSide;
        public VfxAnchorType PositionType => _positionType;
        public GameObject SpritePrefab => _spritePrefab;
        public GameObject WorldParticlePrefab => _worldParticlePrefab;
        public CardSfxCue Sfx => _sfx;
        public float Lifetime => _lifetime;

        /// <summary>
        /// Whether this entry should fire given who actually played the card this time.
        /// </summary>
        /// <param name="actualCaster">Always Player or Enemy — never Any.</param>
        public bool MatchesCaster(CardCasterFilter actualCaster)
        {
            return _casterFilter == CardCasterFilter.Any || _casterFilter == actualCaster;
        }
    }
}
