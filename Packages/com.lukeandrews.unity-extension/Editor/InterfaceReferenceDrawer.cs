using System;

using UnityEngine;
using UnityEngine.Extension;

namespace UnityEditor.Extension
{
    [CustomPropertyDrawer(typeof(InterfaceReference<>))]
    public class InterfaceReferenceDrawer : PropertyDrawer
    {
        private SerializedProperty _unityObjectProperty = null;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _unityObjectProperty = property.FindPropertyRelative("_unityObject");
            Type interfaceType = GetInterfaceType();

            if (interfaceType == null)
            {
                throw new InvalidOperationException("Generic argument for interface reference is null.");
            }

            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException("Generic argument for interface reference is not an interface.");
            }

            Type[] genericArguments = fieldInfo.FieldType.GetGenericArguments();
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

            EditorGUI.BeginChangeCheck();
            UnityEngine.Object obj = EditorGUILayout.ObjectField(label, _unityObjectProperty.objectReferenceValue, typeof(UnityEngine.Object), true);

            if (EditorGUI.EndChangeCheck())
            {
                if (obj != null)
                {
                    Type objType = obj.GetType();
                    if (!interfaceType.IsAssignableFrom(objType))
                    {
                        if (obj is GameObject)
                        {
                            GameObject gameObject = obj as GameObject;
                            obj = gameObject.GetComponent(interfaceType);
                        }
                        else
                        {
                            obj = null;
                        }
                    }
                }
                _unityObjectProperty.objectReferenceValue = obj;
            }
        }

        Type GetInterfaceType()
        {
            Type[] genericArguments = fieldInfo.FieldType.GetGenericArguments();
            return genericArguments[0];
        }
    }
}