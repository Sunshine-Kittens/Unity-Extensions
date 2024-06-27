using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public class ObjectTypeMap<T>
    {
        public T[] Array { get; private set; } = null;
        public Dictionary<Type, T> Dictionary { get; private set; } = null;

        private ObjectTypeMap() { }

        public ObjectTypeMap(ICollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            Array = new T[collection.Count];
            collection.CopyTo(Array, 0);
            Dictionary = new Dictionary<Type, T>(Array.Length);

            for (int i = 0; i < Array.Length; i++)
            {
                Type type = Array[i].GetType();
                if (!Dictionary.ContainsKey(type))
                {
                    Dictionary.Add(type, Array[i]);
                }
                else
                {
                    throw new InvalidOperationException("Multiple instances of the same array element have been found, " +
                        "please ensure all instances are of a unique type.");
                }
            }
        }
    }
}