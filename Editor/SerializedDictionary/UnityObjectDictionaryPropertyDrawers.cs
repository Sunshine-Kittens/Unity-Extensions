using UnityEngine.Extension.SerializedDictionary;

namespace UnityEditor.Extension.SerializedDictionary
{
    [CustomPropertyDrawer(typeof(UnityObjectDictionary))] public sealed class UnityObjectDictionaryPropertyDrawer : 
        UnityObjectDictionaryPropertyDrawerBase { }
    [CustomPropertyDrawer(typeof(StringUnityObjectDictionary))] public sealed class StringUnityObjectDictionaryPropertyDrawer : 
        StringDictionaryPropertyDrawerBase { }
}
