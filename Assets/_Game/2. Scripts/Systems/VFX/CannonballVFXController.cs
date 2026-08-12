// Assets/_Game/2. Scripts/Systems/VFX/CannonballVFXController.cs
using System;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using ThroneOfTides.Core;
using ThroneOfTides.Data;
using ThroneOfTides.Systems.VFX;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Cannonball travel-and-impact effect shared by Pistol, Cannon Ball, and Chain Shot (same
    /// controller, tuned per-prefab). Spawns already positioned at the caster's ship
    /// (CardPresentationPlayer places it there), fires the shot SFX, arcs across to the
    /// opponent's ShipHit anchor regardless of which anchor it spawned at — spinning in flight if
    /// configured to (Chain Shot) — then hides itself and plays the impact SFX + a small camera
    /// shake on arrival, plus an explosion prefab if one is assigned (Chain Shot leaves it unset
    /// for a clean hit with no explosion). Implements ICardPlayEffect so CardPresentationPlayer
    /// can host it without knowing any of this internal sequencing.
    /// </summary>
    public class CannonballVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Travel")]
        [Tooltip("Which anchor on the opponent's ship the ball travels to and impacts at, regardless of which anchor this entry originally spawned it at.")]
        [SerializeField] private VfxAnchorType _impactAnchorType = VfxAnchorType.ShipHit;
        [Tooltip("Seconds the cannonball takes to arc from the caster's ship to the target.")]
        [SerializeField] private float _travelDuration = 0.5f;
        [Tooltip("Height added at the midpoint of the travel arc.")]
        [SerializeField] private float _arcHeight = 1.5f;
        [SerializeField] private Ease _travelEase = Ease.Linear;
        [Tooltip("Degrees the sprite spins over the course of the flight. 0 = no spin (Cannonball/Pistol); nonzero for a tumbling projectile (Chain Shot).")]
        [SerializeField] private float _spinDegrees = 0f;

        [Header("Explosion")]
        [Tooltip("Explosion animation prefab, played once on top of the target ship on impact. Leave unset for a clean hit with no explosion (Chain Shot).")]
        [SerializeField] private GameObject _explosionPrefab;
        [Tooltip("Seconds before the spawned explosion instance is destroyed.")]
        [SerializeField] private float _explosionLifetime = 1.5f;

        [Header("SFX")]
        [Tooltip("Played the instant the shot fires, before the ball starts moving.")]
        [SerializeField] private CardSfxCue _shotSfx;
        [Tooltip("Played the instant the ball reaches the target.")]
        [SerializeField] private CardSfxCue _impactSfx;

        [Header("Screen Shake (on impact)")]
        [SerializeField] private float _shakeDuration = 0.2f;
        [SerializeField] private float _shakeAmplitude = 0.4f;
        [SerializeField] private float _shakeFrequency = 30f;

        public event Action Completed;

        private CardEffectSpawnContext _context;
        private Sequence _sequence;

        public void Initialize(CardEffectSpawnContext context)
        {
            _context = context;
            BuildAndPlaySequence();
        }

        private void BuildAndPlaySequence()
        {
            Vector3 start = transform.position;
            // Always lands at the configured impact anchor, regardless of which anchor this
            // entry spawned the ball at — e.g. left at the default ShipHit so impact lines up
            // with where the damage number/hit-flash from the generic combat feedback appears,
            // or pointed elsewhere per-prefab via the Impact Anchor Type dropdown.
            Transform endAnchor = _context.GetOpponentAnchor?.Invoke(_impactAnchorType) ?? _context.OpponentAnchor;
            Vector3 end = endAnchor.position;
            Vector3 midpoint = Vector3.Lerp(start, end, 0.5f) + Vector3.up * _arcHeight;

            // Fires immediately on spawn — no wind-up delay before the ball starts moving.
            OnShoot();

            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOPath(new[] { start, midpoint, end }, _travelDuration, PathType.CatmullRom)
                .SetEase(_travelEase));

            if (_spinDegrees != 0f)
            {
                _sequence.Join(transform.DORotate(new Vector3(0f, 0f, _spinDegrees), _travelDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear));
            }

            _sequence.AppendCallback(() => OnImpact(end));
            _sequence.OnComplete(() => Completed?.Invoke());
        }

        private void OnShoot()
        {
            CardSfxPlayer.Play(_shotSfx, transform.position);
        }

        private void OnImpact(Vector3 impactPosition)
        {
            gameObject.SetActive(false);

            ExplosionEffectPool.PlayAt(_explosionPrefab, impactPosition, _explosionLifetime);
            CardSfxPlayer.Play(_impactSfx, impactPosition);
            MMCameraShakeEvent.Trigger(_shakeDuration, _shakeAmplitude, _shakeFrequency, 0f, 0f, 0f);
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
