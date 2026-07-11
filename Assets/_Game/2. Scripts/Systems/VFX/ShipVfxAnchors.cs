// Assets/_Game/2. Scripts/Systems/VFX/ShipVfxAnchors.cs
using System.Collections.Generic;
using UnityEngine;
using ThroneOfTides.Core;

namespace ThroneOfTides.Systems
{
    /// <summary>
    /// Caches this ship's VFXSpawnPosition markers once so card-play effects can resolve a
    /// spawn transform by anchor type without any per-play GetComponentsInChildren/Find calls.
    /// Place on each ship root (PlayerShip, EnemyShip) alongside its VFXSpawnPosition children.
    /// Does not handle spawning or presentation logic itself — see CardPresentationPlayer.
    /// </summary>
    public class ShipVfxAnchors : MonoBehaviour
    {
        private readonly Dictionary<VfxAnchorType, Transform> _anchors = new Dictionary<VfxAnchorType, Transform>();

        private void Awake()
        {
            foreach (var marker in GetComponentsInChildren<VFXSpawnPosition>(true))
            {
                if (_anchors.ContainsKey(marker.Type))
                {
                    Debug.LogWarning(
                        $"{name}: multiple VFXSpawnPosition markers of type {marker.Type} found — " +
                        $"keeping the first, ignoring '{marker.name}'.", marker);
                    continue;
                }

                _anchors.Add(marker.Type, marker.transform);
            }
        }

        /// <summary>
        /// Resolves the transform for the given anchor type on this ship. Falls back to this
        /// ship's own root transform (with a warning) if no marker of that type is placed yet —
        /// keeps effects spawning somewhere sensible rather than throwing mid-match.
        /// </summary>
        public Transform Get(VfxAnchorType anchorType)
        {
            if (_anchors.TryGetValue(anchorType, out var anchor))
                return anchor;

            Debug.LogWarning(
                $"{name}: no VFXSpawnPosition marker of type {anchorType} found — falling back to ship root.",
                this);
            return transform;
        }
    }
}
