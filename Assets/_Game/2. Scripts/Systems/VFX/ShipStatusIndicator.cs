// Assets/_Game/2. Scripts/Systems/VFX/ShipStatusIndicator.cs
using DG.Tweening;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Fades a persistent world-space status sprite in/out in response to
    /// GameEventBus.OnShipStatusCountChanged. Place one instance per status-sprite-per-ship
    /// (e.g. a gunpowder-barrel decoration already sitting on each ship), configure which
    /// status type and which ship it represents, and it handles the rest. Reusable for any
    /// future ShipStatusType without writing a new script — see also ActiveEffectsBar, which
    /// listens to the same event for the HUD badge equivalent.
    /// </summary>
    public class ShipStatusIndicator : MonoBehaviour
    {
        [Tooltip("Which status this indicator represents.")]
        [SerializeField] private ShipStatusType _statusType;

        [Tooltip("Which ship this indicator is attached to.")]
        [SerializeField] private DamageTarget _ship;

        [Tooltip("The sprite that fades in/out. Its alpha should start at 0 if the status begins inactive.")]
        [SerializeField] private SpriteRenderer _icon;

        [Tooltip("Fade tween duration in seconds.")]
        [SerializeField] private float _fadeDuration = 0.3f;

        [Tooltip("If false, this indicator ignores the 0→active transition of OnShipStatusCountChanged and waits for an external RevealNow() call instead — for statuses whose activation is timed to a thrown-projectile VFX's impact (e.g. Gunpowder Barrel) rather than the instant the card is played. Fading back out on expiry still works normally either way.")]
        [SerializeField] private bool _autoRevealOnActivate = true;

        [Header("Activation Burst")]
        [Tooltip("Optional one-shot particle burst played only on the 0→active transition (not on every subsequent stack increase, and not on a re-application while already active). Stays dormant otherwise. Fires at the same moment the icon reveals — immediately if AutoRevealOnActivate, otherwise deferred to the external RevealNow() call (e.g. a thrown-projectile's impact).")]
        [SerializeField] private ParticleSystem _activationBurst;

        public ShipStatusType StatusType => _statusType;
        public DamageTarget   Ship       => _ship;

        /// <summary>
        /// True while this status is actually active on the ship (last known count > 0) —
        /// tracks game state directly rather than the optional decorative icon's alpha, so this
        /// stays correct even on indicators that don't have an icon wired at all.
        /// </summary>
        public bool IsVisible => _previousCount > 0;

        private int  _previousCount;
        private bool _burstPending;

        private void OnEnable()  => GameEventBus.OnShipStatusCountChanged += HandleStatusChanged;
        private void OnDisable() => GameEventBus.OnShipStatusCountChanged -= HandleStatusChanged;

        private void HandleStatusChanged(ShipStatusType type, DamageTarget ship, int count)
        {
            if (type != _statusType || ship != _ship) return;

            bool activating = count > 0 && _previousCount <= 0;
            _previousCount = count;

            if (count > 0)
            {
                if (activating) _burstPending = true;
                if (_autoRevealOnActivate) RevealNow();
                return;
            }

            if (_icon == null) return;
            _icon.DOKill();
            _icon.DOFade(0f, _fadeDuration);
        }

        /// <summary>
        /// Fades the icon to fully visible right now, regardless of _autoRevealOnActivate —
        /// called by a thrown-projectile VFX controller at its impact moment. Also fires the
        /// activation burst if a genuine 0→active transition is still waiting on this reveal.
        /// </summary>
        public void RevealNow()
        {
            if (_icon != null)
            {
                _icon.DOKill();
                _icon.DOFade(1f, _fadeDuration);
            }

            if (_burstPending)
            {
                _burstPending = false;
                _activationBurst?.Play();
            }
        }
    }
}
