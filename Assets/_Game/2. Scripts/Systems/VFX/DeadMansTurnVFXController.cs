// Assets/_Game/2. Scripts/Systems/VFX/DeadMansTurnVFXController.cs
using DG.Tweening;
using UnityEngine;

namespace ThroneOfTides.Systems.VFX
{
    // Dead Man's Turn reaction VFX. Reactions are charged on draw and consumed later inside
    // TurnCoordinator — they never flow through CardPresentationPlayer/ICardPlayEffect, so this
    // is spawned directly by CardVFXHandler.OnReactionFired at the activating ship's hit point
    // and manages its own full lifecycle (no external Destroy/lifetime timer).
    // A fast fade-in on top of the ship, a recoil away from the opponent, then a tween back to
    // its spawn position before fading out.
    public class DeadMansTurnVFXController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sprite;

        [Header("Fade In")]
        [SerializeField] private float _fadeInDuration = 0.08f;

        [Header("Recoil")]
        [Tooltip("World-space direction the anchor recoils toward — up by default since the caller only supplies a single anchor point, not a facing direction.")]
        [SerializeField] private Vector2 _moveDirection = Vector2.up;
        [Tooltip("How far the recoil travels before tweening back — the 'medium distance' called out in design.")]
        [SerializeField] private float _moveBackDistance = 1.5f;
        [SerializeField] private float _moveBackDuration = 0.2f;
        [SerializeField] private Ease  _moveBackEase     = Ease.OutQuad;

        [Header("Return")]
        [SerializeField] private float _returnDuration = 0.25f;
        [SerializeField] private Ease  _returnEase     = Ease.InOutSine;

        [Header("Hold & Fade Out")]
        [SerializeField] private float _holdDuration    = 0.2f;
        [SerializeField] private float _fadeOutDuration = 0.2f;

        private Sequence _sequence;

        private void Awake()
        {
            if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();

            var c = _sprite.color;
            _sprite.color = new Color(c.r, c.g, c.b, 0f);
        }

        private void Start() => BuildAndPlaySequence();

        private void BuildAndPlaySequence()
        {
            Vector3 basePosition = transform.position;
            Vector3 backPosition = basePosition + (Vector3)(_moveDirection.normalized * _moveBackDistance);

            _sequence = DOTween.Sequence();
            _sequence.Append(_sprite.DOFade(1f, _fadeInDuration));
            _sequence.Append(transform.DOMove(backPosition, _moveBackDuration).SetEase(_moveBackEase));
            _sequence.Append(transform.DOMove(basePosition, _returnDuration).SetEase(_returnEase));
            _sequence.AppendInterval(_holdDuration);
            _sequence.Append(_sprite.DOFade(0f, _fadeOutDuration));
            _sequence.OnComplete(() => Destroy(gameObject));
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
