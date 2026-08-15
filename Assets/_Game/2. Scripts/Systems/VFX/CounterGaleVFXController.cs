// Assets/_Game/2. Scripts/Systems/VFX/CounterGaleVFXController.cs
using DG.Tweening;
using UnityEngine;

namespace ThroneOfTides.Systems.VFX
{
    // Counter Gale reaction VFX - spawned directly by CardVFXHandler.OnReactionFired, always at
    // the player's sky anchor regardless of which side actually fired the reaction (see
    // CardVFXHandler for that side-independent spawn call). Reactions bypass
    // CardPresentationPlayer/ICardPlayEffect entirely, so this manages its own full lifecycle.
    // Jitters randomly within a small radius of its spawn point for _wanderDuration, then fades
    // out and self-destroys.
    public class CounterGaleVFXController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sprite;

        [Header("Wander")]
        [SerializeField] private float _wanderDuration   = 0.65f;
        [SerializeField] private float _stepDuration     = 0.08f;
        [Tooltip("Max distance from the spawn point any single random step can land - keeps the wander confined to a tight area instead of drifting away.")]
        [SerializeField] private float _maxMoveDistance  = 0.5f;
        [SerializeField] private Ease  _stepEase         = Ease.InOutSine;

        [Header("Fade Out")]
        [SerializeField] private float _fadeOutDuration = 0.25f;

        private Sequence _sequence;

        private void Awake()
        {
            if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start() => BuildAndPlaySequence();

        private void BuildAndPlaySequence()
        {
            Vector3 origin = transform.position;
            _sequence = DOTween.Sequence();

            // Each step picks a fresh random point within _maxMoveDistance of the ORIGINAL spawn
            // point (not the previous step) so the wander stays bounded rather than drifting.
            int stepCount = Mathf.Max(1, Mathf.RoundToInt(_wanderDuration / _stepDuration));
            for (int i = 0; i < stepCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * _maxMoveDistance;
                Vector3 nextPoint = origin + (Vector3)offset;
                _sequence.Append(transform.DOMove(nextPoint, _stepDuration).SetEase(_stepEase));
            }

            _sequence.Append(_sprite.DOFade(0f, _fadeOutDuration));
            _sequence.OnComplete(() => Destroy(gameObject));
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
