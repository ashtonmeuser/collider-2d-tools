using System.Collections.Generic;
using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Collider2DTools.Tests
{
    public class SvgShapeInfoTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void CircleInfo_Bake_AppliesTransformAndScalesRadius()
        {
            var circle = new SvgCircleInfo(new Vector2(1f, 2f), 3f);
            Matrix3x3 transform = Matrix3x3.Matrix(new[] { 2f, 0f, 0f, 2f, 3f, 4f });

            circle.Bake(transform);

            AssertVector2(circle.Center, 5f, 8f);
            Assert.That(circle.Radius, Is.EqualTo(6f).Within(Tolerance));
        }

        [Test]
        public void CircleInfo_Bake_NonUniformScale_LogsWarning()
        {
            var circle = new SvgCircleInfo(new Vector2(1f, 1f), 2f);
            Matrix3x3 transform = Matrix3x3.Matrix(new[] { 2f, 0f, 0f, 4f, 0f, 0f });

            LogAssert.Expect(LogType.Warning, new Regex("non-uniform scale approximated"));
            circle.Bake(transform);

            Assert.That(circle.Radius, Is.EqualTo(6f).Within(Tolerance));
            AssertVector2(circle.Center, 2f, 4f);
        }

        [Test]
        public void RectInfo_Bake_UpdatesRotationCenterAndSize()
        {
            var rect = new SvgRectInfo(new Rect(0f, 0f, 2f, 2f));
            Matrix3x3 transform = Matrix3x3.Matrix(new[] { 0f, 1f, -1f, 0f, 1f, 2f });

            rect.Bake(transform);

            Assert.That(rect.RotationDeg, Is.EqualTo(90f).Within(Tolerance));
            Assert.That(rect.HasRotation, Is.True);
            AssertVector2(rect.Center, 0f, 3f);
            AssertVector2(rect.Size, 2f, 2f);
        }

        [Test]
        public void PolygonInfo_Bake_TransformsPointsAndRecomputesCenter()
        {
            var polygon = new SvgPolygonInfo(new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(2f, 0f),
                new Vector2(0f, 2f)
            });
            Matrix3x3 transform = Matrix3x3.Matrix(new[] { 2f, 0f, 0f, 2f, 1f, 3f });

            polygon.Bake(transform);

            AssertVector2(polygon.Points[0], 1f, 3f);
            AssertVector2(polygon.Points[1], 5f, 3f);
            AssertVector2(polygon.Points[2], 1f, 7f);
            AssertVector2(polygon.Center, 7f / 3f, 13f / 3f);
        }

        [Test]
        public void PolylineInfo_Bake_TransformsPointsAndRecomputesCenter()
        {
            var polyline = new SvgPolylineInfo(new List<Vector2>
            {
                new Vector2(1f, 1f),
                new Vector2(3f, 1f)
            });
            Matrix3x3 transform = Matrix3x3.Matrix(new[] { 1f, 0f, 0f, 1f, -2f, 4f });

            polyline.Bake(transform);

            AssertVector2(polyline.Points[0], -1f, 5f);
            AssertVector2(polyline.Points[1], 1f, 5f);
            AssertVector2(polyline.Center, 0f, 5f);
        }

        private static void AssertVector2(Vector2 value, float expectedX, float expectedY)
        {
            Assert.That(value.x, Is.EqualTo(expectedX).Within(Tolerance));
            Assert.That(value.y, Is.EqualTo(expectedY).Within(Tolerance));
        }
    }
}
