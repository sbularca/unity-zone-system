using UnityEngine;

namespace Jovian.ZoneSystem {
    /// <summary>
    ///     Defines which two world axes the map (and zone polygons) lie on.
    ///     XY = flat sprite / UI map (Z is depth)
    ///     XZ = 3D world map (Y is up) ← standard Unity 3D
    ///     YZ = side-on map (X is depth)
    /// </summary>
    public enum MapPlane {
        XY,
        XZ,
        YZ
    }

    public static class MapPlaneUtility {
        /// <summary>
        ///     Projects a 3D world position onto the chosen map plane,
        ///     returning a 2D point suitable for polygon testing.
        /// </summary>
        public static Vector2 ProjectToPlane(Vector3 worldPos, MapPlane plane) {
            switch(plane) {
                case MapPlane.XY: return new Vector2(worldPos.x, worldPos.y);
                case MapPlane.XZ: return new Vector2(worldPos.x, worldPos.z);
                case MapPlane.YZ: return new Vector2(worldPos.y, worldPos.z);
                default: return new Vector2(worldPos.x, worldPos.y);
            }
        }

        /// <summary>
        ///     Reconstructs a 3D world position from a 2D polygon point on the chosen plane.
        ///     The depth value fills the axis not covered by the plane.
        /// </summary>
        public static Vector3 UnprojectFromPlane(Vector2 point, MapPlane plane, float depth = 0f) {
            switch(plane) {
                case MapPlane.XY: return new Vector3(point.x, point.y, depth);
                case MapPlane.XZ: return new Vector3(point.x, depth, point.y);
                case MapPlane.YZ: return new Vector3(depth, point.x, point.y);
                default: return new Vector3(point.x, point.y, depth);
            }
        }
    }
}
