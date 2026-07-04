using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using HierarchyTools.Runtime;

namespace HierarchyTools.Editor
{
    /// <summary>
    /// Flattens HierarchyFolders and removes HierarchySymbols from a scene before it is built
    /// or entered in Play Mode, so neither component carries any runtime cost. Hooks into the
    /// build/Play pipeline through IProcessSceneWithReport.
    /// </summary>
    public class HierarchyFolderProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => -1000;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // FindObjectsByType returns objects from every loaded scene. This callback fires
            // once per scene (during builds and on Play Mode entry), so results must be scoped
            // to the scene being processed; otherwise a pass could mutate objects belonging to
            // a scene that another pass is responsible for.
            List<HierarchyFolder> folders = Object
                .FindObjectsByType<HierarchyFolder>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(f => f != null && f.gameObject.scene == scene)
                // Deepest folders are unpacked first so a parent folder sees its final child
                // layout only after its nested folders have already been dissolved.
                .OrderByDescending(f => GetTransformDepth(f.transform))
                .ToList();

            foreach (HierarchyFolder folder in folders)
            {
                if (folder == null)
                    continue;

                UnpackFolder(folder.transform);
                Object.DestroyImmediate(folder.gameObject);
            }

            // Symbols are pure editor metadata; strip them here so they never reach a build,
            // mirroring the zero-cost guarantee the folders provide.
            HierarchySymbol[] symbols = Object
                .FindObjectsByType<HierarchySymbol>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (HierarchySymbol symbol in symbols)
            {
                if (symbol != null && symbol.gameObject.scene == scene)
                    Object.DestroyImmediate(symbol);
            }
        }

        /// <summary>
        /// Moves every child of <paramref name="folder"/> up to the folder's parent, preserving
        /// each child's world transform and the folder's original position among its siblings.
        /// </summary>
        private static void UnpackFolder(Transform folder)
        {
            Transform newParent = folder.parent;
            int insertIndex = folder.GetSiblingIndex();
            int childCount = folder.childCount;

            for (int i = 0; i < childCount; i++)
            {
                // Always take child 0: reparenting removes it from the folder, so the next
                // remaining child shifts down into slot 0.
                Transform child = folder.GetChild(0);
                child.SetParent(newParent, true);
                child.SetSiblingIndex(insertIndex + i);
            }
        }

        private static int GetTransformDepth(Transform t)
        {
            int depth = 0;
            while (t.parent != null)
            {
                depth++;
                t = t.parent;
            }
            return depth;
        }
    }
}