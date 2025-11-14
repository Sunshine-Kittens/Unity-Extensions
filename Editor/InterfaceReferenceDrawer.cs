using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Extension;

namespace UnityEditor.Extension
{
    [CustomPropertyDrawer(typeof(InterfaceReference<>))]
    public class InterfaceReferenceDrawer : PropertyDrawer
    {
        private SerializedProperty _unityObjectProperty = null;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        
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
            
            string typeName = property.displayName + " <";
            string genericArgName = interfaceType.Name;
            if (interfaceType.GenericTypeArguments.Length > 0)
            {
                genericArgName += "<";
                for (int j = 0; j < interfaceType.GenericTypeArguments.Length; j++)
                {
                    genericArgName += interfaceType.GenericTypeArguments[j].Name;
                    if (j < interfaceType.GenericTypeArguments.Length - 1)
                    {
                        genericArgName += ",";
                    }
                }
                genericArgName += ">";   
            }
            typeName += genericArgName;
            typeName += ">";
            label = new GUIContent(typeName);

            EditorGUI.BeginChangeCheck();
            UnityEngine.Object obj = EditorGUI.ObjectField(position, label, _unityObjectProperty.objectReferenceValue, typeof(UnityEngine.Object), true);

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
            Type interfaceRefType = fieldInfo.FieldType;
            if (interfaceRefType.IsArray)
            {
                interfaceRefType = interfaceRefType.GetElementType();    
            }
            else if (interfaceRefType.IsGenericType)
            {
                if (interfaceRefType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    interfaceRefType = interfaceRefType.GetGenericArguments()[0];
                }
                else
                {
                    foreach (Type @interface in interfaceRefType.GetInterfaces())
                    {
                        if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IList<>))
                        {
                            interfaceRefType = @interface.GetGenericArguments()[0];
                        }
                    }   
                }
            }

            if (interfaceRefType != null && interfaceRefType.IsGenericType)
            {
                Type[] genericArguments = interfaceRefType.GetGenericArguments();
                return genericArguments[0];
            }
            return null;
        }
    }
}