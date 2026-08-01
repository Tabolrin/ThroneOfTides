// Assets/_Game/2. Scripts/Systems/VFX/ExplosionEffectPool.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ThroneOfTides.Systems.VFX
{
    // Lazily-created, scene-persistent pool for one-shot impact VFX prefabs shared by card
    // controllers (Cannonball, Torch). Replaces per-cast Instantiate/Destroy with Get/Release
    // so frequently-played attack cards don't churn the GC/instantiation cost every play.
    public class ExplosionEffectPool : MonoBehaviour
    {
        private static ExplosionEffectPool _instance;
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

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
                pool = new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(prefab),
                    actionOnGet: obj => obj.SetActive(true),
                    actionOnRelease: obj => obj.SetActive(false),
                    actionOnDestroy: Destroy,
                    collectionCheck: false);
                _pools[prefab] = pool;
            }

            GameObject instance = pool.Get();
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            StartCoroutine(ReleaseAfter(pool, instance, lifetime));
        }

        private IEnumerator ReleaseAfter(ObjectPool<GameObject> pool, GameObject instance, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            pool.Release(instance);
        }
    }
}
