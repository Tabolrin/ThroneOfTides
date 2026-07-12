// Assets/_Game/2. Scripts/Systems/VFX/CannonballVFXController.cs
using System;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Cannonball travel-and-impact effect shared by Pistol, Cannon Ball, and Chain Shot.
    /// Spawns already positioned at the caster's ship (CardPresentationPlayer places it there),
    /// waits GameConfigSO's pre-shot delay, fires the shot feedback + shake, arcs across to the
    /// opponent's ship, then hides itself and plays the explosion prefab + impact feedback/shake
    /// + SFX on arrival. Implements ICardPlayEffect so CardPresentationPlayer can host it without
    /// knowing any of this internal sequencing.
    /// </summary>
    public class CannonballVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Config")]
        [Tooltip("Supplies the pre-shot delay (GameConfigSO.CannonballPreShotDelay).")]
        [SerializeField] private GameConfigSO _config;

        [Header("Travel")]
        [Tooltip("Seconds the cannonball takes to arc from the caster's ship to the target.")]
        [SerializeField] private float _travelDuration = 0.5f;
        [Tooltip("Height added at the midpoint of the travel arc.")]
        [SerializeField] private float _arcHeight = 1.5f;
        [SerializeField] private Ease _travelEase = Ease.Linear;

        [Header("Explosion")]
        [Tooltip("Explosion animation prefab, played once on top of the target ship on impact.")]
        [SerializeField] private GameObject _explosionPrefab;
        [Tooltip("Seconds before the spawned explosion instance is destroyed.")]
        [SerializeField] private float _explosionLifetime = 1.5f;

        [Header("Feedbacks")]
        [Tooltip("Played the moment the cannon fires (muzzle flash, etc).")]
        [SerializeField] private MMF_Player _feedbackOnShoot;
        [Tooltip("Played the moment the cannonball impacts the target.")]
        [SerializeField] private MMF_Player _feedbackOnImpact;
        [Tooltip("Screen shake played at the moment of firing.")]
        [SerializeField] private MMF_Player _shakeOnShoot;
        [Tooltip("Screen shake played at the moment of impact.")]
        [SerializeField] private MMF_Player _shakeOnImpact;

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
            Vector3 end = _context.OpponentAnchor.position;
            Vector3 midpoint = Vector3.Lerp(start, end, 0.5f) + Vector3.up * _arcHeight;
            float preShotDelay = _config != null ? _config.CannonballPreShotDelay : 0f;

            _sequence = DOTween.Sequence();
            _sequence.AppendInterval(preShotDelay);
            _sequence.AppendCallback(OnShoot);
            _sequence.Append(transform.DOPath(new[] { start, midpoint, end }, _travelDuration, PathType.CatmullRom)
                .SetEase(_travelEase));
            _sequence.AppendCallback(OnImpact);
            _sequence.OnComplete(() => Completed?.Invoke());
        }

        private void OnShoot()
        {
            _feedbackOnShoot?.PlayFeedbacks();
            _shakeOnShoot?.PlayFeedbacks();
        }

        private void OnImpact()
        {
            gameObject.SetActive(false);

            if (_explosionPrefab != null)
            {
                var explosion = Instantiate(_explosionPrefab, _context.OpponentAnchor.position, Quaternion.identity);
                Destroy(explosion, _explosionLifetime);
            }

            _feedbackOnImpact?.PlayFeedbacks();
            _shakeOnImpact?.PlayFeedbacks();
            _context.PlaySfx?.Invoke(_context.OpponentAnchor.position);
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
