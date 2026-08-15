// Assets/_Game/2. Scripts/Systems/VFX/HighSpiritsVFXController.cs
using DG.Tweening;
using ThroneOfTides.Data;
using UnityEngine;

namespace ThroneOfTides.Systems.VFX
{
    // High Spirits VFX - spawned by CardVFXHandler's legacy PlayCardVFX routing (no
    // CardPresentationEntry for this card; see its _highSpiritsPrefab field). Two mug sprites
    // start apart and move toward each other for a "cheers" - on collision, plays a clink SFX
    // and fires a droplet burst (same instantiate/stop/play-on-payoff pattern as Treasure
    // Chest's coin burst), then both mugs fade out and the whole effect self-destroys.
    public class HighSpiritsVFXController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _leftMug;
        [SerializeField] private SpriteRenderer _rightMug;
        [SerializeField] private ParticleSystem _dropletParticlesPrefab;
        [SerializeField] private CardSfxCue     _clinkSfx = new CardSfxCue();

        [Header("Setup")]
        [Tooltip("How far apart the two mugs start, split evenly to either side of this GameObject's position.")]
        [SerializeField] private float _startSeparation = 2f;
        [Tooltip("Empty child transform marking where the droplet particle burst plays - falls back to this GameObject's own position if left unassigned.")]
        [SerializeField] private Transform _particleAnchor;

        [Header("Approach")]
        [SerializeField] private float _approachDuration = 0.35f;
        [SerializeField] private Ease  _approachEase     = Ease.InQuad;
        [Tooltip("Gap left between the two mugs when they stop - 0 means they meet exactly in the middle.")]
        [SerializeField] private float _stopDistance = 0f;

        [Header("Hold & Fade Out")]
        [SerializeField] private float _holdDuration    = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.3f;

        private ParticleSystem _dropletInstance;
        private Sequence       _sequence;

        private void Awake()
        {
            if (_leftMug == null || _rightMug == null) return;

            Vector3 center = transform.position;
            _leftMug.transform.position  = center + Vector3.left  * (_startSeparation * 0.5f);
            _rightMug.transform.position = center + Vector3.right * (_startSeparation * 0.5f);

            if (_dropletParticlesPrefab != null)
            {
                Vector3 particlePosition = _particleAnchor != null ? _particleAnchor.position : center;

                // Same pattern as TreasureChestVFXController's coin burst: spawn stopped+cleared
                // now, play it later at the actual payoff beat instead of on instantiation.
                _dropletInstance = Instantiate(_dropletParticlesPrefab, particlePosition, Quaternion.identity);
                _dropletInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Start() => BuildAndPlaySequence();

        private void BuildAndPlaySequence()
        {
            Vector3 center = transform.position;
            Vector3 leftStop  = center + Vector3.left  * (_stopDistance * 0.5f);
            Vector3 rightStop = center + Vector3.right * (_stopDistance * 0.5f);

            _sequence = DOTween.Sequence();
            _sequence.Append(_leftMug.transform.DOMove(leftStop, _approachDuration).SetEase(_approachEase));
            _sequence.Join(_rightMug.transform.DOMove(rightStop, _approachDuration).SetEase(_approachEase));
            _sequence.AppendCallback(OnCollide);
            _sequence.AppendInterval(_holdDuration);
            _sequence.Append(_leftMug.DOFade(0f, _fadeOutDuration));
            _sequence.Join(_rightMug.DOFade(0f, _fadeOutDuration));
            _sequence.OnComplete(OnSequenceComplete);
        }

        private void OnCollide()
        {
            CardSfxPlayer.Play(_clinkSfx, transform.position);
            _dropletInstance?.Play();
        }

        private void OnSequenceComplete()
        {
            if (_dropletInstance != null) Destroy(_dropletInstance.gameObject, 3f);
            Destroy(gameObject);
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
