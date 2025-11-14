using System;

namespace UnityEngine.Extension.SerializedDictionary
{
    [Serializable] public sealed class StringDictionary : SerializedDictionary<string, string> { }
    [Serializable] public sealed class UnityObjectStringDictionary : SerializedDictionary<Object, string> { }
    
    [Serializable] public sealed class StringIntDictionary : SerializedDictionary<string, int> { }
    [Serializable] public sealed class UnityObjectIntDictionary : SerializedDictionary<Object, int> { }
    
    [Serializable] public sealed class StringFloatDictionary : SerializedDictionary<string, float> { }
    [Serializable] public sealed class UnityObjectFloatDictionary : SerializedDictionary<Object, float> { }
    
    [Serializable] public sealed class StringVector2IntDictionary : SerializedDictionary<string, Vector2Int> { }
    [Serializable] public sealed class UnityObjectVector2IntDictionary : SerializedDictionary<Object, Vector2Int> { }
    
    [Serializable] public sealed class StringVector2Dictionary : SerializedDictionary<string, Vector2> { }
    [Serializable] public sealed class UnityObjectVector2Dictionary : SerializedDictionary<Object, Vector2> { }
    
    [Serializable] public sealed class StringVector3IntDictionary : SerializedDictionary<string, Vector3Int> { }
    [Serializable] public sealed class UnityObjectVector3IntDictionary : SerializedDictionary<Object, Vector3Int> { }
    
    [Serializable] public sealed class StringVector3Dictionary : SerializedDictionary<string, Vector3> { }
    [Serializable] public sealed class UnityObjectVector3Dictionary : SerializedDictionary<Object, Vector3> { }
    
    [Serializable] public sealed class StringVector4Dictionary : SerializedDictionary<string, Vector4> { }
    [Serializable] public sealed class UnityObjectVector4Dictionary : SerializedDictionary<Object, Vector4> { }
}
