using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEngine.Extension.SerializedDictionary
{
    public sealed class SerializedDictionaryDebugView<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _dictionary;

        public SerializedDictionaryDebugView(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Items
        {
            get
            {
                KeyValuePair<TKey, TValue>[] items = new KeyValuePair<TKey, TValue>[_dictionary.Count];
                _dictionary.CopyTo(items, 0);
                return items;
            }
        }
    }

    /// <summary>
    /// Unity can't serialize Dictionary so here's a custom wrapper that does. Note that you have to
    /// extend it before it can be serialized as Unity won't serialize generic-based types either.
    /// </summary>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="TValue">The value</typeparam>
    /// <example>
    /// public sealed class MyDictionary : SerializedDictionary&lt;KeyType, ValueType&gt; {}
    /// </example>
    [Serializable]
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(SerializedDictionaryDebugView<,>))]
    public class SerializedDictionary<TKey, TValue> : SerializedDictionary<TKey, TValue, TKey, TValue>
    {
        /// <summary>
        /// Conversion to serialize a key
        /// </summary>
        /// <param name="key">The key to serialize</param>
        /// <returns>The Key that has been serialized</returns>
        public override TKey SerializeKey(TKey key) => key;

        /// <summary>
        /// Conversion to serialize a value
        /// </summary>
        /// <param name="val">The value</param>
        /// <returns>The value</returns>
        public override TValue SerializeValue(TValue val) => val;

        /// <summary>
        /// Conversion to serialize a key
        /// </summary>
        /// <param name="key">The key to serialize</param>
        /// <returns>The Key that has been serialized</returns>
        public override TKey DeserializeKey(TKey key) => key;

        /// <summary>
        /// Conversion to serialize a value
        /// </summary>
        /// <param name="val">The value</param>
        /// <returns>The value</returns>
        public override TValue DeserializeValue(TValue val) => val;
    }

    /// <summary>
    /// Dictionary that can serialize keys and values as other types
    /// </summary>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="TValue">The value type</typeparam>
    /// <typeparam name="TSerializedKeyType">The type which the key will be serialized for</typeparam>
    /// <typeparam name="TSerializedValueType">The type which the value will be serialized for</typeparam>
    [Serializable]
    public abstract class SerializedDictionary<TKey, TValue, TSerializedKeyType, TSerializedValueType> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TSerializedKeyType> _keys = new List<TSerializedKeyType>();
        [SerializeField] private List<TSerializedValueType> _values = new List<TSerializedValueType>();

        /// <summary>
        /// From <see cref="TKey"/> to <see cref="TSerializedKeyType"/>
        /// </summary>
        /// <param name="key">They key in <see cref="TKey"/></param>
        /// <returns>The key in <see cref="TSerializedKeyType"/></returns>
        public abstract TSerializedKeyType SerializeKey(TKey key);

        /// <summary>
        /// From <see cref="TValue"/> to <see cref="TSerializedValueType"/>
        /// </summary>
        /// <param name="value">The value in <see cref="TValue"/></param>
        /// <returns>The value in <see cref="TSerializedValueType"/></returns>
        public abstract TSerializedValueType SerializeValue(TValue value);

        /// <summary>
        /// From <see cref="TSerializedKeyType"/> to <see cref="TKey"/>
        /// </summary>
        /// <param name="serializedKey">They key in <see cref="TSerializedKeyType"/></param>
        /// <returns>The key in <see cref="TKey"/></returns>
        public abstract TKey DeserializeKey(TSerializedKeyType serializedKey);

        /// <summary>
        /// From <see cref="TSerializedValueType"/> to <see cref="TValue"/>
        /// </summary>
        /// <param name="serializedValue">The value in <see cref="TSerializedValueType"/></param>
        /// <returns>The value in <see cref="TValue"/></returns>
        public abstract TValue DeserializeValue(TSerializedValueType serializedValue);

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();

            foreach (var kvp in this)
            {
                _keys.Add(SerializeKey(kvp.Key));
                _values.Add(SerializeValue(kvp.Value));
            }
        }

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i < _keys.Count; i++)
                Add(DeserializeKey(_keys[i]), DeserializeValue(_values[i]));
        }
    }
}