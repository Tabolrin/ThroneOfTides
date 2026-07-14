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

        public ShipStatusType StatusType => _statusType;
        public DamageTarget   Ship       => _ship;

        /// <summary>True while the icon is at (or fading toward) full visibility.</summary>
        public bool IsVisible => _icon != null && _icon.color.a > 0.01f;

        private void OnEnable()  => GameEventBus.OnShipStatusCountChanged += HandleStatusChanged;
        private void OnDisable() => GameEventBus.OnShipStatusCountChanged -= HandleStatusChanged;

        private void HandleStatusChanged(ShipStatusType type, DamageTarget ship, int count)
        {
            if (type != _statusType || ship != _ship || _icon == null) return;

            if (count > 0)
            {
                if (_autoRevealOnActivate) RevealNow();
                return;
            }

            _icon.DOKill();
            _icon.DOFade(0f, _fadeDuration);
        }

        /// <summary>
        /// Fades the icon to fully visible right now, regardless of _autoRevealOnActivate —
        /// called by a thrown-projectile VFX controller at its impact moment.
        /// </summary>
        public void RevealNow()
        {
            if (_icon == null) return;
            _icon.DOKill();
            _icon.DOFade(1f, _fadeDuration);
        }
    }
}
