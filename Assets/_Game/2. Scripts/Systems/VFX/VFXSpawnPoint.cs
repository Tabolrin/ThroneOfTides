using UnityEngine;

namespace ThroneOfTides.Systems
{
    // Marks a world-space position for VFX spawning
    // Place as child GameObjects on each ship
    public class VFXSpawnPoint : MonoBehaviour
    {
        public enum SpawnPointType
        {
            ShipHit,
            ShipDeck,
            ShipFront,
            SeaSurface
        }

        [SerializeField] private SpawnPointType _type;
        public SpawnPointType Type => _type;
    }
}
