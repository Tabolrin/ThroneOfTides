// Assets/_Game/2. Scripts/Systems/VFX/ShipGunpowderVisual.cs
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

        private void OnEnable()  => GameEventBus.OnShipStatusCountChanged += HandleStatusChanged;
        private void OnDisable() => GameEventBus.OnShipStatusCountChanged -= HandleStatusChanged;

        private void HandleStatusChanged(ShipStatusType type, DamageTarget ship, int count)
        {
            if (type != ShipStatusType.Gunpowder || ship != _ship || _shipRenderer == null) return;
            _shipRenderer.sprite = count > 0 ? _poweredSprite : _normalSprite;
        }
    }
}
