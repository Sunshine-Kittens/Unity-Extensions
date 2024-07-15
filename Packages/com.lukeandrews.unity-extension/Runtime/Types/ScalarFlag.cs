using System;

namespace UnityEngine.Extension
{
    public class ScalarFlag : IComparable, IComparable<ScalarFlag>, IComparable<bool>, IEquatable<ScalarFlag>, IEquatable<bool>
    {
        private static readonly int _false = 0;
        private static readonly int _true = 1;

        private int _backingValue;

        public ScalarFlag(bool value)
        {
            _backingValue = value ? _true : _false;
        }

        public bool AsBool()
        {
            return _backingValue > _false;
        }

        public void Reset(bool defaultValue = false)
        {
            _backingValue = defaultValue ? _true : _false;
        }

        public void Set(bool value)
        {
            if (value)
            {
                _backingValue++;
            }
            else
            {
                _backingValue--;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (obj is ScalarFlag otherFlag)
            {
                return CompareTo(otherFlag);
            }
            else if (obj is bool otherBool)
            {
                return CompareTo(otherBool);
            }
            throw new ArgumentException("Object is not comparible to ScalarFlag");
        }

        public int CompareTo(ScalarFlag other)
        {
            return _backingValue.CompareTo(other._backingValue);
        }

        public int CompareTo(bool other)
        {
            return AsBool().CompareTo(other);
        }

        public override bool Equals(object obj)
        {
            if (obj is ScalarFlag otherFlag)
            {
                return Equals(otherFlag);
            }
            else if (obj is bool otherBool)
            {
                return Equals(otherBool);
            }
            return false;
        }

        public bool Equals(ScalarFlag other)
        {
            return Equals(other.AsBool());
        }

        public bool Equals(bool other)
        {
            return AsBool() == other;
        }

        public override string ToString()
        {
            return AsBool().ToString();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_backingValue);
        }

        public static bool operator ==(ScalarFlag lhs, ScalarFlag rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ScalarFlag lhs, ScalarFlag rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(ScalarFlag lhs, bool rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ScalarFlag lhs, bool rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(bool lhs, ScalarFlag rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(bool lhs, ScalarFlag rhs)
        {
            return !(lhs == rhs);
        }

        public static implicit operator bool(ScalarFlag scalarFlag)
        {
            return scalarFlag.AsBool();
        }

        public static ScalarFlag operator ++(ScalarFlag scalarFlag)
        {
            scalarFlag._backingValue++;
            return scalarFlag;
        }

        public static ScalarFlag operator --(ScalarFlag scalarFlag)
        {
            scalarFlag._backingValue--;
            return scalarFlag;
        }
    }
}