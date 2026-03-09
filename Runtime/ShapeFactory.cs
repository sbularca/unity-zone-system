using System.Collections.Generic;
using UnityEngine;

namespace Jovian.ZoneSystem {
    public static class ShapeFactory {
        public const int CircleSegments = 24;
        public const float DefaultRadius = 2f;
        public const float DefaultSquareHalf = 2f;
        public const float DefaultPolygonRadius = 3f;
        public const int DefaultPolygonVertices = 12;

        public static List<Vector2> CreateSquare(float halfSize = DefaultSquareHalf) {
            return new List<Vector2> {
                new(-halfSize, -halfSize),
                new(-halfSize, halfSize),
                new(halfSize, halfSize),
                new(halfSize, -halfSize)
            };
        }

        public static List<Vector2> CreateCircle(float radius = DefaultRadius, int segments = CircleSegments) {
            List<Vector2> points = new List<Vector2>(segments);
            float step = 2f * Mathf.PI / segments;
            for(int i = 0; i < segments; i++) {
                float angle = i * step;
                points.Add(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
            }
            return points;
        }

        public static List<Vector2> CreatePolygon(float radius = DefaultPolygonRadius, int vertices = DefaultPolygonVertices) {
            return CreateCircle(radius, vertices);
        }

        public static List<Vector2> CreateDefault(ZoneShape shape) {
            switch(shape) {
                case ZoneShape.Square: return CreateSquare();
                case ZoneShape.Circle: return CreateCircle();
                case ZoneShape.Polygon: return CreatePolygon();
                default: return CreateSquare();
            }
        }

        public static void RegenerateCircle(ZoneData data) {
            data.polygon.Clear();
            data.polygon.AddRange(CreateCircle(data.circleRadius));
        }
    }
}
