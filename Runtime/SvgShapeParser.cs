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

            if (name == "circle")
            {
                if (!TryParseAttr(el, "cx", out float cx) || !TryParseAttr(el, "cy", out float cy) ||
                    !TryParseAttr(el, "r", out float r) || r <= 0f)
                    return null;

                return new SvgCircleInfo(new Vector2(cx, -cy), r);
            }

            if (name == "rect")
            {
                if (!TryParseAttr(el, "x", out float x) || !TryParseAttr(el, "y", out float y) ||
                    !TryParseAttr(el, "width", out float w) || !TryParseAttr(el, "height", out float h) || w <= 0f || h <= 0f)
                    return null;

                return new SvgRectInfo(new Rect(new Vector2(x, -y - h), new Vector2(w, h)));
            }

            if (name == "polygon")
            {
                if (!TryParsePoints(el.GetAttribute("points"), out List<Vector2> points) || points.Count < 3)
                    return null;

                return new SvgPolygonInfo(points);
            }

            if (name == "polyline")
            {
                if (!TryParsePoints(el.GetAttribute("points"), out List<Vector2> points) || points.Count < 2)
                    return null;

                return new SvgPolylineInfo(points);
            }

            if (name == "line")
            {
                if (!TryParseAttr(el, "x1", out float x1) || !TryParseAttr(el, "y1", out float y1) ||
                    !TryParseAttr(el, "x2", out float x2) || !TryParseAttr(el, "y2", out float y2))
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

                return isClosed ? new SvgPolygonInfo(points) : new SvgPolylineInfo(points);
            }

            return null;
        }

        private static bool TryParseAttr(XmlElement el, string name, out float value)
            => float.TryParse(el.GetAttribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static bool TryParsePoints(string pointsAttr, out List<Vector2> points)
        {
            points = new List<Vector2>();
            if (string.IsNullOrWhiteSpace(pointsAttr)) return false;

            MatchCollection matches = PointNumberRegex.Matches(pointsAttr);
            if (matches.Count < 6 || (matches.Count % 2) != 0) return false;

            for (int i = 0; i < matches.Count; i += 2)
            {
                if (!float.TryParse(matches[i].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                    return false;
                if (!float.TryParse(matches[i + 1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                    return false;
                points.Add(new Vector2(x, -y));
            }

            return true;
        }
    }
}
