using System;
using DG.Tweening;
using UnityEngine;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems.VFX
{
    /// <summary>
    /// Gunpowder Barrel: a UI-canvas sprite thrown from the caster's ship to the target's,
    /// spinning, trailing a world-space dust particle system the whole flight. On impact,
    /// reveals the target ship's Gunpowder world-space indicator (ShipStatusIndicator) if it
    /// wasn't already visible - timed to this throw's landing rather than the instant GameState
    /// registers the stack.
    ///
    /// Uses ProjectileThrow for the shared spin-and-arc motion - also used by
    /// TorchVFXController, and intended for Cannonball/Pistol to adopt later.
    ///
    /// Scene/prefab setup requirements:
    ///   - Root prefab is a UI Image (RectTransform), same canvas-space convention as
    ///     Kraken/Siren/Lightning/Hail Storm - positioned via anchoredPosition, not world position.
    ///   - Dust trail is a persistent scene-level ParticleSystem (like Siren's music notes or
    ///     Lightning's strike burst) injected via CardEffectSpawnContext.GunpowderDustParticles -
    ///     reused and repositioned every frame rather than instantiated/destroyed per throw.
    /// </summary>
    public class GunpowderBarrelVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("References")]
        [Tooltip("Empty child RectTransform marking where the dust should emit from on the barrel. Converted to world space every frame, matching the Siren mouth-anchor / Lightning strike-anchor pattern.")]
        [SerializeField] private RectTransform _dustAnchor;

        [Header("Throw")]
        [SerializeField] private float _throwDuration = 0.5f;
        [Tooltip("Arc height in canvas units.")]
        [SerializeField] private float _arcHeight = 150f;
        [SerializeField] private float _spinDegrees = 720f;
        [SerializeField] private Ease  _moveEase = Ease.Linear;

        [Header("SFX")]
        [Tooltip("Played the moment the barrel is thrown.")]
        [SerializeField] private CardSfxCue _throwSfx;

        public event Action Completed;

        private RectTransform _rectTransform;
        private RectTransform _rootCanvasRect;
        private Camera        _gameCamera;
        private CardEffectSpawnContext _context;
        private ParticleSystem _dustParticles;
        private Sequence _sequence;

        private void Awake() => _rectTransform = GetComponent<RectTransform>();

        public void Initialize(CardEffectSpawnContext context)
        {
            _context        = context;
            _rootCanvasRect = context.GameCanvas;
            _gameCamera     = context.GameCamera;
            BuildAndPlaySequence();
        }

        private void BuildAndPlaySequence()
        {
            Vector2 from = WorldToCanvasLocalPoint(_context.CasterAnchor.position);
            Vector2 to   = WorldToCanvasLocalPoint(_context.OpponentAnchor.position);
            _rectTransform.anchoredPosition = from;

            _dustParticles = _context.GunpowderDustParticles;
            if (_dustParticles != null)
            {
                _dustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _dustParticles.transform.position = _context.CasterAnchor.position;

                // Player→Enemy throws face 0° (rightward); Enemy→Player throws face the
                // opposite direction, so the trail's shape/emission cone points the right way.
                float zRotation = _context.Caster == CardCasterFilter.Player ? 0f : 180f;
                _dustParticles.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);

                _dustParticles.Play();
            }

            CardSfxPlayer.Play(_throwSfx, _context.CasterAnchor.position);

            _sequence = ProjectileThrow.Build(_rectTransform, from, to, _throwDuration, _arcHeight, _spinDegrees, _moveEase);
            _sequence.OnUpdate(FollowDustToCurrentPosition);
            _sequence.OnComplete(OnImpact);
        }

        // ── Positioning ───────────────────────────────────────────────────────

        private Vector2 WorldToCanvasLocalPoint(Vector3 worldPosition)
        {
            Vector2 screenPoint = _gameCamera.WorldToScreenPoint(worldPosition);

            // null camera is correct for Screen Space Overlay canvases, matching the other
            // controllers' convention.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvasRect, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        private void FollowDustToCurrentPosition()
        {
            if (_dustParticles == null) return;

            // Convert the dust anchor's current on-screen world position back to a 3D world
            // point every frame, so the trail tracks the moving barrel - same conversion as
            // Lightning's _strikeAnchor / Siren's _mouthAnchor, just re-run continuously instead
            // of once, since this anchor is moving rather than static.
            Transform anchor = _dustAnchor != null ? _dustAnchor : _rectTransform;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, anchor.position);
            Vector3 worldPoint  = _gameCamera.ScreenToWorldPoint(
                new Vector3(screenPoint.x, screenPoint.y, _gameCamera.nearClipPlane + 1f));

            _dustParticles.transform.position = worldPoint;
        }

        private void OnImpact()
        {
            _dustParticles?.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // Always call this - RevealNow() is idempotent (the activation burst only ever
            // fires once per genuine 0->active transition, gated by its own internal
            // _burstPending flag; re-fading an already-visible icon is harmless). It must NOT be
            // gated here on IsOpponentStatusVisible: that reflects live game-state count, which
            // GameState.IncrementCombo already set the instant the card resolved - before this
            // throw animation even started - so it would always read "already visible" and skip
            // the reveal entirely, silently killing the activation burst.
            _context.RevealOpponentStatusIndicator?.Invoke(ShipStatusType.Gunpowder);

            Completed?.Invoke();
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
