using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public class ObjectTypeMap<T>
    {
        public T[] array { get; private set; } = null;
        public Dictionary<Type, T> dictionary { get; private set; } = null;

        private ObjectTypeMap() { }

        public ObjectTypeMap(ICollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            array = new T[collection.Count];
            collection.CopyTo(array, 0);
            dictionary = new Dictionary<Type, T>(array.Length);

            for (int i = 0; i < array.Length; i++)
            {
                Type type = array[i].GetType();
                if (!dictionary.ContainsKey(type))
                {
                    dictionary.Add(type, array[i]);
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