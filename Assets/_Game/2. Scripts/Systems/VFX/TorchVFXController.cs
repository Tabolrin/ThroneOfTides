using System;
using DG.Tweening;
using UnityEngine;
using ThroneOfTides.Core;
using ThroneOfTides.Data;

namespace ThroneOfTides.Systems.VFX
{
    /// <summary>
    /// Torch: a UI-canvas sprite thrown from the caster's ship to the target's, spinning (no
    /// trail). On impact, checks the target ship's Gunpowder world-space indicator
    /// (ShipStatusIndicator) — if active, plays the explosion animation/SFX and a screen shake;
    /// if not, the throw itself is the whole show. Damage math (base vs. combo-bonus) is already
    /// handled by CombatResolver/GameState — this only decides what to show.
    ///
    /// Uses ProjectileThrow for the shared spin-and-arc motion — also used by
    /// GunpowderBarrelVFXController, and intended for Cannonball/Pistol to adopt later.
    ///
    /// Scene/prefab setup requirements:
    ///   - Root prefab is a UI Image (RectTransform), same canvas-space convention as
    ///     Kraken/Siren/Lightning/Hail Storm — positioned via anchoredPosition, not world position.
    /// </summary>
    public class TorchVFXController : MonoBehaviour, ICardPlayEffect
    {
        [Header("Throw")]
        [SerializeField] private float _throwDuration = 0.45f;
        [Tooltip("Arc height in canvas units.")]
        [SerializeField] private float _arcHeight = 120f;
        [SerializeField] private float _spinDegrees = 900f;
        [SerializeField] private Ease  _moveEase = Ease.Linear;

        [Header("SFX")]
        [Tooltip("Played the moment the torch is thrown.")]
        [SerializeField] private CardSfxCue _throwSfx;
        [Tooltip("Played on impact only if the target ship had an active Gunpowder stack.")]
        [SerializeField] private CardSfxCue _explosionSfx;

        [Header("Explosion (only plays if Gunpowder was active on the hit ship)")]
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private float      _explosionLifetime = 1.5f;

        public event Action Completed;

        private RectTransform _rectTransform;
        private RectTransform _rootCanvasRect;
        private Camera        _gameCamera;
        private CardEffectSpawnContext _context;
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

            CardSfxPlayer.Play(_throwSfx, _context.CasterAnchor.position);

            _sequence = ProjectileThrow.Build(_rectTransform, from, to, _throwDuration, _arcHeight, _spinDegrees, _moveEase);
            _sequence.OnComplete(OnImpact);
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

        private void OnImpact()
        {
            bool gunpowderActive = _context.IsOpponentStatusVisible?.Invoke(ShipStatusType.Gunpowder) ?? false;

            if (gunpowderActive)
            {
                ExplosionEffectPool.PlayAt(_explosionPrefab, _context.OpponentAnchor.position, _explosionLifetime);

                CardSfxPlayer.Play(_explosionSfx, _context.OpponentAnchor.position);
                ScreenShake.Trigger(ScreenShakeLevel.Level4);
            }

            Completed?.Invoke();
        }

        private void OnDestroy() => _sequence?.Kill();
    }
}
