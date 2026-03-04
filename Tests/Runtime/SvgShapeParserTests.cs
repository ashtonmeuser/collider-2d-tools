using System.Xml;
using NUnit.Framework;
using UnityEngine;

namespace Collider2DTools.Tests
{
    public class SvgShapeParserTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void Parse_WithNullElement_ReturnsNull()
        {
            SvgShapeInfo shape = SvgShapeParser.Parse(null, 0.25f);

            Assert.That(shape, Is.Null);
        }

        [Test]
        public void Parse_Circle_ParsesAndFlipsY()
        {
            XmlElement el = LoadElement("<circle cx='3' cy='4' r='2' />");

            SvgShapeInfo shape = SvgShapeParser.Parse(el, 0.25f);

            Assert.That(shape, Is.TypeOf<SvgCircleInfo>());
            var circle = (SvgCircleInfo)shape;
            AssertVector2(circle.Center, 3f, -4f);
            Assert.That(circle.Radius, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void Parse_Rect_ParsesToBottomLeftRectCoordinates()
        {
            XmlElement el = LoadElement("<rect x='1' y='2' width='3' height='4' />");

            SvgShapeInfo shape = SvgShapeParser.Parse(el, 0.25f);

            Assert.That(shape, Is.TypeOf<SvgRectInfo>());
            var rect = ((SvgRectInfo)shape).Rect;
            AssertVector2(rect.position, 1f, -6f);
            AssertVector2(rect.size, 3f, 4f);
        }

        [Test]
        public void Parse_Polygon_ParsesPointsAndFlipsY()
        {
            XmlElement el = LoadElement("<polygon points='0,0 2,0 1,2' />");

            SvgShapeInfo shape = SvgShapeParser.Parse(el, 0.25f);

            Assert.That(shape, Is.TypeOf<SvgPolygonInfo>());
            var polygon = (SvgPolygonInfo)shape;
            Assert.That(polygon.Points.Count, Is.EqualTo(3));
            AssertVector2(polygon.Points[2], 1f, -2f);
        }

        [Test]
        public void Parse_Line_BuildsPolylineFromEndpoints()
        {
            XmlElement el = LoadElement("<line x1='1' y1='2' x2='3' y2='4' />");

            SvgShapeInfo shape = SvgShapeParser.Parse(el, 0.25f);

            Assert.That(shape, Is.TypeOf<SvgPolylineInfo>());
            var line = (SvgPolylineInfo)shape;
            Assert.That(line.Points.Count, Is.EqualTo(2));
            AssertVector2(line.Points[0], 1f, -2f);
            AssertVector2(line.Points[1], 3f, -4f);
        }

        [Test]
        public void Parse_PathClosed_ReturnsPolygon()
        {
            XmlElement el = LoadElement("<path d='M 0 0 L 2 0 L 0 2 Z' />");

            SvgShapeInfo shape = SvgShapeParser.Parse(el, 0.25f);

            Assert.That(shape, Is.TypeOf<SvgPolygonInfo>());
        }

        [Test]
        public void Parse_PathOpen_ReturnsPolyline()
        {
            XmlElement el = LoadElement("<path d='M 0 0 L 2 0' />");

            SvgShapeInfo shape = SvgShapeParser.Parse(el, 0.25f);

            Assert.That(shape, Is.TypeOf<SvgPolylineInfo>());
        }

        [Test]
        public void Parse_InvalidValues_ReturnsNull()
        {
            XmlElement circle = LoadElement("<circle cx='1' cy='2' r='0' />");
            XmlElement polyline = LoadElement("<polyline points='0,0 1,1' />");

            SvgShapeInfo invalidCircle = SvgShapeParser.Parse(circle, 0.25f);
            SvgShapeInfo invalidPolyline = SvgShapeParser.Parse(polyline, 0.25f);

            Assert.That(invalidCircle, Is.Null);
            Assert.That(invalidPolyline, Is.Null);
        }

        private static XmlElement LoadElement(string xml)
        {
            var document = new XmlDocument();
            document.LoadXml(xml);
            return document.DocumentElement;
        }

        private static void AssertVector2(Vector2 value, float expectedX, float expectedY)
        {
            Assert.That(value.x, Is.EqualTo(expectedX).Within(Tolerance));
            Assert.That(value.y, Is.EqualTo(expectedY).Within(Tolerance));
        }
    }
}
