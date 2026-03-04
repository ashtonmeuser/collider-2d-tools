using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Collider2DTools.Tests
{
    public class SvgTransformParserTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void Parse_WithEmptyInput_ReturnsIdentity()
        {
            Matrix3x3 parsed = SvgTransformParser.Parse(string.Empty);

            Assert.That(parsed, Is.EqualTo(Matrix3x3.identity));
        }

        [Test]
        public void Parse_Translate_ConvertsSvgYAxisToUnity()
        {
            Matrix3x3 parsed = SvgTransformParser.Parse("translate(10,20)");

            Assert.That(parsed.translation.x, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(parsed.translation.y, Is.EqualTo(-20f).Within(Tolerance));
        }

        [Test]
        public void Parse_AppliesTransformsLeftToRight()
        {
            Matrix3x3 parsed = SvgTransformParser.Parse("translate(3,4) scale(2,3)");
            Matrix3x3 basis = Matrix3x3.Scale(new[] { 1f, -1f });
            Matrix3x3 expectedSvgSpace = Matrix3x3.Scale(new[] { 2f, 3f }) * Matrix3x3.Translate(new[] { 3f, 4f });
            Matrix3x3 expected = basis * expectedSvgSpace * basis;

            Assert.That(parsed.m00, Is.EqualTo(expected.m00).Within(Tolerance));
            Assert.That(parsed.m01, Is.EqualTo(expected.m01).Within(Tolerance));
            Assert.That(parsed.m02, Is.EqualTo(expected.m02).Within(Tolerance));
            Assert.That(parsed.m10, Is.EqualTo(expected.m10).Within(Tolerance));
            Assert.That(parsed.m11, Is.EqualTo(expected.m11).Within(Tolerance));
            Assert.That(parsed.m12, Is.EqualTo(expected.m12).Within(Tolerance));
        }

        [Test]
        public void Parse_Matrix_SupportsScientificNotation()
        {
            Matrix3x3 parsed = SvgTransformParser.Parse("matrix(1e0, 0, 0, 1, 1.5e1, -2.5e1)");

            Assert.That(parsed.translation.x, Is.EqualTo(15f).Within(Tolerance));
            Assert.That(parsed.translation.y, Is.EqualTo(25f).Within(Tolerance));
        }

        [Test]
        public void Parse_UnsupportedTransform_IgnoresUnknownAndContinues()
        {
            LogAssert.Expect(LogType.Warning, new Regex("unsupported transform 'skewx'"));

            Matrix3x3 parsed = SvgTransformParser.Parse("skewX(20) translate(2,5)");

            Assert.That(parsed.translation.x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(parsed.translation.y, Is.EqualTo(-5f).Within(Tolerance));
        }
    }
}
