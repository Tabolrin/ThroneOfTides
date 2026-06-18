using UnityEngine;

namespace HierarchyTools.Runtime
{
    // Marks a GameObject as a pure organizational folder. Its real children are
    // exactly what shows up nested under it in the Hierarchy -- this uses Unity's
    // normal Transform parenting, so grouping, collapsing, and drag-and-drop all
    // work natively with zero custom GUI code.
    [DisallowMultipleComponent]
    [AddComponentMenu("")] // hide from Add Component; created only via the Folder tool
    public class HierarchyFolder : MonoBehaviour
    {
#if UNITY_EDITOR
        private void Awake()
        {
            // This object only exists to organize the editor. Before it's destroyed,
            // move its children up one level -- destroying a GameObject also destroys
            // everything parented under it, and we don't want to take real content
            // down with the folder.
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
            // Fallback safety if the object somehow leaks into standalone player builds
            Destroy(gameObject);
        }
#endif
    }
}