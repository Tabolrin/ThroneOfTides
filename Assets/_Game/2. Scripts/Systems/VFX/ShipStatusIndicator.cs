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

        private void OnEnable()  => GameEventBus.OnShipStatusCountChanged += HandleStatusChanged;
        private void OnDisable() => GameEventBus.OnShipStatusCountChanged -= HandleStatusChanged;

        private void HandleStatusChanged(ShipStatusType type, DamageTarget ship, int count)
        {
            if (type != _statusType || ship != _ship || _icon == null) return;

            float targetAlpha = count > 0 ? 1f : 0f;
            _icon.DOKill();
            _icon.DOFade(targetAlpha, _fadeDuration);
        }
    }
}
