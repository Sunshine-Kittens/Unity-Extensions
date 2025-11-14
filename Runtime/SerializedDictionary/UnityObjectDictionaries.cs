using System;

namespace UnityEngine.Extension.SerializedDictionary
{
    [Serializable] public sealed class UnityObjectDictionary : SerializedDictionary<Object, Object> { }
    
    /// <summary>
    /// Requires a new concrete implementations of the dictionary and property drawer in order to be used in the editor. 
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <code language="csharp">
    /// [Serializable] public sealed class ScriptableObjectDictionary : UnityObjectDictionaryT<![CDATA[<ScriptableObject>]]>{ }
    /// ...
    /// [CustomPropertyDrawer(typeof(ScriptableObjectDictionary))] public sealed class ScriptableObjectDictionaryPropertyDrawer : 
    /// UnityObjectDictionaryPropertyDrawerBase { }
    /// </code>
    [Serializable] public abstract class UnityObjectDictionaryT<TObject> : SerializedDictionary<TObject, TObject> where TObject : Object { }
    
    [Serializable] public sealed class StringUnityObjectDictionary : SerializedDictionary<string, Object> { }
    
    /// <summary>
    /// Requires a new concrete implementations of the dictionary and property drawer in order to be used in the editor. 
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    /// <code language="csharp">
    /// [Serializable] public sealed class StringScriptableObjectDictionary : StringUnityObjectDictionaryT<![CDATA[<ScriptableObject>]]>{ }
    /// ...
    /// [CustomPropertyDrawer(typeof(StringScriptableObjectDictionary))] public sealed class StringScriptableObjectDictionaryPropertyDrawer : 
    /// StringDictionaryPropertyDrawerBase { }
    /// </code>
    [Serializable] public abstract class StringUnityObjectDictionaryT<TObject> : SerializedDictionary<string, TObject> where TObject : Object { }
    
    /// <summary>
    /// Requires a new concrete implementations of the dictionary and property drawer in order to be used in the editor. 
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <code language="csharp">
    /// [Serializable] public sealed class MyEnumUnityObjectDictionary : EnumUnityObjectDictionary<![CDATA[<MyEnum>]]>{ }
    /// ...
    /// [CustomPropertyDrawer(typeof(MyEnumUnityObjectDictionary))] public sealed class MyEnumUnityObjectDictionaryPropertyDrawer : 
    /// EnumDictionaryPropertyDrawerBase<![CDATA[<MyEnum>]]> { }
    /// </code>
    [Serializable] public sealed class EnumUnityObjectDictionary<TEnum> : SerializedDictionary<TEnum, Object> where TEnum : Enum { }
    
    /// <summary>
    /// Requires a new concrete implementations of the dictionary and property drawer in order to be used in the editor. 
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <typeparam name="TObject"></typeparam>
    /// <code language="csharp">
    /// [Serializable] public sealed class MyEnumScriptableObjectDictionary : EnumUnityObjectDictionaryT<![CDATA[<MyEnum, ScriptableObject>]]>{ }
    /// ...
    /// [CustomPropertyDrawer(typeof(MyEnumScriptableObjectDictionary))] public sealed class MyEnumScriptableObjectDictionaryPropertyDrawer : 
    /// EnumDictionaryPropertyDrawerBase<![CDATA[<MyEnum>]]> { }
    /// </code>
    [Serializable] public abstract class EnumUnityObjectDictionaryT<TEnum, TObject> : SerializedDictionary<TEnum, TObject> 
        where TEnum : Enum where TObject : Object { }
}   