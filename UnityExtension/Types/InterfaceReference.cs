using System;

namespace UnityEngine.Extension
{
    [Serializable]
    public sealed class InterfaceReference<T> : IEquatable<T>, IEquatable<InterfaceReference<T>> where T : class
    {
        [SerializeField] private UnityEngine.Object _unityObject = null;
        private System.Object _systemObject = null;
        
        private Type _interfaceType = typeof(T);
        private Type _objectType = null;

        private object _object { get { return _systemObject != null ? _systemObject : _unityObject; } }

        public T value { get { return _object as T; } }

        public bool hasValue { get { return value != null; } }

        private InterfaceReference() { }

        public InterfaceReference(T interfaceReference)
        {
            if(interfaceReference == null)
            {
                throw new ArgumentNullException(nameof(interfaceReference));
            }

            _interfaceType = typeof(T);            

            if(!_interfaceType.IsInterface)
            {
                throw new InvalidOperationException(string.Format("T {0}, is not an interface.", _interfaceType.Name));
            }

            _objectType = interfaceReference.GetType();
            _systemObject = interfaceReference;
        }

        public InterfaceReference(object objectReference)
        {
            if (objectReference == null)
            {
                throw new ArgumentNullException(nameof(objectReference));
            }

            _interfaceType = typeof(T);            

            if (!_interfaceType.IsInterface)
            {
                throw new InvalidOperationException(string.Format("T {0}, is not an interface.", _interfaceType.Name));
            }

            _objectType = objectReference.GetType();

            if (!_interfaceType.IsAssignableFrom(_objectType))
            {
                throw new InvalidOperationException(string.Format("object {0} does not implement interface T {1}.", _objectType.Name, _interfaceType.Name));
            }

            _systemObject = objectReference;
        }

        public static implicit operator InterfaceReference<T>(T interfaceReference)
        {
            return new InterfaceReference<T>(interfaceReference);
        }

        public static implicit operator T(InterfaceReference<T> interfaceReference)
        {
            return interfaceReference.value;
        }

        public static bool operator ==(InterfaceReference<T> lhs, InterfaceReference<T> rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(InterfaceReference<T> lhs, InterfaceReference<T> rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(InterfaceReference<T> lhs, T rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(InterfaceReference<T> lhs, T rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(T lhs, InterfaceReference<T> rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(T lhs, InterfaceReference<T> rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(T other)
        {
            return _object.Equals(other);
        }

        public bool Equals(InterfaceReference<T> other)
        {
            return _object.Equals(other._object);
        }

        public override bool Equals(object obj)
        {
            if (obj is InterfaceReference<T> otherInterfaceReference)
            {
                return Equals(otherInterfaceReference);
            }
            else if (obj is T otherInterface)
            {
                return Equals(otherInterface);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_object);
        }

        public T GetValueOrDefault(T defaultValue)
        {
            T ret = value;
            return ret != null ? ret : defaultValue; 
        }
    }
}
