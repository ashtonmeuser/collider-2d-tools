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
        public Vector2 Center { get; protected set; }
        public float RotationDeg { get; protected set; }

        public bool HasRotation => !Mathf.Approximately(RotationDeg, 0f);

        protected SvgShapeInfo(SvgShapeKind kind, Vector2 center)
        {
            Kind = kind;
            Center = center;
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
    }

    public sealed class SvgDocumentInfo : SvgShapeInfo
    {
        public float Width { get; private set; }
        public float Height { get; private set; }

        public SvgDocumentInfo(float width, float height)
            : base(SvgShapeKind.Svg, new Vector2(width * 0.5f, -height * 0.5f))
        {
            Width = width;
            Height = height;
        }

        public override void Bake(Matrix3x3 transform)
        {
            Vector2 scale = transform.scale;
            Vector2 translation = transform.translation;

            Width *= scale.x;
            Height *= scale.y;
            Center = Rotate2D(Vector2.Scale(Center, scale), transform.rotation) + translation;
        }
    }

    public sealed class SvgCircleInfo : SvgShapeInfo
    {
        public float Radius { get; private set; }

        public SvgCircleInfo(Vector2 center, float radius)
            : base(SvgShapeKind.Circle, center)
        {
            Radius = radius;
        }

        public override void Bake(Matrix3x3 transform)
        {
            Vector2 scale = transform.scale;
            Vector2 translation = transform.translation;

            if (Mathf.Abs(scale.x - scale.y) > 0.0001f)
                Debug.LogWarning("SvgCircleInfo: non-uniform scale approximated using average scale.");

            Center = Rotate2D(Vector2.Scale(Center, scale), transform.rotation) + translation;
            Radius *= (scale.x + scale.y) * 0.5f;
        }
    }

    public sealed class SvgRectInfo : SvgShapeInfo
    {
        public Rect Rect { get; private set; }
        public Vector2 Size => Rect.size;

        public SvgRectInfo(Rect rect)
            : base(SvgShapeKind.Rect, rect.center)
        {
            Rect = rect;
        }

        public override void Bake(Matrix3x3 transform)
        {
            RotationDeg = transform.rotation;
            Vector2 scale = transform.scale;
            Vector2 translation = transform.translation;

            Vector2 center = Rotate2D(Vector2.Scale(Center, scale), RotationDeg) + translation;
            Vector2 size = Vector2.Scale(Size, scale);
            Rect = new Rect(center - size * 0.5f, size);
            Center = Rect.center;
        }
    }

    public sealed class SvgPolygonInfo : SvgShapeInfo
    {
        public Vector2[] Points => _points;
        private readonly Vector2[] _points;

        public SvgPolygonInfo(Vector2[] points)
            : base(SvgShapeKind.Polygon, ComputeCenter(points))
        {
            _points = points;
        }

        public override void Bake(Matrix3x3 transform)
        {
            Vector2 scale = transform.scale;
            Vector2 translation = transform.translation;

            for (int i = 0; i < _points.Length; i++)
                _points[i] = Rotate2D(Vector2.Scale(_points[i], scale), transform.rotation) + translation;

            Center = ComputeCenter(_points);
        }

        private static Vector2 ComputeCenter(Vector2[] points)
        {
            if (points == null || points.Length == 0) return Vector2.zero;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < points.Length; i++)
                sum += points[i];
            return sum / points.Length;
        }
    }

    public sealed class SvgPolylineInfo : SvgShapeInfo
    {
        public Vector2[] Points => _points;
        private readonly Vector2[] _points;

        public SvgPolylineInfo(Vector2[] points)
            : base(SvgShapeKind.Polyline, ComputeCenter(points))
        {
            _points = points;
        }

        public override void Bake(Matrix3x3 transform)
        {
            Vector2 scale = transform.scale;
            Vector2 translation = transform.translation;

            for (int i = 0; i < _points.Length; i++)
                _points[i] = Rotate2D(Vector2.Scale(_points[i], scale), transform.rotation) + translation;

            Center = ComputeCenter(_points);
        }

        private static Vector2 ComputeCenter(Vector2[] points)
        {
            if (points == null || points.Length == 0) return Vector2.zero;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < points.Length; i++)
                sum += points[i];
            return sum / points.Length;
        }
    }
}
