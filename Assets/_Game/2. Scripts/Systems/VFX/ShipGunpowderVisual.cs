// Assets/_Game/2. Scripts/Systems/VFX/ShipGunpowderVisual.cs
using System.Collections;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems
{
    // Swaps a ship's main sprite between its normal and "powdered" (Gunpowder-primed) look,
    // driven by the same GameEventBus.OnShipStatusCountChanged event that feeds the Gunpowder
    // HUD badge and world-space indicator. Place one instance per ship.
    public class ShipGunpowderVisual : MonoBehaviour
    {
        [Tooltip("Which ship this instance represents.")]
        [SerializeField] private DamageTarget _ship;

        [SerializeField] private SpriteRenderer _shipRenderer;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _poweredSprite;

        [Tooltip("Seconds to wait before actually swapping the sprite, so the visual change doesn't snap the instant the card resolves — lines it up better with a thrown-projectile VFX's travel time.")]
        [SerializeField] private float _swapDelay = 0.5f;

        // While held, incoming status changes are recorded but not applied — lets a VFX
        // sequence (e.g. Tidal Wave) keep the powdered look on screen past the instant the
        // game state actually clears it, then reveal the change at its own chosen beat.
        private bool _overriding;
        private int  _latestCount;
        private Coroutine _pendingSwap;

        private void OnEnable()  => GameEventBus.OnShipStatusCountChanged += HandleStatusChanged;

        private void OnDisable()
        {
            GameEventBus.OnShipStatusCountChanged -= HandleStatusChanged;
            if (_pendingSwap != null) StopCoroutine(_pendingSwap);
            _pendingSwap = null;
        }

        private void HandleStatusChanged(ShipStatusType type, DamageTarget ship, int count)
        {
            if (type != ShipStatusType.Gunpowder || ship != _ship || _shipRenderer == null) return;
            _latestCount = count;
            if (_overriding) return;

            if (_pendingSwap != null) StopCoroutine(_pendingSwap);
            _pendingSwap = StartCoroutine(ApplyAfterDelay(count));
        }

        private IEnumerator ApplyAfterDelay(int count)
        {
            yield return new WaitForSeconds(_swapDelay);
            _pendingSwap = null;
            Apply(count);
        }

        private void Apply(int count) => _shipRenderer.sprite = count > 0 ? _poweredSprite : _normalSprite;

        /// <summary>Starts ignoring live status updates — the sprite stays exactly as it is now.</summary>
        public void BeginOverride() => _overriding = true;

        /// <summary>
        /// Stops ignoring updates and immediately (no delay — the caller already timed this to
        /// its own sequence) applies whatever the real state became meanwhile.
        /// </summary>
        public void EndOverride()
        {
            _overriding = false;
            if (_pendingSwap != null) { StopCoroutine(_pendingSwap); _pendingSwap = null; }
            Apply(_latestCount);
        }
    }
}
