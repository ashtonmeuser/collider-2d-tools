using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Collider2DTools
{
    internal static class SvgPathParser
    {
        private static readonly Regex PathTokenRegex = new Regex(@"[MmLlHhVvCcSsQqTtZz]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled);

        public static bool Parse(string d, float curveUnitResolution, out List<Vector2> points, out bool isClosed)
        {
            points = new List<Vector2>();
            isClosed = false;
            if (string.IsNullOrWhiteSpace(d)) return false;

            MatchCollection tokens = PathTokenRegex.Matches(d);
            if (tokens.Count == 0) return false;

            int i = 0;
            char cmd = '\0';
            char prevCmd = '\0';
            Vector2 current = Vector2.zero;
            Vector2 subpathStart = Vector2.zero;
            Vector2 lastCubicControl = Vector2.zero;
            Vector2 lastQuadraticControl = Vector2.zero;
            bool hasLastCubicControl = false;
            bool hasLastQuadraticControl = false;
            bool hasCurrent = false;

            while (i < tokens.Count)
            {
                string token = tokens[i].Value;
                if (IsCommandToken(token))
                {
                    cmd = token[0];
                    i++;
                }
                else if (cmd == '\0')
                {
                    return false;
                }

                switch (cmd)
                {
                    case 'M':
                    case 'm':
                    {
                        if (!TryReadPoint(tokens, ref i, out Vector2 p)) return false;
                        current = cmd == 'm' && hasCurrent ? current + p : p;
                        subpathStart = current;
                        points.Add(current);
                        hasCurrent = true;
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;

                        // Subsequent pairs after M/m are implicit L/l.
                        cmd = cmd == 'm' ? 'l' : 'L';
                        while (TryReadPoint(tokens, ref i, out p))
                        {
                            current = cmd == 'l' ? current + p : p;
                            points.Add(current);
                            hasLastCubicControl = false;
                            hasLastQuadraticControl = false;
                        }
                        break;
                    }
                    case 'L':
                    case 'l':
                    {
                        Vector2 p;
                        bool any = false;
                        while (TryReadPoint(tokens, ref i, out p))
                        {
                            current = cmd == 'l' ? current + p : p;
                            points.Add(current);
                            hasCurrent = true;
                            any = true;
                            hasLastCubicControl = false;
                            hasLastQuadraticControl = false;
                        }
                        if (!any) return false;
                        break;
                    }
                    case 'H':
                    case 'h':
                    {
                        bool any = false;
                        while (TryReadNumber(tokens, ref i, out float x))
                        {
                            current.x = cmd == 'h' ? current.x + x : x;
                            points.Add(current);
                            hasCurrent = true;
                            any = true;
                            hasLastCubicControl = false;
                            hasLastQuadraticControl = false;
                        }
                        if (!any) return false;
                        break;
                    }
                    case 'V':
                    case 'v':
                    {
                        bool any = false;
                        while (TryReadNumber(tokens, ref i, out float y))
                        {
                            current.y = cmd == 'v' ? current.y + y : y;
                            points.Add(current);
                            hasCurrent = true;
                            any = true;
                            hasLastCubicControl = false;
                            hasLastQuadraticControl = false;
                        }
                        if (!any) return false;
                        break;
                    }
                    case 'C':
                    case 'c':
                    {
                        bool any = false;
                        while (TryReadPoint(tokens, ref i, out Vector2 c1) &&
                               TryReadPoint(tokens, ref i, out Vector2 c2) &&
                               TryReadPoint(tokens, ref i, out Vector2 p))
                        {
                            Vector2 p0 = current;
                            Vector2 p1 = cmd == 'c' ? current + c1 : c1;
                            Vector2 p2 = cmd == 'c' ? current + c2 : c2;
                            Vector2 p3 = cmd == 'c' ? current + p : p;
                            AppendCubicBezier(points, p0, p1, p2, p3, curveUnitResolution);
                            current = p3;
                            hasCurrent = true;
                            any = true;
                            lastCubicControl = p2;
                            hasLastCubicControl = true;
                            hasLastQuadraticControl = false;
                        }
                        if (!any) return false;
                        break;
                    }
                    case 'S':
                    case 's':
                    {
                        bool any = false;
                        while (TryReadPoint(tokens, ref i, out Vector2 c2) &&
                               TryReadPoint(tokens, ref i, out Vector2 p))
                        {
                            Vector2 p0 = current;
                            Vector2 p1 = (prevCmd is 'C' or 'c' or 'S' or 's') && hasLastCubicControl
                                ? (current * 2f) - lastCubicControl
                                : current;
                            Vector2 p2 = cmd == 's' ? current + c2 : c2;
                            Vector2 p3 = cmd == 's' ? current + p : p;
                            AppendCubicBezier(points, p0, p1, p2, p3, curveUnitResolution);
                            current = p3;
                            hasCurrent = true;
                            any = true;
                            lastCubicControl = p2;
                            hasLastCubicControl = true;
                            hasLastQuadraticControl = false;
                        }
                        if (!any) return false;
                        break;
                    }
                    case 'Q':
                    case 'q':
                    {
                        bool any = false;
                        while (TryReadPoint(tokens, ref i, out Vector2 c) &&
                               TryReadPoint(tokens, ref i, out Vector2 p))
                        {
                            Vector2 p0 = current;
                            Vector2 p1 = cmd == 'q' ? current + c : c;
                            Vector2 p2 = cmd == 'q' ? current + p : p;
                            AppendQuadraticBezier(points, p0, p1, p2, curveUnitResolution);
                            current = p2;
                            hasCurrent = true;
                            any = true;
                            lastQuadraticControl = p1;
                            hasLastQuadraticControl = true;
                            hasLastCubicControl = false;
                        }
                        if (!any) return false;
                        break;
                    }
                    case 'T':
                    case 't':
                    {
                        bool any = false;
                        while (TryReadPoint(tokens, ref i, out Vector2 p))
                        {
                            Vector2 p0 = current;
                            Vector2 p1 = (prevCmd is 'Q' or 'q' or 'T' or 't') && hasLastQuadraticControl
                                ? (current * 2f) - lastQuadraticControl
                                : current;
                            Vector2 p2 = cmd == 't' ? current + p : p;
                            AppendQuadraticBezier(points, p0, p1, p2, curveUnitResolution);
                            current = p2;
                            hasCurrent = true;
                            any = true;
                            lastQuadraticControl = p1;
                            hasLastQuadraticControl = true;
                            hasLastCubicControl = false;
                        }
                        if (!any) return false;
                        break;
                    }
                    case 'Z':
                    case 'z':
                    {
                        current = subpathStart;
                        hasCurrent = true;
                        isClosed = true;
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        break;
                    }
                    default:
                        return false;
                }

                prevCmd = cmd;
            }

            if (points.Count > 1 && NearlyEqual(points[0], points[points.Count - 1]))
            {
                isClosed = true;
                points.RemoveAt(points.Count - 1);
            }

            for (int p = 0; p < points.Count; p++)
                points[p] = new Vector2(points[p].x, -points[p].y);

            return isClosed ? points.Count >= 3 : points.Count >= 2;
        }

        private static void AppendCubicBezier(List<Vector2> points, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float unitResolution)
        {
            int segments = SegmentsFromLengthEstimate(
                EstimateCubicLength(p0, p1, p2, p3),
                unitResolution
            );

            for (int s = 1; s <= segments; s++)
            {
                float t = s / (float)segments;
                float omt = 1f - t;
                Vector2 point = (omt * omt * omt * p0)
                    + (3f * omt * omt * t * p1)
                    + (3f * omt * t * t * p2)
                    + (t * t * t * p3);
                points.Add(point);
            }
        }

        private static void AppendQuadraticBezier(List<Vector2> points, Vector2 p0, Vector2 p1, Vector2 p2, float unitResolution)
        {
            int segments = SegmentsFromLengthEstimate(
                EstimateQuadraticLength(p0, p1, p2),
                unitResolution
            );

            for (int s = 1; s <= segments; s++)
            {
                float t = s / (float)segments;
                float omt = 1f - t;
                Vector2 point = (omt * omt * p0)
                    + (2f * omt * t * p1)
                    + (t * t * p2);
                points.Add(point);
            }
        }

        private static int SegmentsFromLengthEstimate(float estimatedLength, float unitResolution)
        {
            float safeResolution = unitResolution > Mathf.Epsilon ? unitResolution : 0.25f;
            return Mathf.Max(1, Mathf.CeilToInt(estimatedLength / safeResolution));
        }

        private static float EstimateCubicLength(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float chord = Vector2.Distance(p0, p3);
            float controlNet = Vector2.Distance(p0, p1) + Vector2.Distance(p1, p2) + Vector2.Distance(p2, p3);
            return 0.5f * (chord + controlNet);
        }

        private static float EstimateQuadraticLength(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float chord = Vector2.Distance(p0, p2);
            float controlNet = Vector2.Distance(p0, p1) + Vector2.Distance(p1, p2);
            return 0.5f * (chord + controlNet);
        }

        private static bool IsCommandToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length != 1) return false;
            return char.ToUpperInvariant(token[0]) switch
            {
                'M' or 'L' or 'H' or 'V' or 'C' or 'S' or 'Q' or 'T' or 'Z' => true,
                _ => false
            };
        }

        private static bool TryReadNumber(MatchCollection tokens, ref int index, out float value)
        {
            value = 0f;
            if (index >= tokens.Count) return false;
            string t = tokens[index].Value;
            if (IsCommandToken(t)) return false;
            if (!float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return false;
            index++;
            return true;
        }

        private static bool TryReadPoint(MatchCollection tokens, ref int index, out Vector2 point)
        {
            point = Vector2.zero;
            if (!TryReadNumber(tokens, ref index, out float x)) return false;
            if (!TryReadNumber(tokens, ref index, out float y)) return false;
            point = new Vector2(x, y);
            return true;
        }

        private static bool NearlyEqual(Vector2 a, Vector2 b, float epsilon = 0.0001f)
        {
            return Mathf.Abs(a.x - b.x) <= epsilon && Mathf.Abs(a.y - b.y) <= epsilon;
        }
    }
}
