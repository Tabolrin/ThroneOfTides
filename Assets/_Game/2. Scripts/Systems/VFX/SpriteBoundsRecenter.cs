// Assets/_Game/2. Scripts/Systems/VFX/SpriteBoundsRecenter.cs
using UnityEngine;

namespace ThroneOfTides.Systems.VFX
{
    // Keeps this SpriteRenderer visually centered on its parent and capped to a consistent max
    // on-screen size, frame by frame — for animated spritesheets (e.g. Explosion) whose frames
    // were trimmed to wildly different pixel dimensions with inconsistent pivots, so the effect
    // doesn't balloon or drift off-center on whichever frame the animator happens to land on.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteBoundsRecenter : MonoBehaviour
    {
        [Tooltip("Largest dimension (width or height), in world units, any single frame is allowed to render at.")]
        [SerializeField] private float _maxSize = 1f;

        private SpriteRenderer _renderer;

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        private void LateUpdate()
        {
            if (_renderer.sprite == null) return;

            Vector3 rawSize = _renderer.sprite.bounds.size;
            float largestDimension = Mathf.Max(rawSize.x, rawSize.y);
            float scale = largestDimension > 0f ? _maxSize / largestDimension : 1f;

            transform.localScale = Vector3.one * scale;
            transform.localPosition = -_renderer.sprite.bounds.center * scale;
        }
    }
}
