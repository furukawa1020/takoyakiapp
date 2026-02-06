using UnityEngine;
using System.Collections.Generic;

namespace TakoyakiPhysics.Visuals
{
    /// <summary>
    /// Simple object pool for particle systems to reduce instantiation overhead
    /// </summary>
    public class ParticlePool : MonoBehaviour
    {
        private static ParticlePool _instance;
        public static ParticlePool Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("ParticlePool");
                    _instance = obj.AddComponent<ParticlePool>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private Dictionary<string, Queue<ParticleSystem>> _pools = new Dictionary<string, Queue<ParticleSystem>>();
        private Dictionary<ParticleSystem, string> _activeParticles = new Dictionary<ParticleSystem, string>();

        /// <summary>
        /// Get a particle system from the pool or create a new one
        /// </summary>
        public ParticleSystem Get(string poolName, System.Func<ParticleSystem> createFunc)
        {
            if (!_pools.ContainsKey(poolName))
            {
                _pools[poolName] = new Queue<ParticleSystem>();
            }

            ParticleSystem ps = null;
            
            // Try to get from pool
            while (_pools[poolName].Count > 0)
            {
                ps = _pools[poolName].Dequeue();
                if (ps != null) break;
            }

            // Create new if pool is empty
            if (ps == null)
            {
                ps = createFunc();
                if (ps != null)
                {
                    ps.transform.SetParent(transform);
                }
            }

            if (ps != null)
            {
                _activeParticles[ps] = poolName;
                ps.gameObject.SetActive(true);
            }

            return ps;
        }

        /// <summary>
        /// Return a particle system to the pool
        /// </summary>
        public void Return(ParticleSystem ps)
        {
            if (ps == null) return;

            if (_activeParticles.TryGetValue(ps, out string poolName))
            {
                _activeParticles.Remove(ps);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
                ps.transform.SetParent(transform);
                
                if (!_pools.ContainsKey(poolName))
                {
                    _pools[poolName] = new Queue<ParticleSystem>();
                }
                
                _pools[poolName].Enqueue(ps);
            }
        }

        /// <summary>
        /// Auto-return particle systems that have finished playing
        /// </summary>
        private void LateUpdate()
        {
            List<ParticleSystem> toReturn = new List<ParticleSystem>();

            foreach (var kvp in _activeParticles)
            {
                ParticleSystem ps = kvp.Key;
                
                // Check if particle system has stopped and all particles are gone
                if (ps != null && !ps.isPlaying && ps.particleCount == 0)
                {
                    toReturn.Add(ps);
                }
            }

            foreach (var ps in toReturn)
            {
                Return(ps);
            }
        }

        /// <summary>
        /// Clear all pools and destroy pooled objects
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    var ps = pool.Dequeue();
                    if (ps != null)
                    {
                        Destroy(ps.gameObject);
                    }
                }
            }
            _pools.Clear();
            _activeParticles.Clear();
        }
    }
}
