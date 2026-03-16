using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace Collider2DTools
{
    internal static class SvgShapeParser
    {
        private static readonly Regex PointNumberRegex = new Regex(@"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled);

        public static SvgShapeInfo Parse(XmlElement el, float curveUnitResolution)
        {
            if (el == null) return null;

            string name = el.LocalName;

            if (name == "svg")
            {
                if (!TryParseFloat(el, "width", out float width) || !TryParseFloat(el, "height", out float height) ||
                    width <= 0f || height <= 0f)
                    return null;

                return new SvgDocumentInfo(width, height);
            }

            if (name == "circle")
            {
                if (!TryParseFloat(el, "cx", out float cx, 0f) || !TryParseFloat(el, "cy", out float cy, 0f) ||
                    !TryParseFloat(el, "r", out float r) || r <= 0f)
                    return null;

                return new SvgCircleInfo(new Vector2(cx, -cy), r);
            }

            if (name == "rect")
            {
                if (!TryParseFloat(el, "x", out float x, 0f) || !TryParseFloat(el, "y", out float y, 0f) ||
                    !TryParseFloat(el, "width", out float w) || !TryParseFloat(el, "height", out float h) || w <= 0f || h <= 0f)
                    return null;

                return new SvgRectInfo(new Rect(new Vector2(x, -y - h), new Vector2(w, h)));
            }

            if (name == "polygon")
            {
                if (!TryParsePoints(el.GetAttribute("points"), out Vector2[] points) || points.Length < 3)
                    return null;

                return new SvgPolygonInfo(points);
            }

            if (name == "polyline")
            {
                if (!TryParsePoints(el.GetAttribute("points"), out Vector2[] points) || points.Length < 2)
                    return null;

                return new SvgPolylineInfo(points);
            }

            if (name == "line")
            {
                if (!TryParseFloat(el, "x1", out float x1, 0f) || !TryParseFloat(el, "y1", out float y1, 0f) ||
                    !TryParseFloat(el, "x2", out float x2, 0f) || !TryParseFloat(el, "y2", out float y2, 0f))
                    return null;

                return new SvgPolylineInfo(new[]
                {
                    new Vector2(x1, -y1),
                    new Vector2(x2, -y2)
                });
            }

            if (name == "path")
            {
                if (!SvgPathParser.Parse(el.GetAttribute("d"), curveUnitResolution, out List<Vector2> points, out bool isClosed))
                    return null;

                return isClosed ? new SvgPolygonInfo(points.ToArray()) : new SvgPolylineInfo(points.ToArray());
            }

            return null;
        }

        private static bool TryParseFloat(XmlElement el, string name, out float value, float? defaultValue = null)
        {
            var attribute = el.GetAttribute(name);
            if (string.IsNullOrWhiteSpace(attribute) && defaultValue is float v)
            {
                value = v;
                return true;
            }
            return float.TryParse(attribute, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParsePoints(string pointsAttr, out Vector2[] points)
        {
            points = null;
            if (string.IsNullOrWhiteSpace(pointsAttr)) return false;

            MatchCollection matches = PointNumberRegex.Matches(pointsAttr);
            if (matches.Count < 6 || (matches.Count % 2) != 0) return false;

            points = new Vector2[matches.Count / 2];

            for (int i = 0, pointIndex = 0; i < matches.Count; i += 2, pointIndex++)
            {
                if (!float.TryParse(matches[i].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                    return false;
                if (!float.TryParse(matches[i + 1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                    return false;
                points[pointIndex] = new Vector2(x, -y);
            }

            return true;
        }
    }
}
