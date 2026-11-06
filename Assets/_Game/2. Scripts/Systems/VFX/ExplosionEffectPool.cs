// Assets/_Game/2. Scripts/Systems/VFX/ExplosionEffectPool.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThroneOfTides.Systems.VFX
{
    // Lazily-created, scene-persistent pool for one-shot impact VFX prefabs shared by card
    // controllers (Cannonball, Torch). Replaces per-cast Instantiate/Destroy with reuse so
    // frequently-played attack cards don't churn the GC/instantiation cost every play.
    //
    // The pool itself survives scene loads (DontDestroyOnLoad), so its pooled instances are
    // parented under it to survive too. They used to be created in the battle scene instead,
    // which destroyed them on the next scene load while the pool kept handing them out - the
    // first explosion of the next battle then threw, which aborted the calling VFX mid-impact
    // (Torch's image stayed stuck on screen). Destroyed instances are also skipped on reuse, as
    // a second line of defense.
    public class ExplosionEffectPool : MonoBehaviour
    {
        private static ExplosionEffectPool _instance;
        private readonly Dictionary<GameObject, Stack<GameObject>> _pools = new();

        private static ExplosionEffectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ExplosionEffectPool");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ExplosionEffectPool>();
                }
                return _instance;
            }
        }

        public static void PlayAt(GameObject prefab, Vector3 position, float lifetime)
        {
            if (prefab == null) return;
            Instance.Spawn(prefab, position, lifetime);
        }

        private void Spawn(GameObject prefab, Vector3 position, float lifetime)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<GameObject>();
                _pools[prefab] = pool;
            }

            GameObject instance = null;
            while (pool.Count > 0 && instance == null)
                instance = pool.Pop(); // Unity's == null also catches destroyed objects

            if (instance == null)
                instance = Instantiate(prefab, transform);

            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            instance.SetActive(true);
            StartCoroutine(ReleaseAfter(pool, instance, lifetime));
        }

        private IEnumerator ReleaseAfter(Stack<GameObject> pool, GameObject instance, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            if (instance == null) yield break;

            instance.SetActive(false);
            pool.Push(instance);
        }
    }
}
