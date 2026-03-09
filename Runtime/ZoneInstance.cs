using System.Collections.Generic;
using UnityEngine;

namespace Jovian.ZoneSystem {
    public class ZoneInstance : MonoBehaviour {
        public ZoneData data;
        private Vector2 _boundsMax;

        // Cached AABB for fast pre-rejection (rebuilt when data changes)
        private Vector2 _boundsMin;
        private bool _boundsValid;

        private void Awake() {
            RebuildBoundsCache();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if(data == null || data.polygon == null || data.polygon.Count < 2) {
                return;
            }

            MapPlane plane = MapPlane.XZ;
            ZonesObjectHolder mgr = FindFirstObjectByType<ZonesObjectHolder>();
            if(mgr != null) {
                plane = mgr.mapPlane;
            }

            float depth = plane == MapPlane.XZ ? transform.position.y
                : plane == MapPlane.YZ ? transform.position.x
                : transform.position.z;

            Vector2 origin = MapPlaneUtility.ProjectToPlane(transform.position, plane);
            Gizmos.color = data.debugColor;
            List<Vector2> pts = data.polygon;

            for(int i = 0; i < pts.Count; i++) {
                Vector3 a = MapPlaneUtility.UnprojectFromPlane(pts[i] + origin, plane, depth);
                Vector3 b = MapPlaneUtility.UnprojectFromPlane(pts[(i + 1) % pts.Count] + origin, plane, depth);
                Gizmos.DrawLine(a, b);
            }
        }
#endif
        private void OnValidate() {
            RebuildBoundsCache();
        }

        /// <summary>
        ///     Rebuilds the AABB cache from the current polygon data.
        ///     Called automatically on Awake/Validate; also call this in the
        ///     editor after modifying polygon points.
        /// </summary>
        public void RebuildBoundsCache() {
            if(data == null || data.polygon == null || data.polygon.Count < 3) {
                _boundsValid = false;
                return;
            }

            (_boundsMin, _boundsMax) = PolygonUtils.Bounds(data.polygon);
            _boundsValid = true;
        }

        /// <summary>
        ///     Returns true if the given world position is inside this zone's polygon.
        ///     Plane controls which two axes are used for the 2D projection.
        /// </summary>
        public bool Contains(Vector3 worldPos, MapPlane plane) {
            if(data == null || data.polygon == null || data.polygon.Count < 3) {
                return false;
            }

            Vector2 projected = MapPlaneUtility.ProjectToPlane(worldPos, plane);
            Vector2 origin = MapPlaneUtility.ProjectToPlane(transform.position, plane);
            Vector2 localPoint = projected - origin;

            // Fast AABB reject before running the full ray-cast
            if(_boundsValid && !PolygonUtils.PointInBounds(localPoint, _boundsMin, _boundsMax)) {
                return false;
            }

            return PolygonUtils.PointInPolygon(localPoint, data.polygon);
        }
    }
}
