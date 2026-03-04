using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Collider2DTools.Tests
{
    public class SvgPathParserTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Parse_WithEmptyInput_ReturnsFalse()
        {
            bool ok = SvgPathParser.Parse("", 0.25f, out List<Vector2> points, out bool isClosed);

            Assert.That(ok, Is.False);
            Assert.That(points, Is.Empty);
            Assert.That(isClosed, Is.False);
        }

        [Test]
        public void Parse_WithNoStartingCommand_ReturnsFalse()
        {
            bool ok = SvgPathParser.Parse("0 0 L 1 1", 0.25f, out _, out _);

            Assert.That(ok, Is.False);
        }

        [Test]
        public void Parse_LinePath_ParsesPointsAndFlipsY()
        {
            bool ok = SvgPathParser.Parse("M 0 0 L 2 3", 0.25f, out List<Vector2> points, out bool isClosed);

            Assert.That(ok, Is.True);
            Assert.That(isClosed, Is.False);
            Assert.That(points.Count, Is.EqualTo(2));
            Assert.That(points[0].x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(points[0].y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(points[1].x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(points[1].y, Is.EqualTo(-3f).Within(Tolerance));
        }

        [Test]
        public void Parse_ClosedPath_RemovesDuplicateTerminalPoint()
        {
            bool ok = SvgPathParser.Parse("M 0 0 L 2 0 L 0 2 L 0 0 Z", 0.25f, out List<Vector2> points, out bool isClosed);

            Assert.That(ok, Is.True);
            Assert.That(isClosed, Is.True);
            Assert.That(points.Count, Is.EqualTo(3));
            Assert.That(points[0].x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(points[0].y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(points[1].x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(points[1].y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Parse_RelativeCommands_AreAccumulatedFromCurrentPoint()
        {
            bool ok = SvgPathParser.Parse("M 1 1 l 2 0 v 3 h -1", 0.25f, out List<Vector2> points, out bool isClosed);

            Assert.That(ok, Is.True);
            Assert.That(isClosed, Is.False);
            Assert.That(points.Count, Is.EqualTo(4));
            Assert.That(points[3].x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(points[3].y, Is.EqualTo(-4f).Within(Tolerance));
        }

        [Test]
        public void Parse_CubicCurve_EndsAtCurveEndpoint()
        {
            bool ok = SvgPathParser.Parse("M 0 0 C 0 1 1 1 1 0", 0.5f, out List<Vector2> points, out bool isClosed);

            Assert.That(ok, Is.True);
            Assert.That(isClosed, Is.False);
            Assert.That(points.Count, Is.GreaterThanOrEqualTo(2));
            Vector2 last = points[points.Count - 1];
            Assert.That(last.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(last.y, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
