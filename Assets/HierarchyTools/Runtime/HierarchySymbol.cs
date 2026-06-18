using UnityEngine;

namespace HierarchyTools.Runtime
{
    /// <summary>
    /// Editor-only metadata that links a GameObject to a custom Hierarchy icon. The scene
    /// processor strips this component before builds and Play Mode, so it normally never runs.
    /// The Awake fallback handles the remaining case: a symbol baked into a prefab that is
    /// instantiated dynamically at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class HierarchySymbol : MonoBehaviour
    {
        public string symbolKey;

        private void Awake()
        {
            // Strictly editor-facing data with no runtime purpose; remove the instance if it
            // ever survives into a running build.
            Destroy(this);
        }
    }
}