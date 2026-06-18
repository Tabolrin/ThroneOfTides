using UnityEngine;

namespace HierarchyTools.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class HierarchySymbol : MonoBehaviour
    {
        public string symbolKey;

        private void Awake()
        {
            // Strips the component cleanly at runtime with zero game overhead
            Destroy(this);
        }
    }
}