// Assets/_Game/2. Scripts/Systems/VFX/DeadMansTurnVFXController.cs
using DG.Tweening;
using UnityEngine;

namespace ThroneOfTides.Systems.VFX
{
    // Dead Man's Turn reaction VFX. Reactions are charged on draw and consumed later inside
    // TurnCoordinator - they never flow through CardPresentationPlayer/ICardPlayEffect, so this
    // is spawned directly by CardVFXHandler.OnReactionFired, parented under the evading ship
    // itself (at the ship's hit point) so the anchor sprite genuinely rides along as part of the
    // ship rather than floating independently in world space. It manages its own full lifecycle
    // (no external Destroy/lifetime timer).
    // A fast fade-in on the anchor, then the WHOLE SHIP recoils away and tweens back to its
    // resting position before the anchor fades out. The ship's idle Oscillator bob is paused for
    // the duration so the two don't fight over the same transform, then resumed unchanged.
    public class DeadMansTurnVFXController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sprite;

        [Header("Fade In")]
        [SerializeField] private float _fadeInDuration = 0.08f;

        [Header("Recoil")]
        [Tooltip("World-space direction the ship recoils toward before tweening back.")]
        [SerializeField] private Vector2 _moveDirection = Vector2.right;
        [Tooltip("How far the recoil travels before tweening back - the 'medium distance' called out in design.")]
        [SerializeField] private float _moveBackDistance = 1.5f;
        [SerializeField] private float _moveBackDuration = 0.2f;
        [SerializeField] private Ease  _moveBackEase     = Ease.OutQuad;

        [Header("Return")]
        [SerializeField] private float _returnDuration = 0.25f;
        [SerializeField] private Ease  _returnEase     = Ease.InOutSine;

        [Header("Hold & Fade Out")]
        [SerializeField] private float _holdDuration    = 0.2f;
        [SerializeField] private float _fadeOutDuration = 0.2f;

        private Transform  _shipTransform;
        private Oscillator _shipOscillator;
        private Sequence   _sequence;

        private void Awake()
        {
            if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();

            var c = _sprite.color;
            _sprite.color = new Color(c.r, c.g, c.b, 0f);
        }

        // The evading ship whose whole hull should recoil - this object is already parented
        // under it by the spawner, but the ship's own Transform (and its Oscillator, to pause
        // the idle bob for the duration) still need to be resolved explicitly.
        public void Setup(Transform shipTransform)
        {
            _shipTransform  = shipTransform;
            _shipOscillator = shipTransform != null ? shipTransform.GetComponent<Oscillator>() : null;
        }

        private void Start() => BuildAndPlaySequence();

        private void BuildAndPlaySequence()
        {
            // Falls back to recoiling this anchor alone if no ship was supplied - keeps the
            // effect functional even if Setup() is ever skipped.
            Transform mover = _shipTransform != null ? _shipTransform : transform;

            Vector3 basePosition = mover.position;
            Vector3 backPosition = basePosition + (Vector3)(_moveDirection.normalized * _moveBackDistance);

            // Stop the ship's own idle drift for the duration - otherwise it and this recoil
            // fight over the same transform every frame.
            mover.DOKill();

            _sequence = DOTween.Sequence();
            _sequence.Append(_sprite.DOFade(1f, _fadeInDuration));
            _sequence.Append(mover.DOMove(backPosition, _moveBackDuration).SetEase(_moveBackEase));
            _sequence.Append(mover.DOMove(basePosition, _returnDuration).SetEase(_returnEase));
            _sequence.AppendInterval(_holdDuration);
            _sequence.Append(_sprite.DOFade(0f, _fadeOutDuration));
            _sequence.OnComplete(() =>
            {
                _shipOscillator?.RestartOscillation();
                Destroy(gameObject);
            });
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
