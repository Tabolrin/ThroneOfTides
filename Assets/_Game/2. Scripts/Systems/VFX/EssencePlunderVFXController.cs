// Assets/_Game/2. Scripts/Systems/VFX/EssencePlunderVFXController.cs
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems.VFX
{
    /// <summary>
    /// Essence Plunder: a lantern (UI-canvas sprite, same convention as Torch/Gunpowder Barrel)
    /// fades in on the activating player's ship. Once fully visible, a mana-flow particle system
    /// plays from the opponent's ship toward the caster's - always correct regardless of which
    /// side actually cast the card, since it's positioned/oriented from
    /// CardEffectSpawnContext.OpponentAnchor toward CasterAnchor (both already resolved relative
    /// to the caster) rather than any hardcoded "enemy"/"player" assumption. Only once the
    /// particle finishes does the "+N mana" popup appear (the actual mana transfer itself already
    /// happened synchronously when the card resolved - CombatResolver has no async capability -
    /// so this suppresses the generic instant popup and fires its own deferred one instead, timed
    /// to this animation). The lantern then fades out.
    ///
    /// The particle prefab ships empty on purpose - configure its Shape/Velocity/Color/etc.
    /// freely. This controller only positions and rotates the GameObject each play so its local
    /// +Y (up) points from the opponent's ship toward the caster's; aim your emission shape's
    /// "forward" along local +Y to match.
    /// </summary>
    public class EssencePlunderVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("References")]
        [SerializeField] private Image _lanternImage;
        [Tooltip("Ships empty - fully configure its Shape/Velocity/Color/etc. yourself. Rotated at runtime so its local +Y (up) points from the opponent's ship toward the caster's.")]
        [SerializeField] private ParticleSystem _manaFlowParticlesPrefab;

        [Header("Lantern Fade")]
        [SerializeField] private float _fadeInDuration = 0.2f;
        [SerializeField] private Ease  _fadeInEase = Ease.OutSine;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField] private Ease  _fadeOutEase = Ease.InSine;

        [Header("Mana Feedback")]
        [Tooltip("Shown as a floating \"+N\" once the particle finishes - keep in sync with the card's own configured steal amount (EssencePlunderEffectSO's _manaToSteal).")]
        [SerializeField] private int _manaAmountForDisplay = 2;

        public event Action Completed;

        private RectTransform _rectTransform;
        private RectTransform _rootCanvasRect;
        private Camera        _gameCamera;
        private CardEffectSpawnContext _context;
        private CanvasGroup   _canvasGroup;
        private ParticleSystem _particleInstance;
        private Sequence _sequence;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Initialize(CardEffectSpawnContext context)
        {
            _context        = context;
            _rootCanvasRect = context.GameCanvas;
            _gameCamera     = context.GameCamera;

            // Must happen now, before CombatResolver's synchronous StealEnemyMana call (which
            // fires moments after this method returns, as part of the same card resolution) -
            // by the time our own deferred popup would fire, it's already too late to still be
            // the "next" mana-gain event.
            _context.SuppressManaGainPopup?.Invoke();

            _rectTransform.anchoredPosition = WorldToCanvasLocalPoint(context.CasterAnchor.position);
            _canvasGroup.alpha = 0f;

            BuildFadeInSequence();
        }

        private void BuildFadeInSequence()
        {
            _sequence = DOTween.Sequence();
            _sequence.Append(_canvasGroup.DOFade(1f, _fadeInDuration).SetEase(_fadeInEase));
            _sequence.OnComplete(PlayManaFlowParticle);
        }

        private void PlayManaFlowParticle()
        {
            float waitDuration = 0f;

            if (_manaFlowParticlesPrefab != null)
            {
                Vector3 from = _context.OpponentAnchor.position;
                Vector3 to   = _context.CasterAnchor.position;
                Vector3 direction = (to - from).normalized;

                // Local +Y (up) points along the opponent->caster direction - see class remarks.
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

                _particleInstance = Instantiate(_manaFlowParticlesPrefab, from, Quaternion.Euler(0f, 0f, angle));
                _context.PlaySfx?.Invoke(from);
                _particleInstance.Play();

                waitDuration = _particleInstance.main.duration + _particleInstance.main.startLifetime.constantMax;
            }

            _sequence = DOTween.Sequence();
            _sequence.AppendInterval(waitDuration);
            _sequence.OnComplete(OnParticleComplete);
        }

        private void OnParticleComplete()
        {
            if (_particleInstance != null) Destroy(_particleInstance.gameObject, 2f);

            _context.SpawnManaGainedNumber?.Invoke(_manaAmountForDisplay, _context.CasterAnchor.position);

            _sequence = DOTween.Sequence();
            _sequence.Append(_canvasGroup.DOFade(0f, _fadeOutDuration).SetEase(_fadeOutEase));
            _sequence.OnComplete(() => Completed?.Invoke());
        }

        private Vector2 WorldToCanvasLocalPoint(Vector3 worldPosition)
        {
            Vector2 screenPoint = _gameCamera.WorldToScreenPoint(worldPosition);

            // null camera is correct for Screen Space Overlay canvases, matching the other
            // controllers' convention.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvasRect, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
