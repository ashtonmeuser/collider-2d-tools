using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Collider2DTools.Tests
{
    public class SvgCollider2DTests
    {
        [Test]
        public void Walk_PassesDirectParentGroupIdToHooks()
        {
            var go = new GameObject("SvgCollider2DTests");
            var collider = go.AddComponent<TestableSvgCollider2D>();

            const string svg = @"
<svg xmlns='http://www.w3.org/2000/svg'>
  <g id='outer'>
    <rect x='0' y='0' width='1' height='1' />
    <g>
      <rect x='2' y='0' width='1' height='1' />
    </g>
    <g id='inner'>
      <rect x='4' y='0' width='1' height='1' />
    </g>
  </g>
</svg>";

            collider.Parse(svg);

            Assert.That(collider.TargetParentGroupIds.Count, Is.EqualTo(3));
            Assert.That(collider.TargetParentGroupIds[0], Is.EqualTo("outer"));
            Assert.That(collider.TargetParentGroupIds[1], Is.Null);
            Assert.That(collider.TargetParentGroupIds[2], Is.EqualTo("inner"));

            Assert.That(collider.CreatedParentGroupIds.Count, Is.EqualTo(3));
            Assert.That(collider.CreatedParentGroupIds[0], Is.EqualTo("outer"));
            Assert.That(collider.CreatedParentGroupIds[1], Is.Null);
            Assert.That(collider.CreatedParentGroupIds[2], Is.EqualTo("inner"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Walk_CallsOnDocumentCreatedWithDocumentDimensions()
        {
            var go = new GameObject("SvgCollider2DTests");
            var collider = go.AddComponent<TestableSvgCollider2D>();

            const string svg = @"
<svg xmlns='http://www.w3.org/2000/svg' width='12' height='8'>
  <rect x='0' y='0' width='1' height='1' />
</svg>";

            collider.Parse(svg);

            Assert.That(collider.Documents.Count, Is.EqualTo(1));
            Assert.That(collider.Documents[0].Width, Is.EqualTo(12f));
            Assert.That(collider.Documents[0].Height, Is.EqualTo(8f));
            Assert.That(collider.Documents[0].Attributes["width"], Is.EqualTo("12"));
            Assert.That(collider.Documents[0].Attributes["height"], Is.EqualTo("8"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Walk_ContinuesTraversingChildrenAfterOnDocumentCreated()
        {
            var go = new GameObject("SvgCollider2DTests");
            var collider = go.AddComponent<TestableSvgCollider2D>();

            const string svg = @"
<svg xmlns='http://www.w3.org/2000/svg' width='12' height='8'>
  <rect x='0' y='0' width='1' height='1' />
</svg>";

            collider.Parse(svg);

            Assert.That(collider.Documents.Count, Is.EqualTo(1));
            Assert.That(collider.TargetParentGroupIds.Count, Is.EqualTo(1));
            Assert.That(collider.CreatedParentGroupIds.Count, Is.EqualTo(1));
            Assert.That(go.GetComponents<Collider2D>().Length, Is.EqualTo(1));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Walk_PreservesInheritedTagsWhenChildRepeatsSameTag()
        {
            var go = new GameObject("SvgCollider2DTests");
            var collider = go.AddComponent<TestableSvgCollider2D>();

            const string svg = @"
<svg xmlns='http://www.w3.org/2000/svg'>
  <g class='shared'>
    <rect class='shared first' x='0' y='0' width='1' height='1' />
    <rect class='second' x='2' y='0' width='1' height='1' />
  </g>
</svg>";

            collider.Parse(svg);

            Assert.That(collider.TargetTags.Count, Is.EqualTo(2));
            CollectionAssert.AreEquivalent(new[] { "shared", "first" }, collider.TargetTags[0]);
            CollectionAssert.AreEquivalent(new[] { "shared", "second" }, collider.TargetTags[1]);

            Object.DestroyImmediate(go);
        }

        private sealed class TestableSvgCollider2D : SvgCollider2D
        {
            public List<string> TargetParentGroupIds { get; } = new List<string>();
            public List<string> CreatedParentGroupIds { get; } = new List<string>();
            public List<DocumentHookCall> Documents { get; } = new List<DocumentHookCall>();
            public List<string[]> TargetTags { get; } = new List<string[]>();

            public void Parse(string svg)
            {
                Walk(svg);
            }

            protected override GameObject GetColliderTarget(SvgShapeInfo shape, IReadOnlyCollection<string> tags, IReadOnlyDictionary<string, string> attributes, string parentGroupId)
            {
                TargetParentGroupIds.Add(parentGroupId);
                TargetTags.Add(tags.OrderBy(tag => tag).ToArray());
                return gameObject;
            }

            protected override void OnColliderCreated(Collider2D collider, IReadOnlyCollection<string> tags, IReadOnlyDictionary<string, string> attributes, string parentGroupId)
            {
                CreatedParentGroupIds.Add(parentGroupId);
            }

            protected override void OnDocumentCreated(SvgDocumentInfo document, IReadOnlyCollection<string> tags, IReadOnlyDictionary<string, string> attributes)
            {
                Documents.Add(new DocumentHookCall(document.Width, document.Height, attributes));
            }
        }

        private readonly struct DocumentHookCall
        {
            public float Width { get; }
            public float Height { get; }
            public IReadOnlyDictionary<string, string> Attributes { get; }

            public DocumentHookCall(float width, float height, IReadOnlyDictionary<string, string> attributes)
            {
                Width = width;
                Height = height;
                Attributes = attributes;
            }
        }
    }
}
