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
    /// plays between the two ships - which ship it starts on is configurable via Mana Flow
    /// Origin, but it always travels toward the OTHER ship automatically. Both ends are resolved
    /// relative to whoever cast the card (CardEffectSpawnContext.CasterAnchor/OpponentAnchor),
    /// so this is correct regardless of which side actually cast it - never a hardcoded
    /// "enemy"/"player" assumption. Only once the particle finishes does the "+N mana" popup
    /// appear (the actual mana transfer itself already happened synchronously when the card
    /// resolved - CombatResolver has no async capability - so this suppresses the generic
    /// instant popup and fires its own deferred one instead, timed to this animation). The
    /// lantern then fades out.
    ///
    /// The mana-flow prefab's root is an empty rig (no ParticleSystem of its own) holding one or
    /// more child ParticleSystems - configure each child's Shape/Velocity/Color/etc. and local
    /// tilt freely (e.g. several streams at slightly different angles to sell the suction
    /// illusion). This controller only positions and rotates the root each play so its local +Y
    /// (up) points from the start ship toward the end ship, then finds and plays every child
    /// ParticleSystem underneath it - aim each child's emission shape "forward" along local +Y
    /// to match, however many children there are.
    /// </summary>
    public class EssencePlunderVFXController : MonoBehaviour, ICardPlayEffect
    {
        /// <summary>Which ship the mana-flow particle spawns on - see ManaFlowOrigin field below.</summary>
        public enum ManaFlowOrigin { Opponent, Caster }

        [Header("References")]
        [SerializeField] private Image _lanternImage;
        [Tooltip("An empty root GameObject holding one or more child ParticleSystems (e.g. several tilted streams to sell the suction illusion) - fully configure them yourself. The whole prefab is rotated at runtime so its local +Y (up) points from the Mana Flow Origin ship toward the other ship; all child particle systems are found and played together automatically, however many there are.")]
        [SerializeField] private GameObject _manaFlowParticlesPrefab;

        [Tooltip("Which ship the mana-flow particle spawns on - it always travels toward the OTHER ship automatically, regardless of this choice. Both are resolved relative to whoever cast the card, so 'Opponent'/'Caster' stay correct no matter which side plays it.")]
        [SerializeField] private ManaFlowOrigin _manaFlowOrigin = ManaFlowOrigin.Opponent;

        [Header("Lantern Fade")]
        [SerializeField] private float _fadeInDuration = 0.2f;
        [SerializeField] private Ease  _fadeInEase = Ease.OutSine;
        [Tooltip("Beat after the mana-flow particle finishes, with the lantern still fully visible, before the fade-out itself starts - long enough to read as 'done', short enough not to feel like a stall.")]
        [SerializeField] private float _postParticleHoldDuration = 0.25f;
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
        private GameObject    _particleInstance;
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
                bool startsOnCaster = _manaFlowOrigin == ManaFlowOrigin.Caster;
                Vector3 from = startsOnCaster ? _context.CasterAnchor.position   : _context.OpponentAnchor.position;
                Vector3 to   = startsOnCaster ? _context.OpponentAnchor.position : _context.CasterAnchor.position;
                Vector3 direction = (to - from).normalized;

                // Local +Y (up) points along the configured start->end direction - see class remarks.
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

                _particleInstance = Instantiate(_manaFlowParticlesPrefab, from, Quaternion.Euler(0f, 0f, angle));
                _context.PlaySfx?.Invoke(from);

                // The root itself has no ParticleSystem - it's just a rig holding one or more
                // child streams (e.g. several tilted copies to sell the suction illusion) - so
                // each child is found and started individually rather than relying on
                // ParticleSystem.Play's own withChildren cascade, which needs a root system to
                // cascade FROM. Wait for whichever one actually finishes last, not just the
                // first, or the mana popup/fade-out could fire while another stream is still
                // visibly playing.
                foreach (var system in _particleInstance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    system.Play();
                    float systemDuration = system.main.duration + system.main.startLifetime.constantMax;
                    waitDuration = Mathf.Max(waitDuration, systemDuration);
                }
            }

            _sequence = DOTween.Sequence();
            _sequence.AppendInterval(waitDuration);
            _sequence.OnComplete(OnParticleComplete);
        }

        private void OnParticleComplete()
        {
            if (_particleInstance != null) Destroy(_particleInstance, 2f);

            _context.SpawnManaGainedNumber?.Invoke(_manaAmountForDisplay, _context.CasterAnchor.position);

            _sequence = DOTween.Sequence();
            // Lantern stays fully visible (not fading) for this beat - the particle is already
            // done, but starting the fade immediately read as too abrupt.
            _sequence.AppendInterval(_postParticleHoldDuration);
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
