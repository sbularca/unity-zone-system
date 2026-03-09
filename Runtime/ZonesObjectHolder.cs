using System.Collections.Generic;
using UnityEngine;

namespace Jovian.ZoneSystem {
    public class ZonesObjectHolder: MonoBehaviour {
        internal List<ZoneInstance> Zones { get; } = new();
        public MapPlane mapPlane;

        public IReadOnlyList<ZoneInstance> AllZones => Zones;

        private void Awake() {
            Refresh();
        }

#if UNITY_EDITOR
        private void OnValidate() {
            Refresh();
        }
#endif

        /// <summary>
        ///     Re-scans the scene for all ZoneInstances and rebuilds their bounds caches.
        ///     Call this if you add or remove zones at runtime.
        /// </summary>
        private void Refresh() {
            Zones.Clear();
            var found = FindObjectsByType<ZoneInstance>(FindObjectsSortMode.None);
            foreach(var z in found) {
                z.RebuildBoundsCache();
                Zones.Add(z);
            }
        }
    }

}
