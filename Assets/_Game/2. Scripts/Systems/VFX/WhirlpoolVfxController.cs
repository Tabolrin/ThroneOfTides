using System.Collections.Generic;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems.VFX
{
    /// <summary>
    /// Spawns/despawns the Whirlpool world-space VFX at a ship's sea-surface anchor in response
    /// to GameEventBus.OnShipStatusCountChanged. At most one instance per ship — re-applying
    /// Whirlpool to a ship that already has one just extends the status's turn count (handled
    /// entirely by the game's own status tracking), so this controller never re-triggers the VFX
    /// while one is already playing on that ship. Both ships can have their own instance active
    /// at the same time.
    /// </summary>
    public class WhirlpoolVfxController : MonoBehaviour
    {
        [SerializeField] private ShipVfxAnchors _playerAnchors;
        [SerializeField] private ShipVfxAnchors _enemyAnchors;
        [SerializeField] private GameObject     _whirlpoolPrefab;

        private readonly Dictionary<DamageTarget, GameObject> _activeVfx = new Dictionary<DamageTarget, GameObject>();

        private void OnEnable()  => GameEventBus.OnShipStatusCountChanged += OnStatusCountChanged;
        private void OnDisable() => GameEventBus.OnShipStatusCountChanged -= OnStatusCountChanged;

        private void OnStatusCountChanged(ShipStatusType type, DamageTarget ship, int count)
        {
            if (type != ShipStatusType.Whirlpool) return;

            bool hasActive = _activeVfx.TryGetValue(ship, out GameObject instance);

            if (count > 0)
            {
                if (hasActive) return; // already playing — extra turns are tracked by GameState, not by us.

                Transform anchor = GetAnchors(ship)?.Get(VfxAnchorType.SeaSurface);
                if (anchor == null || _whirlpoolPrefab == null) return;

                _activeVfx[ship] = Instantiate(_whirlpoolPrefab, anchor.position, anchor.rotation, anchor);
                return;
            }

            if (!hasActive) return;
            if (instance != null) Destroy(instance);
            _activeVfx.Remove(ship);
        }

        private ShipVfxAnchors GetAnchors(DamageTarget ship) => ship == DamageTarget.Player ? _playerAnchors : _enemyAnchors;

        private void OnDestroy()
        {
            foreach (var kvp in _activeVfx)
                if (kvp.Value != null) Destroy(kvp.Value);
            _activeVfx.Clear();
        }
    }
}
