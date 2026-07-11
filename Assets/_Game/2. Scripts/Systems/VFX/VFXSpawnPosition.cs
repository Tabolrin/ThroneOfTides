using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems
{
    // Marks a world-space position for VFX spawning
    // Place as child GameObjects on each ship
    public class VFXSpawnPosition : MonoBehaviour
    {
        [SerializeField] private VfxAnchorType _type;
        public VfxAnchorType Type => _type;
    }
}