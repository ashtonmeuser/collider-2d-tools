using NUnit.Framework;
using UnityEngine;

namespace Collider2DTools.Tests
{
    public class Matrix3x3Tests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void Identity_HasExpectedProperties()
        {
            Matrix3x3 matrix = Matrix3x3.identity;

            Assert.That(matrix.isIdentity, Is.True);
            Assert.That(matrix.translation, Is.EqualTo(Vector2.zero));
            Assert.That(matrix.scale, Is.EqualTo(Vector2.one));
            Assert.That(matrix.rotation, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(matrix.determinant, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Translate_WithOneValue_UsesZeroForY()
        {
            Matrix3x3 matrix = Matrix3x3.Translate(new[] { 3f });

            Assert.That(matrix.translation.x, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(matrix.translation.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(matrix.scale, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void Scale_WithSingleValue_UsesUniformScale()
        {
            Matrix3x3 matrix = Matrix3x3.Scale(new[] { 2f });

            Assert.That(matrix.m00, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(matrix.m11, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(matrix.scale, Is.EqualTo(new Vector2(2f, 2f)));
            Assert.That(matrix.determinant, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void Rotate_WithPivot_RotatesPointAroundPivot()
        {
            Matrix3x3 matrix = Matrix3x3.Rotate(new[] { 90f, 1f, 2f });
            Vector3 point = new Vector3(2f, 2f, 1f);

            Vector3 result = matrix * point;

            Assert.That(result.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(result.y, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(result.z, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Matrix_FromSixValues_MapsInSvgOrder()
        {
            Matrix3x3 matrix = Matrix3x3.Matrix(new[] { 1f, 2f, 3f, 4f, 5f, 6f });

            Assert.That(matrix.m00, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(matrix.m10, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(matrix.m01, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(matrix.m11, Is.EqualTo(4f).Within(Tolerance));
            Assert.That(matrix.m02, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(matrix.m12, Is.EqualTo(6f).Within(Tolerance));
        }

        [Test]
        public void Multiply_ComposesTransformsInOrder()
        {
            Matrix3x3 translate = Matrix3x3.Translate(new[] { 2f, 3f });
            Matrix3x3 scale = Matrix3x3.Scale(new[] { 2f, 4f });
            Matrix3x3 combined = translate * scale;
            Vector3 point = new Vector3(1f, 1f, 1f);

            Vector3 result = combined * point;

            Assert.That(result.x, Is.EqualTo(4f).Within(Tolerance));
            Assert.That(result.y, Is.EqualTo(7f).Within(Tolerance));
            Assert.That(result.z, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void GetColumn_WithInvalidIndex_Throws()
        {
            Assert.Throws<System.IndexOutOfRangeException>(() => Matrix3x3.identity.GetColumn(3));
        }

        [Test]
        public void GetRow_WithInvalidIndex_Throws()
        {
            Assert.Throws<System.IndexOutOfRangeException>(() => Matrix3x3.identity.GetRow(-1));
        }
    }
}
