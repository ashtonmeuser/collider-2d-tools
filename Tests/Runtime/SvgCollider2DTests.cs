using System.Collections.Generic;
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

        private sealed class TestableSvgCollider2D : SvgCollider2D
        {
            public List<string> TargetParentGroupIds { get; } = new List<string>();
            public List<string> CreatedParentGroupIds { get; } = new List<string>();

            public void Parse(string svg)
            {
                Walk(svg);
            }

            protected override GameObject GetColliderTarget(SvgShapeInfo shape, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> attributes, string parentGroupId)
            {
                TargetParentGroupIds.Add(parentGroupId);
                return gameObject;
            }

            protected override void OnColliderCreated(Collider2D collider, IReadOnlyList<string> tags, IReadOnlyDictionary<string, string> attributes, string parentGroupId)
            {
                CreatedParentGroupIds.Add(parentGroupId);
            }
        }
    }
}
