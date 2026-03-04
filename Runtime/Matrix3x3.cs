using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Collider2DTools
{
    public struct Matrix3x3 : IEquatable<Matrix3x3>, IFormattable
    {
        public float m00;
        public float m10;
        public const float m20 = 0f;
        public float m01;
        public float m11;
        public const float m21 = 0f;
        public float m02;
        public float m12;
        public const float m22 = 1f;

        private static readonly Matrix3x3 IdentityMatrix = new Matrix3x3(
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0f, 1f)
        );

        public static Matrix3x3 identity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return IdentityMatrix; }
        }

        public readonly bool isIdentity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return this == IdentityMatrix; }
        }

        public readonly Vector2 translation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Vector2(m02, m12); }
        }

        public readonly Vector2 scale
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                float sx = new Vector2(m00, m10).magnitude;
                float sy = new Vector2(m01, m11).magnitude;
                if (determinant < 0f) sy = -sy;
                return new Vector2(sx, sy);
            }
        }

        public readonly float rotation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Mathf.Atan2(determinant < 0f ? -m10 : m10, m00) * Mathf.Rad2Deg; }
        }

        public readonly float determinant
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (m00 * m11) - (m01 * m10); }
        }

        public Matrix3x3(Vector3 column0, Vector3 column1, Vector3 column2)
        {
            m00 = column0.x;
            m01 = column1.x;
            m02 = column2.x;
            m10 = column0.y;
            m11 = column1.y;
            m12 = column2.y;
        }

        public static Matrix3x3 Translate(float[] values)
        {
            if (values == null || values.Length < 1) return identity;
            float ty = values.Length > 1 ? values[1] : 0f;
            var m = identity;
            m.m02 = values[0];
            m.m12 = ty;
            return m;
        }

        public static Matrix3x3 Scale(float[] values)
        {
            if (values == null || values.Length < 1) return identity;
            float sy = values.Length > 1 ? values[1] : values[0];
            var m = identity;
            m.m00 = values[0];
            m.m11 = sy;
            return m;
        }

        public static Matrix3x3 Rotate(float[] values)
        {
            if (values == null || values.Length < 1) return identity;
            float angleDeg = values[0];
            float cx = values.Length > 2 ? values[1] : 0f;
            float cy = values.Length > 2 ? values[2] : 0f;
            float radians = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            var m = identity;
            m.m00 = cos;
            m.m01 = -sin;
            m.m02 = cx - cos * cx + sin * cy;
            m.m10 = sin;
            m.m11 = cos;
            m.m12 = cy - sin * cx - cos * cy;
            return m;
        }

        public static Matrix3x3 Matrix(float[] values)
        {
            if (values == null || values.Length < 6) return identity;
            var m = identity;
            m.m00 = values[0];
            m.m01 = values[2];
            m.m02 = values[4];
            m.m10 = values[1];
            m.m11 = values[3];
            m.m12 = values[5];
            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 GetColumn(int index)
        {
            return index switch
            {
                0 => new Vector3 { x = m00, y = m10, z = m20 },
                1 => new Vector3 { x = m01, y = m11, z = m21 },
                2 => new Vector3 { x = m02, y = m12, z = m22 },
                _ => throw new IndexOutOfRangeException("Invalid column index!"),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 GetRow(int index)
        {
            return index switch
            {
                0 => new Vector3 { x = m00, y = m01, z = m02 },
                1 => new Vector3 { x = m10, y = m11, z = m12 },
                2 => new Vector3 { x = m20, y = m21, z = m22 },
                _ => throw new IndexOutOfRangeException("Invalid row index!"),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
            unchecked
            {
                int hash = m00.GetHashCode();
                hash = (hash * 397) ^ m10.GetHashCode();
                hash = (hash * 397) ^ m01.GetHashCode();
                hash = (hash * 397) ^ m11.GetHashCode();
                hash = (hash * 397) ^ m02.GetHashCode();
                hash = (hash * 397) ^ m12.GetHashCode();
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is Matrix3x3 other2)
                return Equals(in other2);

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Matrix3x3 other)
        {
            return GetRow(0).Equals(other.GetRow(0))
                   && GetRow(1).Equals(other.GetRow(1))
                   && GetRow(2).Equals(other.GetRow(2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(in Matrix3x3 other)
        {
            return GetRow(0).Equals(other.GetRow(0))
                   && GetRow(1).Equals(other.GetRow(1))
                   && GetRow(2).Equals(other.GetRow(2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Matrix3x3 lhs, Matrix3x3 rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Matrix3x3 lhs, Matrix3x3 rhs)
        {
            return !lhs.Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator *(Matrix3x3 lhs, Matrix3x3 rhs)
        {
            return new Matrix3x3
            {
                m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10,
                m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11,
                m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02,
                m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10,
                m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11,
                m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Matrix3x3 lhs, Vector3 vector)
        {
            return new Vector3
            {
                x = lhs.m00 * vector.x + lhs.m01 * vector.y + lhs.m02 * vector.z,
                y = lhs.m10 * vector.x + lhs.m11 * vector.y + lhs.m12 * vector.z,
                z = vector.z
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly string ToString()
        {
            return ToString(null, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(string format)
        {
            return ToString(format, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F5";

            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;

            return $"{m00.ToString(format, formatProvider)}\t{m01.ToString(format, formatProvider)}\t{m02.ToString(format, formatProvider)}\n{m10.ToString(format, formatProvider)}\t{m11.ToString(format, formatProvider)}\t{m12.ToString(format, formatProvider)}\n{m20.ToString(format, formatProvider)}\t{m21.ToString(format, formatProvider)}\t{m22.ToString(format, formatProvider)}\n";
        }
    }
}
