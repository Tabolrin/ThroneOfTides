using UnityEngine;

namespace HierarchyTools.Runtime
{
    /// <summary>
    /// Data container for assigning custom icons to GameObjects within the editor Hierarchy.
    /// Safely strips itself during initialization to ensure zero runtime performance cost.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class HierarchySymbol : MonoBehaviour
    {
        public string symbolKey;

        private void Awake()
        {
            // Component is strictly editor-facing; destroy instance at runtime
            Destroy(this);
        }
    }
}