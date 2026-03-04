using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;

namespace Collider2DTools
{
    /// <summary>
    /// Walks an SVG document and creates 2D colliders for supported SVG shape elements.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Set higher priority to dynamically add colliders prior to any rendering
    public class SvgCollider2D : MonoBehaviour
    {
        [Tooltip("Optional SVG asset to parse automatically on Awake.")]
        [SerializeField] private TextAsset _staticSvg;
        [Tooltip("Global XY scale applied to parsed SVG geometry before collider creation.")]
        [SerializeField] private float _scale = 1f;
        [Tooltip("Approximate curve sampling interval in world units when tessellating SVG bezier path segments.")]
        [SerializeField] private float _curveUnitResolution = 0.5f;

        protected virtual void Awake()
        {
            if (_staticSvg != null)
                Walk(_staticSvg.text);
        }

        /// <summary>
        /// Returns the GameObject that should receive the collider for the current shape.
        /// Override to route colliders to custom targets.
        /// </summary>
        /// <param name="shape">The parsed and baked SVG shape being converted to a collider.</param>
        /// <param name="tags">Collected tag tokens from SVG id/class attributes in the current traversal path.</param>
        /// <param name="attributes">Read-only map of attributes from the source SVG element.</param>
        /// <returns>The target GameObject to receive the collider, or <c>null</c> to skip collider creation.</returns>
        protected virtual GameObject GetColliderTarget(SvgShapeInfo shape, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> attributes) => gameObject;

        /// <summary>
        /// Called after a collider is created and configured.
        /// Override to apply additional setup (layer, physics material, metadata, etc.).
        /// </summary>
        /// <param name="collider">The newly created collider component.</param>
        /// <param name="tags">Collected tag tokens from SVG id/class attributes in the current traversal path.</param>
        /// <param name="attributes">Read-only map of attributes from the source SVG element.</param>
        protected virtual void OnColliderCreated(Collider2D collider, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> attributes) { }

        /// <summary>
        /// Parses SVG XML text and creates colliders for supported shapes.
        /// </summary>
        /// <param name="svgXml">Raw SVG XML text.</param>
        protected void Walk(string svgXml)
        {
            var doc = new XmlDocument { XmlResolver = null }; // avoid external entity resolution
            doc.LoadXml(svgXml);

            XmlElement root = doc.DocumentElement;
            if (root == null) return;

            // SVG uses namespaces; read LocalName not Name.
            var tags = new List<string>();
            WalkNode(root, tags, Matrix3x3.Scale(new[] { _scale, _scale }));
        }

        /// <summary>
        /// Parses an SVG TextAsset and creates colliders for supported shapes.
        /// </summary>
        /// <param name="svgText">SVG asset containing XML text.</param>
        protected void Walk(TextAsset svgText)
        {
            if (svgText == null) return;
            Walk(svgText.text);
        }

        /// <summary>
        /// Parses an SVG stream and creates colliders for supported shapes.
        /// </summary>
        /// <param name="svgStream">Readable stream containing SVG XML text.</param>
        protected void Walk(Stream svgStream)
        {
            using var reader = new StreamReader(svgStream);
            Walk(reader.ReadToEnd());
        }

        private void WalkNode(XmlElement el, List<string> tags, Matrix3x3 accumulatedTransform)
        {
            int pushed = PushTags(el.GetAttribute("id"), tags, true);
            pushed += PushTags(el.GetAttribute("class"), tags);

            Matrix3x3 nextTransform = accumulatedTransform * SvgTransformParser.Parse(el.GetAttribute("transform"));

            if (!TryCreateCollider(el, tags, nextTransform))
            {
                for (int i = 0; i < el.ChildNodes.Count; i++)
                {
                    if (el.ChildNodes[i] is XmlElement child)
                        WalkNode(child, tags, nextTransform);
                }
            }

            if (pushed > 0)
                tags.RemoveRange(tags.Count - pushed, pushed);
        }

        private bool TryCreateCollider(XmlElement el, List<string> tags, Matrix3x3 accumulatedTransform)
        {
            SvgShapeInfo shape = SvgShapeParser.Parse(el, _curveUnitResolution);
            if (shape == null) return false;
            shape.Bake(accumulatedTransform);

            IReadOnlyDictionary<string, string> attributes = GetAttributes(el);
            GameObject target = GetColliderTarget(shape, tags, attributes);
            if (target == null) return true;

            GameObject colliderTarget = target;
            if (shape.HasRotation)
            {
                colliderTarget = new GameObject();
                colliderTarget.transform.SetParent(target.transform, false);
                colliderTarget.transform.localPosition = shape.Center;
                colliderTarget.transform.localRotation = Quaternion.Euler(0f, 0f, shape.RotationDeg);
            }

            Collider2D collider;
            switch (shape)
            {
                case SvgCircleInfo circle:
                {
                    var c = colliderTarget.AddComponent<CircleCollider2D>();
                    c.offset = circle.Center;
                    c.radius = circle.Radius;
                    collider = c;
                    break;
                }
                case SvgRectInfo rect:
                {
                    var c = colliderTarget.AddComponent<BoxCollider2D>();
                    c.offset = shape.HasRotation ? Vector2.zero : rect.Center;
                    c.size = rect.Size;
                    collider = c;
                    break;
                }
                case SvgPolygonInfo polygon:
                {
                    var c = colliderTarget.AddComponent<PolygonCollider2D>();
                    c.points = polygon.Points.ToArray();
                    collider = c;
                    break;
                }
                case SvgPolylineInfo polyline:
                {
                    var c = colliderTarget.AddComponent<EdgeCollider2D>();
                    c.points = polyline.Points.ToArray();
                    collider = c;
                    break;
                }
                default:
                    throw new System.NotSupportedException($"Unsupported SVG shape type: {shape.GetType().Name}");
            }

            OnColliderCreated(collider, tags, attributes);
            return true;
        }

        private static IReadOnlyDictionary<string, string> GetAttributes(XmlElement el)
        {
            var attributes = new Dictionary<string, string>();
            if (el?.Attributes == null) return new ReadOnlyDictionary<string, string>(attributes);

            for (int i = 0; i < el.Attributes.Count; i++)
            {
                XmlAttribute attr = el.Attributes[i];
                if (attr == null) continue;
                attributes[attr.Name] = attr.Value ?? string.Empty;
            }

            return new ReadOnlyDictionary<string, string>(attributes);
        }

        private static int PushTags(string tag, List<string> tags, bool trimSuffix = false)
        {
            if (string.IsNullOrWhiteSpace(tag)) return 0;
            int count = 0;
            foreach (string c in tag.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string t = trimSuffix ? Regex.Replace(c, @"_\d+$", "") : c;
                if (string.IsNullOrWhiteSpace(t)) continue;
                tags.Add(t.ToLowerInvariant());
                count++;
            }
            return count;
        }
    }
}
