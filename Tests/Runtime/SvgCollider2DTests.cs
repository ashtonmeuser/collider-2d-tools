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
            Assert.That(collider.Documents[0].Bounds.width, Is.EqualTo(12f));
            Assert.That(collider.Documents[0].Bounds.height, Is.EqualTo(8f));
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

        [Test]
        public void Walk_SyncsPhysicsAfterParentMovesDuringParsing()
        {
            var level = new GameObject("Level");
            var staticTarget = new GameObject("Static");
            staticTarget.transform.SetParent(level.transform, false);
            var collider = level.AddComponent<MovingParentSvgCollider2D>();
            collider.StaticTarget = staticTarget;

            try
            {
                const string svg = @"
<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'>
  <rect class='static' x='4' y='4' width='8' height='8' />
  <rect class='camera' x='0' y='0' width='1' height='1' />
</svg>";

                collider.Parse(svg);

                var box = staticTarget.GetComponent<BoxCollider2D>();
                Assert.That(box, Is.Not.Null);
                AssertVector2(box.bounds.center, new Vector2(0f, 4f));
                AssertVector2(box.bounds.size, new Vector2(8f, 8f));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void VisualizerWalk_SyncsPhysicsBeforeCreatingMesh()
        {
            var level = new GameObject("Level");
            var staticTarget = new GameObject("Static");
            staticTarget.transform.SetParent(level.transform, false);
            var box = staticTarget.AddComponent<BoxCollider2D>();
            box.offset = new Vector2(8f, -8f);
            box.size = new Vector2(8f, 8f);
            var visualizer = staticTarget.AddComponent<ColliderVisualizer2D>();

            try
            {
                level.transform.position = new Vector3(-8f, 12f, 0f);

                visualizer.Walk();

                Mesh mesh = staticTarget.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh, Is.Not.Null);
                AssertVector2(mesh.bounds.center, box.offset);
                AssertVector2(mesh.bounds.size, box.size);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
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
                Documents.Add(new DocumentHookCall(document.Bounds, attributes));
            }
        }

        private sealed class MovingParentSvgCollider2D : SvgCollider2D
        {
            public GameObject StaticTarget { get; set; }

            public void Parse(string svg)
            {
                Walk(svg);
            }

            protected override GameObject GetColliderTarget(SvgShapeInfo shape, IReadOnlyCollection<string> tags, IReadOnlyDictionary<string, string> attributes, string parentGroupId)
            {
                if (tags.Contains("camera"))
                {
                    transform.localPosition = new Vector3(-8f, 12f, 0f);
                    return null;
                }

                if (tags.Contains("static"))
                    return StaticTarget;

                return null;
            }
        }

        private static void AssertVector2(Vector3 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.02f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.02f));
        }

        private readonly struct DocumentHookCall
        {
            public Rect Bounds { get; }
            public IReadOnlyDictionary<string, string> Attributes { get; }

            public DocumentHookCall(Rect bounds, IReadOnlyDictionary<string, string> attributes)
            {
                Bounds = bounds;
                Attributes = attributes;
            }
        }
    }
}
