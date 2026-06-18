using UnityEngine;

namespace HierarchyTools.Runtime
{
    /// <summary>
    /// Marks a GameObject as an organizational node in the Hierarchy. Scene-baked folders are
    /// dissolved by the editor scene processor before they ever run, so this component normally
    /// carries no runtime cost. The Awake fallback below handles the one case the processor
    /// cannot reach: a folder living inside a prefab that is instantiated at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    [DefaultExecutionOrder(-32000)] // Run before user scripts so children are reparented first.
    public class HierarchyFolder : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnValidate()
        {
            // A folder used as the root of a prefab asset is dangerous: it deletes itself at
            // runtime, which would break any Instantiate() call targeting that prefab. Warn the
            // author so they wrap it in a normal GameObject instead.
            if (transform.parent == null && UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            {
                Debug.LogWarning(
                    $"[Hierarchy Tools] '{gameObject.name}' is the root of a Prefab. Hierarchy Folders " +
                    "destroy themselves at runtime, which will break any code calling Instantiate() on " +
                    "this prefab. Wrap the folder inside an empty GameObject.", this);
            }
        }
#endif

        private void Awake()
        {
            // Reaching here means this folder bypassed the editor scene processor, which happens
            // when a prefab containing a folder is instantiated dynamically. Unpack on the fly to
            // restore a standard hierarchy before user scripts execute.
            Transform t = transform;
            Transform newParent = t.parent;

            int insertIndex = t.GetSiblingIndex();
            int childCount = t.childCount;

            for (int i = 0; i < childCount; i++)
            {
                // Always take child 0: reparenting removes it, shifting the next child into slot 0.
                Transform child = t.GetChild(0);
                child.SetParent(newParent, true);
                child.SetSiblingIndex(insertIndex + i);
            }

            Destroy(gameObject);
        }
    }
}