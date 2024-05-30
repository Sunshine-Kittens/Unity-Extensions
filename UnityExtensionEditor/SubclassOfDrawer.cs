using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.Extension;

namespace UnityEditor.Extension
{
    [CustomPropertyDrawer(typeof(SubclassOf<>))]
    public class SubclassOfDrawer : PropertyDrawer
    {
        private class SubclassCache
        {
            public Type baseType = null;
            public Type[] subclassTypes = null;
            public bool dirty = false;

            private SubclassCache() { }

            public SubclassCache(Type baseType)
            {
                this.baseType = baseType;
            }
        }

        private SerializedProperty _classTypeNameProperty = null;
        private SerializedProperty _subclassTypeNameProperty = null;
        private int _selectedIndex = 0;
        private int _selectedClass = -1;

        private static Dictionary<Type, SubclassCache> _cachedSubclassTypes = new Dictionary<Type, SubclassCache>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _classTypeNameProperty = property.FindPropertyRelative("_classTypeName");
            _subclassTypeNameProperty = property.FindPropertyRelative("_subclassTypeName");

            Type[] genericArguments = fieldInfo.FieldType.GetGenericArguments();
            Type[] subclassTypes = GetSubclassTypes(Type.GetType(_classTypeNameProperty.stringValue));
            GUIContent[] subclassLabels = new GUIContent[subclassTypes.Length + 1];
            subclassLabels[0] = new GUIContent("None");
            for (int i = 0; i < subclassTypes.Length; i++)
            {
                subclassLabels[i + 1] = new GUIContent(subclassTypes[i].Name);
            }

            string typeName = property.displayName + " <";
            for (int i = 0; i < genericArguments.Length; i++)
            {
                string genericArgName = genericArguments[i].Name;
                if (genericArguments[i].GenericTypeArguments.Length > 0)
                {
                    genericArgName = genericArgName.Remove(genericArgName.Length - 2);
                    genericArgName += "<";
                    for (int j = 0; j < genericArguments[i].GenericTypeArguments.Length; j++)
                    {
                        genericArgName += genericArguments[i].GenericTypeArguments[j].Name;
                        if (j < genericArguments[i].GenericTypeArguments.Length - 1)
                        {
                            genericArgName += ",";
                        }
                    }
                    genericArgName += ">";
                }
                typeName += genericArgName;
            }
            typeName += ">";


            label = new GUIContent(typeName);
            string subclassString = _subclassTypeNameProperty.stringValue;
            if (!string.IsNullOrWhiteSpace(subclassString) &&
                (_selectedClass == -1 || subclassString != subclassTypes[_selectedClass].AssemblyQualifiedName))
            {
                for (int i = 0; i < subclassTypes.Length; i++)
                {
                    if (subclassString == subclassTypes[i].AssemblyQualifiedName)
                    {
                        _selectedClass = i;
                        _selectedIndex = _selectedClass + 1;
                        break;
                    }
                }
            }

            _selectedIndex = EditorGUI.Popup(position, label, _selectedIndex, subclassLabels);
            _selectedClass = _selectedIndex - 1;

            if (_selectedIndex == 0)
            {
                _subclassTypeNameProperty.stringValue = "";
            }
            else
            {
                _subclassTypeNameProperty.stringValue = subclassTypes[_selectedIndex - 1].AssemblyQualifiedName;
            }
        }

        private Type[] GetSubclassTypes(Type baseType)
        {
            SubclassCache cache;
            if(_cachedSubclassTypes.TryGetValue(baseType, out cache))
            {
                if(!cache.dirty)
                {
                    return cache.subclassTypes;
                }
            }
            else
            {
                cache = new SubclassCache(baseType);
                _cachedSubclassTypes.Add(baseType, cache);
            }
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic).ToArray();
            IEnumerable<Type> exportedTypes = assemblies.SelectMany(domainAssembly => domainAssembly.GetExportedTypes());
            cache.subclassTypes = exportedTypes.Where(type => type.IsSubclassOf(baseType) && type != baseType && !type.IsAbstract).ToArray();
            return cache.subclassTypes;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptReload()
        {
            foreach(KeyValuePair<Type, SubclassCache> pair in _cachedSubclassTypes)
            {
                pair.Value.dirty = true;
            }
        }
    }
}