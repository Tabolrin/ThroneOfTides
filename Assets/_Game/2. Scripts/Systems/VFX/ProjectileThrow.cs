using DG.Tweening;
using UnityEngine;

namespace ThroneOfTides.Systems.VFX
{
    /// <summary>
    /// Shared UI-canvas "thrown object" motion: arcs a RectTransform from one anchored position
    /// to another while spinning, exactly like a lobbed barrel or torch. Used by
    /// GunpowderBarrelVFXController and TorchVFXController, and intended for Cannonball/Pistol
    /// to adopt the same movement later instead of each hand-rolling its own arc lerp.
    /// </summary>
    public static class ProjectileThrow
    {
        /// <summary>
        /// Builds (but does not play) a sequence that moves <paramref name="projectile"/>'s
        /// anchoredPosition from <paramref name="from"/> to <paramref name="to"/> along a
        /// quadratic-bezier arc, while spinning it, over <paramref name="duration"/> seconds.
        /// Caller decides when to play it and what to hook onto OnUpdate/OnComplete.
        /// </summary>
        public static Sequence Build(
            RectTransform projectile,
            Vector2 from,
            Vector2 to,
            float duration,
            float arcHeight,
            float spinDegrees,
            Ease moveEase = Ease.Linear)
        {
            Vector2 midpoint = Vector2.Lerp(from, to, 0.5f) + Vector2.up * arcHeight;

            float t = 0f;
            Sequence seq = DOTween.Sequence();
            seq.Join(DOTween.To(() => t, x => t = x, 1f, duration)
                .SetEase(moveEase)
                .OnUpdate(() => projectile.anchoredPosition = QuadraticBezier(from, midpoint, to, t)));

            seq.Join(projectile
                .DORotate(new Vector3(0f, 0f, spinDegrees), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear));

            return seq;
        }

        private static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float u = 1f - t;
            return (u * u * a) + (2f * u * t * b) + (t * t * c);
        }
    }
}
