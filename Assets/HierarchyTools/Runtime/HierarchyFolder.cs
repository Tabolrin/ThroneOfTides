using UnityEngine;

namespace HierarchyTools.Runtime
{
    /// <summary>
    /// Acts as an editor-only organizational node in the Hierarchy.
    /// Automatically reparents children and self-destructs at runtime to maintain clean architecture.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")] 
    public class HierarchyFolder : MonoBehaviour
    {
#if UNITY_EDITOR
        private void Awake()
        {
            // Unpack hierarchy contents to the parent level to prevent target destruction
            Transform t = transform;
            Transform newParent = t.parent;

            while (t.childCount > 0)
            {
                t.GetChild(0).SetParent(newParent, true);
            }

            Destroy(gameObject);
        }
#else
        private void Awake()
        {
            // Failsafe cleanup for standalone builds
            Destroy(gameObject);
        }
#endif
    }
}