using UnityEngine;

namespace ThroneOfTides.Systems
{
    // Marks a world-space position for VFX spawning
    // Place as child GameObjects on each ship
    public class VFXSpawnPosition : MonoBehaviour
    {
        public enum SpawnPositionType
        {
            ShipHit,
            ShipDeck,
            ShipFront,
            SeaSurface
        }

        [SerializeField] private SpawnPositionType _type;
        public SpawnPositionType Type => _type;
    }
}