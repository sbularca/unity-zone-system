using System.Collections.Generic;
using UnityEngine;

namespace Jovian.ZoneSystem {
    public class ZoneSystem {
        private readonly ZonesObjectHolder zonesObjectHolder;
        public ZoneSystem(ZonesObjectHolder zonesObjectHolder) {
            this.zonesObjectHolder = zonesObjectHolder;
            Refresh();
        }

        /// <summary>
        ///     Returns the resolved ZoneContext at the given world position.
        ///     This is the only call your encounter/travel system needs to make.
        /// </summary>
        public ZoneContext QueryZone(Vector3 worldPos) {
            var overlapping = GetOverlappingZones(worldPos);
            return ZoneResolver.Resolve(overlapping);
        }

        /// <summary>
        ///     Returns all ZoneData assets whose polygons contain worldPos.
        ///     Ordered by descending priority — useful if you need the raw list
        ///     before resolution (e.g. for debug UI).
        /// </summary>
        public List<ZoneData> GetOverlappingZones(Vector3 worldPos) {
            var result = new List<ZoneData>();

            foreach(var zone in zonesObjectHolder.Zones) {
                if(zone == null || zone.data == null) {
                    continue;
                }
                if(zone.Contains(worldPos, zonesObjectHolder.mapPlane)) {
                    result.Add(zone.data);
                }
            }

            result.Sort((a, b) => b.priority.CompareTo(a.priority));
            return result;
        }

        /// <summary>
        ///     Returns true if worldPos is inside any zone that is an Override+Safe zone
        ///     (i.e. a town or safe area). Cheap shortcut before rolling encounters.
        /// </summary>
        public bool IsInSafeZone(Vector3 worldPos) {
            foreach(var zone in zonesObjectHolder.Zones) {
                if(zone == null || zone.data == null) {
                    continue;
                }
                if(zone.data.role == ZoneRole.Override &&
                    zone.data.isSafeZone &&
                    zone.Contains(worldPos, zonesObjectHolder.mapPlane)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Registers a single ZoneInstance dynamically (e.g. spawned at runtime).
        /// </summary>
        internal void Register(ZoneInstance zone) {
            if(!zonesObjectHolder.Zones.Contains(zone)) {
                zone.RebuildBoundsCache();
                zonesObjectHolder.Zones.Add(zone);
            }
        }

        /// <summary>
        ///     Unregisters a ZoneInstance (e.g. before it is destroyed at runtime).
        /// </summary>
        public void Unregister(ZoneInstance zone) {
            zonesObjectHolder.Zones.Remove(zone);
        }

        /// <summary>
        ///     Re-scans the scene for all ZoneInstances and rebuilds their bounds caches.
        ///     Call this if you add or remove zones at runtime.
        /// </summary>
        private void Refresh() {
            zonesObjectHolder.Zones.Clear();
            ZoneInstance[] found = Object.FindObjectsByType<ZoneInstance>(FindObjectsSortMode.None);
            foreach(ZoneInstance z in found) {
                z.RebuildBoundsCache();
                zonesObjectHolder.Zones.Add(z);
            }
        }
    }
}
