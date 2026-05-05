using UnityEngine;

namespace Collider2DTools
{
    public enum SvgShapeKind
    {
        Svg,
        Circle,
        Rect,
        Polygon,
        Polyline
    }

    public abstract class SvgShapeInfo
    {
        public SvgShapeKind Kind { get; }
        public abstract Rect Bounds { get; }
        public float RotationDeg { get; protected set; }
        public bool HasRotation => !Mathf.Approximately(RotationDeg, 0f);

        protected SvgShapeInfo(SvgShapeKind kind)
        {
            Kind = kind;
        }

        public abstract void Bake(Matrix3x3 transform);

        protected static Vector2 Rotate2D(Vector2 v, float angleDeg)
        {
            float radians = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(
                (v.x * cos) - (v.y * sin),
                (v.x * sin) + (v.y * cos)
            );
        }

        protected static Rect ComputeBounds(Vector2 center, Vector2 size, float rotationDeg = 0f)
        {
            Vector2 extents = size * 0.5f;
            Vector2[] points =
            {
                center + Rotate2D(new Vector2(-extents.x, -extents.y), rotationDeg),
                center + Rotate2D(new Vector2(-extents.x, extents.y), rotationDeg),
                center + Rotate2D(new Vector2(extents.x, extents.y), rotationDeg),
                center + Rotate2D(new Vector2(extents.x, -extents.y), rotationDeg)
            };

            return ComputeBounds(points);
        }

        protected static Rect ComputeBounds(Vector2[] points)
        {
            if (points == null || points.Length == 0) return new Rect(Vector2.zero, Vector2.zero);

            Vector2 min = points[0];
            Vector2 max = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }

    public sealed class SvgDocumentInfo : SvgShapeInfo
    {
        public Rect Rect { get; private set; }
        public override Rect Bounds => ComputeBounds(Rect.center, Rect.size, RotationDeg);

        public SvgDocumentInfo(float width, float height)
            : base(SvgShapeKind.Svg)
        {
            Rect = new Rect(0f, -height, width, height);
        }

        public override void Bake(Matrix3x3 transform)
        {
            RotationDeg = transform.rotation;
            Vector2 scale = transform.scale;
            Vector2 translation = transform.translation;

            Vector2 center = Rotate2D(Vector2.Scale(Rect.center, scale), RotationDeg) + translation;
            Vector2 size = Vector2.Scale(Rect.size, scale);
            Rect = new Rect(center - size * 0.5f, size);
        }
    }

    public sealed class SvgCircleInfo : SvgShapeInfo
    {
        public float Radius { get; private set; }
        public override Rect Bounds => new Rect(_center - Vector2.one * Radius, Vector2.one * Radius * 2f);
        private Vector2 _center;

        public SvgCircleInfo(Vector2 center, float radius)
            : base(SvgShapeKind.Circle)
        {
            _center = center;
            Radius = radius;
        }

        public override void Bake(Matrix3x3 transform)
        {
            Vector2 scale = transform.scale;
            Vector2 translation = transform.translation;

            if (Mathf.Abs(scale.x - scale.y) > 0.0001f)
                Debug.LogWarning("SvgCircleInfo: non-uniform scale approximated using average scale.");

            _center = Rotate2D(Vector2.Scale(_center, scale), transform.rotation) + translation;
            Radius *= (scale.x + scale.y) * 0.5f;
        }
    }

    public sealed class SvgRectInfo : SvgShapeInfo
    {
        public Rect Rect { get; private set; }
        public override Rect Bounds => ComputeBounds(Rect.center, Rect.size, RotationDeg);

        public SvgRectInfo(Rect rect)
            : base(SvgShapeKind.Rect)
        {
            Rect = rect;
        }

        public override void Bake(Matrix3x3 transform)
        {
            RotationDeg = transform.rotation;
            Vector2 scale = transform.scale;
            Vector2 translation = transform.translation;

            Vector2 center = Rotate2D(Vector2.Scale(Rect.center, scale), RotationDeg) + translation;
            Vector2 size = Vector2.Scale(Rect.size, scale);
            Rect = new Rect(center - size * 0.5f, size);
        }
    }

    public sealed class SvgPolygonInfo : SvgShapeInfo
    {
        public Vector2[] Points => _points;
        public override Rect Bounds => ComputeBounds(_points);
        private readonly Vector2[] _points;

        public SvgPolygonInfo(Vector2[] points)
            : base(SvgShapeKind.Polygon)
        {
            _points = points;
        }

        public override void Bake(Matrix3x3 transform)
        {
            Vector2 scale = transform.scale;
            Vector2 translation = transform.translation;

            for (int i = 0; i < _points.Length; i++)
                _points[i] = Rotate2D(Vector2.Scale(_points[i], scale), transform.rotation) + translation;
        }
    }

    public sealed class SvgPolylineInfo : SvgShapeInfo
    {
        public Vector2[] Points => _points;
        public override Rect Bounds => ComputeBounds(_points);
        private readonly Vector2[] _points;

        public SvgPolylineInfo(Vector2[] points)
            : base(SvgShapeKind.Polyline)
        {
            _points = points;
        }

        public override void Bake(Matrix3x3 transform)
        {
            Vector2 scale = transform.scale;
            Vector2 translation = transform.translation;

            for (int i = 0; i < _points.Length; i++)
                _points[i] = Rotate2D(Vector2.Scale(_points[i], scale), transform.rotation) + translation;
        }
    }
}
