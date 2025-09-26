using UnityEngine.Extension.SerializedDictionary;

namespace UnityEditor.Extension.SerializedDictionary
{
    [CustomPropertyDrawer(typeof(StringDictionary))] public sealed class StringDictionaryPropertyDrawer : 
        StringDictionaryPropertyDrawerBase { }
    [CustomPropertyDrawer(typeof(UnityObjectStringDictionary))] public sealed class UnityObjectStringDictionaryPropertyDrawer : 
        UnityObjectDictionaryPropertyDrawerBase { }
    
    [CustomPropertyDrawer(typeof(StringIntDictionary))] public sealed class StringIntDictionaryPropertyDrawer : 
        StringDictionaryPropertyDrawerBase { }
    [CustomPropertyDrawer(typeof(UnityObjectIntDictionary))] public sealed class UnityObjectIntDictionaryPropertyDrawer : 
        UnityObjectDictionaryPropertyDrawerBase { }
    
    [CustomPropertyDrawer(typeof(StringFloatDictionary))] public sealed class StringFloatDictionaryPropertyDrawer : 
        StringDictionaryPropertyDrawerBase { }
    [CustomPropertyDrawer(typeof(UnityObjectFloatDictionary))] public sealed class UnityObjectFloatDictionaryPropertyDrawer : 
        UnityObjectDictionaryPropertyDrawerBase { }
    
    [CustomPropertyDrawer(typeof(StringVector2IntDictionary))] public sealed class StringVector2IntDictionaryPropertyDrawer : 
        StringDictionaryPropertyDrawerBase { }
    [CustomPropertyDrawer(typeof(UnityObjectVector2IntDictionary))] public sealed class UnityObjectVector2IntDictionaryPropertyDrawer :
        UnityObjectDictionaryPropertyDrawerBase { }
    
    [CustomPropertyDrawer(typeof(StringVector2Dictionary))] public sealed class StringVector2DictionaryPropertyDrawer : 
        StringDictionaryPropertyDrawerBase { }
    [CustomPropertyDrawer(typeof(UnityObjectVector2Dictionary))] public sealed class UnityObjectVector2DictionaryPropertyDrawer : 
        UnityObjectDictionaryPropertyDrawerBase { }
    
    [CustomPropertyDrawer(typeof(StringVector3IntDictionary))] public sealed class StringVector3IntDictionaryPropertyDrawer : 
        StringDictionaryPropertyDrawerBase { }
    [CustomPropertyDrawer(typeof(UnityObjectVector3IntDictionary))] public sealed class UnityObjectVector3IntDictionaryPropertyDrawer : 
        UnityObjectDictionaryPropertyDrawerBase { }
    
    [CustomPropertyDrawer(typeof(StringVector3Dictionary))] public sealed class StringVector3DictionaryPropertyDrawer :
        StringDictionaryPropertyDrawerBase { }
    [CustomPropertyDrawer(typeof(UnityObjectVector3Dictionary))] public sealed class UnityObjectVector3DictionaryPropertyDrawer : 
        UnityObjectDictionaryPropertyDrawerBase { }
    
    [CustomPropertyDrawer(typeof(StringVector4Dictionary))] public sealed class StringVector4DictionaryPropertyDrawer : 
        StringDictionaryPropertyDrawerBase { }
    [CustomPropertyDrawer(typeof(UnityObjectVector4Dictionary))] public sealed class UnityObjectVector4DictionaryPropertyDrawer : 
        UnityObjectDictionaryPropertyDrawerBase { }
}
