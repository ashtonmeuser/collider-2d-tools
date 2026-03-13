using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;

using Attributes = System.Collections.Generic.IReadOnlyDictionary<string, string>;

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

        /// <summary>
        /// Optional regex used to accept and normalize tag tokens collected from SVG <c>id</c> and <c>class</c> attributes.
        /// Tokens that do not match this pattern are ignored. If the match contains capture groups, the first capture
        /// group is used as the stored tag value. Return <c>null</c> to accept normalized tokens as-is.
        /// </summary>
        protected virtual Regex TagPattern { get; set; }

        protected virtual void Awake()
        {
            if (_staticSvg != null && enabled)
                Walk(_staticSvg.text);
        }

        /// <summary>
        /// Determines whether the current SVG element subtree should be visited.
        /// Called after tags and attributes have been collected for the current element, and before collider
        /// creation or traversal into child elements. Return <c>false</c> to skip the current element entirely,
        /// including any supported shape on this node and all of its descendants.
        /// </summary>
        /// <param name="tags">Collected tag tokens from SVG id/class attributes in the current traversal path.</param>
        /// <param name="attributes">Read-only map of attributes from the current SVG element.</param>
        /// <param name="groupId">The nearest parent <c>g</c> element ID for the current element, or <c>null</c> if none.</param>
        /// <returns><c>true</c> to continue processing this element subtree; otherwise, <c>false</c>.</returns>
        protected virtual bool ShouldDescend(IReadOnlyList<string> tags, Attributes attributes, string groupId) => true;

        /// <summary>
        /// Returns the GameObject that should receive the collider for the current shape.
        /// Override to route colliders to custom targets.
        /// </summary>
        /// <param name="shape">The parsed and baked SVG shape being converted to a collider.</param>
        /// <param name="tags">Collected tag tokens from SVG id/class attributes in the current traversal path.</param>
        /// <param name="attributes">Read-only map of attributes from the source SVG element.</param>
        /// <param name="groupId">The nearest parent <c>g</c> element ID for the shape element, or <c>null</c> if none.</param>
        /// <returns>The target GameObject to receive the collider, or <c>null</c> to skip collider creation.</returns>
        protected virtual GameObject GetColliderTarget(SvgShapeInfo shape, IReadOnlyList<string> tags, Attributes attributes, string groupId)
            => GetColliderTarget(shape, tags, attributes);

        /// <summary>
        /// Returns the GameObject that should receive the collider for the current shape.
        /// Override to route colliders to custom targets.
        /// </summary>
        /// <param name="shape">The parsed and baked SVG shape being converted to a collider.</param>
        /// <param name="tags">Collected tag tokens from SVG id/class attributes in the current traversal path.</param>
        /// <param name="attributes">Read-only map of attributes from the source SVG element.</param>
        /// <returns>The target GameObject to receive the collider, or <c>null</c> to skip collider creation.</returns>
        protected virtual GameObject GetColliderTarget(SvgShapeInfo shape, IReadOnlyList<string> tags, Attributes attributes) => gameObject;

        /// <summary>
        /// Called after a collider is created and configured.
        /// Override to apply additional setup (layer, physics material, metadata, etc.).
        /// </summary>
        /// <param name="collider">The newly created collider component.</param>
        /// <param name="tags">Collected tag tokens from SVG id/class attributes in the current traversal path.</param>
        /// <param name="attributes">Read-only map of attributes from the source SVG element.</param>
        /// <param name="groupId">The nearest parent <c>g</c> element ID for the shape element, or <c>null</c> if none.</param>
        protected virtual void OnColliderCreated(Collider2D collider, IReadOnlyList<string> tags, Attributes attributes, string groupId)
            => OnColliderCreated(collider, tags, attributes);

        /// <summary>
        /// Called after a collider is created and configured.
        /// Override to apply additional setup (layer, physics material, metadata, etc.).
        /// </summary>
        /// <param name="collider">The newly created collider component.</param>
        /// <param name="tags">Collected tag tokens from SVG id/class attributes in the current traversal path.</param>
        /// <param name="attributes">Read-only map of attributes from the source SVG element.</param>
        protected virtual void OnColliderCreated(Collider2D collider, IReadOnlyList<string> tags, Attributes attributes) { }

        /// <summary>
        /// Parses SVG XML text and creates colliders for supported shapes.
        /// </summary>
        /// <param name="svgXml">Raw SVG XML text.</param>
        public void Walk(string svgXml)
        {
            var doc = new XmlDocument { XmlResolver = null }; // Avoid external entity resolution
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
        public void Walk(TextAsset svgText)
        {
            if (svgText == null) return;
            Walk(svgText.text);
        }

        /// <summary>
        /// Parses an SVG stream and creates colliders for supported shapes.
        /// </summary>
        /// <param name="svgStream">Readable stream containing SVG XML text.</param>
        public void Walk(Stream svgStream)
        {
            using var reader = new StreamReader(svgStream);
            Walk(reader.ReadToEnd());
        }

        private void WalkNode(XmlElement el, List<string> tags, Matrix3x3 accumulatedTransform, string groupId = null)
        {
            int pushed = PushTags(el.GetAttribute("id"), tags, true);
            pushed += PushTags(el.GetAttribute("class"), tags);

            string nextGroupId = groupId;
            if (el.LocalName == "g") nextGroupId = ParseGroupId(el.GetAttribute("id"));

            var attributes = GetAttributes(el);
            if (!ShouldDescend(tags, attributes, nextGroupId))
            {
                if (pushed > 0) tags.RemoveRange(tags.Count - pushed, pushed);
                return;
            }

            Matrix3x3 nextTransform = accumulatedTransform * SvgTransformParser.Parse(el.GetAttribute("transform"));

            if (!TryCreateCollider(el, tags, attributes, nextTransform, nextGroupId))
            {
                for (int i = 0; i < el.ChildNodes.Count; i++)
                {
                    if (el.ChildNodes[i] is XmlElement child)
                        WalkNode(child, tags, nextTransform, nextGroupId);
                }
            }

            if (pushed > 0) tags.RemoveRange(tags.Count - pushed, pushed);
        }

        private bool TryCreateCollider(XmlElement el, List<string> tags, Attributes attributes, Matrix3x3 accumulatedTransform, string groupId)
        {
            SvgShapeInfo shape = SvgShapeParser.Parse(el, _curveUnitResolution);
            if (shape == null) return false;
            shape.Bake(accumulatedTransform);

            GameObject target = GetColliderTarget(shape, tags, attributes, groupId);
            if (target == null) return true;

            GameObject colliderTarget = target;
            if (shape.HasRotation)
            {
                colliderTarget = new GameObject { layer = target.layer };
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
                    c.points = polygon.Points;
                    collider = c;
                    break;
                }
                case SvgPolylineInfo polyline:
                {
                    var c = colliderTarget.AddComponent<EdgeCollider2D>();
                    c.points = polyline.Points;
                    collider = c;
                    break;
                }
                default:
                    throw new System.NotSupportedException($"Unsupported SVG shape type: {shape.GetType().Name}");
            }

            OnColliderCreated(collider, tags, attributes, groupId);
            return true;
        }

        private static string ParseGroupId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return id.Trim();
        }

        private static Attributes GetAttributes(XmlElement el)
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

        private int PushTags(string tag, List<string> tags, bool trimSuffix = false)
        {
            if (string.IsNullOrWhiteSpace(tag)) return 0;
            int count = 0;
            foreach (string c in tag.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string normalizedTag = trimSuffix ? Regex.Replace(c, @"_\d+$", "") : c;
                normalizedTag = normalizedTag.ToLowerInvariant();

                if (TagPattern != null)
                {
                    Match match = TagPattern.Match(normalizedTag);
                    if (!match.Success) continue; // Bail if the pattern does not match
                    if (match.Groups.Count >= 2) normalizedTag = match.Groups[1].Value; // If a group is found, use it as the tag
                }

                if (string.IsNullOrWhiteSpace(normalizedTag)) continue;

                tags.Add(normalizedTag);
                count++;
            }
            return count;
        }
    }
}
