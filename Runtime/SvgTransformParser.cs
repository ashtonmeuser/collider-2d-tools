using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Collider2DTools
{
    internal static class SvgTransformParser
    {
        private static readonly Regex TransformRegex = new Regex(@"(\w+)\s*\(([^)]*)\)", RegexOptions.Compiled);
        private static readonly Regex NumberRegex = new Regex(@"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled);
        private static readonly Matrix3x3 SvgToUnityBasis = Matrix3x3.Scale(new[] { 1f, -1f });

        public static Matrix3x3 Parse(string transformAttr)
        {
            var result = Matrix3x3.identity;
            if (string.IsNullOrWhiteSpace(transformAttr)) return result;

            var ci = CultureInfo.InvariantCulture;
            MatchCollection matches = TransformRegex.Matches(transformAttr);

            foreach (Match m in matches)
            {
                if (!m.Success || m.Groups.Count < 3) continue;
                string name = m.Groups[1].Value.Trim().ToLowerInvariant();
                string args = m.Groups[2].Value;
                if (!TryParseFloats(args, ci, out float[] values))
                {
                    Debug.LogWarning($"SvgTransform: failed to parse numeric arguments for '{name}({args})' in '{transformAttr}'.");
                    continue;
                }

                Matrix3x3 t;
                switch (name)
                {
                    case "translate":
                        t = Matrix3x3.Translate(values);
                        break;
                    case "scale":
                        t = Matrix3x3.Scale(values);
                        break;
                    case "rotate":
                        t = Matrix3x3.Rotate(values);
                        break;
                    case "matrix":
                        t = Matrix3x3.Matrix(values);
                        break;
                    default:
                        Debug.LogWarning($"SvgTransform: unsupported transform '{name}' in '{transformAttr}'.");
                        continue;
                }
                result = t * result; // SVG applies left-to-right: first transform is leftmost in multiplication
            }

            return SvgToUnityBasis * result * SvgToUnityBasis;
        }

        private static bool TryParseFloats(string s, IFormatProvider ci, out float[] result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(s)) return false;

            MatchCollection matches = NumberRegex.Matches(s);
            if (matches.Count == 0) return false;

            var list = new List<float>();
            foreach (Match m in matches)
            {
                if (!float.TryParse(m.Value, NumberStyles.Float, ci, out float v)) return false;
                list.Add(v);
            }

            result = list.ToArray();
            return true;
        }
    }
}
