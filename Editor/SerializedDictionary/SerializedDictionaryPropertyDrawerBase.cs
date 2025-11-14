using System;
using System.Collections.Generic;

using UnityEngine;

namespace UnityEditor.Extension.SerializedDictionary
{
    public abstract class SerializedDictionaryPropertyDrawerBase<TKey> : PropertyDrawer
    {
        private const string _KeysFieldName = "_keys";
        private const string _ValuesFieldName = "_values";
        
        private readonly List<TKey> _snapshotKeysCache = new ();
        private readonly List<string> _errorsCache = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int count = GetCount(property.FindPropertyRelative(_KeysFieldName), property.FindPropertyRelative(_ValuesFieldName));
            int lines = 1 + count;
            return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines - 1);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty keysProperty = property.FindPropertyRelative(_KeysFieldName);
            SerializedProperty valuesProperty = property.FindPropertyRelative(_ValuesFieldName);
            
            SyncValuesToKeys(keysProperty, valuesProperty);
            
            List<TKey> snapshotKeys = BuildSnapshot(keysProperty, in _snapshotKeysCache);

            property.serializedObject.Update();
            Rect r = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            EditorGUI.LabelField(r, label, EditorStyles.boldLabel);
            var countRect = new Rect(r.x + EditorGUIUtility.labelWidth, r.y, 80, r.height);
            EditorGUI.LabelField(countRect, $"Count: {Math.Min(keysProperty.arraySize, valuesProperty.arraySize)}", EditorStyles.miniLabel);

            var addRect = new Rect(r.xMax - 46, r.y, 22, r.height);
            var removeRect = new Rect(r.xMax - 22, r.y, 22, r.height);

            if (GUI.Button(addRect, new GUIContent("+", "Add entry")))
            {
                if (!TryAddEntry(keysProperty, valuesProperty, in snapshotKeys))
                {
                    // Display a help box that we cannot add a new key!
                    Debug.LogError("Failed to add entry, unable to add a safe key.");
                }
            }

            if (GUI.Button(removeRect, new GUIContent("-", "Remove last entry")))
            {
                RemoveLast(keysProperty, valuesProperty, in snapshotKeys); 
            }

            // Move to rows
            r.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            int count = GetCount(keysProperty, valuesProperty);
            List<string> errors = GetErrors(_errorsCache);
            
            // Draw rows and detect changes
            for (int i = 0; i < count; i++)
            {
                Rect row = new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight);
                Rect keyRect = new Rect(row.x, row.y, EditorGUIUtility.labelWidth, row.height);
                Rect valRect = new Rect(row.x + EditorGUIUtility.labelWidth + 4, row.y, row.width - EditorGUIUtility.labelWidth - 60, row.height);
                Rect removeBtnRect = new Rect(row.xMax - 28, row.y, 24, row.height);

                SerializedProperty keyElement = keysProperty.GetArrayElementAtIndex(i);
                SerializedProperty valElement = valuesProperty.GetArrayElementAtIndex(i);

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(keyRect, keyElement, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                { 
                    TKey newKey = (TKey)keyElement.boxedValue;
                    if (KeyExists(newKey, i, in snapshotKeys))
                    {
                        keyElement.boxedValue = snapshotKeys[i];
                        errors.Add($"{newKey.ToString()}, reverted to {snapshotKeys[i].ToString()}");
                    }
                    else
                    {
                        snapshotKeys[i] = newKey;
                    }
                }
                
                EditorGUI.PropertyField(valRect, valElement, GUIContent.none);
                
                if (GUI.Button(removeBtnRect, "X"))
                {
                    DeleteAt(keysProperty, valuesProperty, i, in snapshotKeys);
                    count--;
                    i--;
                }
                r.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            if (errors.Count > 0)
            {
                string errorString = $"There were errors updating keys: ";
                for (int i = 0 ; i < errors.Count; i++)
                {
                    errorString += errors[i];
                }
                // Display a help box that we can use to render the errors
                Debug.LogError(errorString);
            }
            property.serializedObject.ApplyModifiedProperties();
        }

        // Helpers

        private int GetCount(SerializedProperty keysProperty, SerializedProperty valuesProperty)
        {
            return Mathf.Max(keysProperty?.arraySize ?? 0, valuesProperty?.arraySize ?? 0);
        }
        
        private bool TryAddEntry(SerializedProperty keysProperty, SerializedProperty valuesProperty, in List<TKey> existingKeys)
        {
            if (TryGetNewSafeKey(in existingKeys, out TKey safeKey))
            {
                int newIndex = Math.Max(keysProperty.arraySize, valuesProperty.arraySize);
                keysProperty.arraySize = newIndex + 1;
                valuesProperty.arraySize = newIndex + 1;
                keysProperty.GetArrayElementAtIndex(newIndex).boxedValue = safeKey;
                existingKeys.Add(safeKey);
                return true;
            }
            return false;
        }

        protected abstract bool TryGetNewSafeKey(in List<TKey> existingKeys, out TKey safeKey);
        
        private void RemoveLast(SerializedProperty keysProperty, SerializedProperty valuesProperty, in List<TKey> existingKeys)
        {
            DeleteAt(keysProperty, valuesProperty, GetCount(keysProperty, valuesProperty) - 1, in existingKeys);
        }

        private void DeleteAt(SerializedProperty keysProperty, SerializedProperty valuesProperty, int index, in List<TKey> existingKeys)
        {
            if (index >= 0 && index < GetCount(keysProperty, valuesProperty))
            {
                keysProperty.DeleteArrayElementAtIndex(index);
                valuesProperty.DeleteArrayElementAtIndex(index);
                existingKeys.RemoveAt(index);
            }
        }

        private bool KeyExists(TKey candidate, int candidateIndex, in List<TKey> existingKeys)
        {
            for (int i = 0; i < existingKeys.Count; i++)
            {
                if (i == candidateIndex) continue;
                TKey key = existingKeys[i];
                if (key.Equals(candidate))
                {
                    return true;
                }
            }
            return false;
        }

        private void SyncValuesToKeys(SerializedProperty keysProperty, SerializedProperty valuesProperty)
        {
            if (keysProperty.arraySize != valuesProperty.arraySize)
            {
                valuesProperty.arraySize = keysProperty.arraySize;
            }
        }
        
        private List<TKey> BuildSnapshot(SerializedProperty keysProp, in List<TKey> cache)
        {
            cache.Clear();
            if (keysProp != null)
            {
                for (int i = 0; i < keysProp.arraySize; i++)
                {
                    _snapshotKeysCache.Add((TKey)keysProp.GetArrayElementAtIndex(i).boxedValue);
                }   
            }
            return cache;
        }

        private List<string> GetErrors(in List<string> cache)
        {
            cache.Clear();
            return cache;
        }
    }

    public abstract class StringDictionaryPropertyDrawerBase : SerializedDictionaryPropertyDrawerBase<string>
    {
        protected sealed override bool TryGetNewSafeKey(in List<string> existingKeys, out string safeKey)
        {
            int keySuffix = existingKeys.Count + 1;
            string key = $"NewKey{keySuffix}";
            while (existingKeys.Contains(key))
            {
                keySuffix++;
                key = $"NewKey{keySuffix}";
            }
            safeKey = key;
            return true;
        }
    }
    
    public abstract class UnityObjectDictionaryPropertyDrawerBase : SerializedDictionaryPropertyDrawerBase<UnityEngine.Object>
    {
        protected sealed override bool TryGetNewSafeKey(in List<UnityEngine.Object> existingKeys, out UnityEngine.Object safeKey)
        {
            safeKey = null;
            for (int i = 0; i < existingKeys.Count; i++)
            {
                if (existingKeys[i] == null)
                {
                    return false;
                }
            }
            return true;
        }
    }
    
    public abstract class EnumDictionaryPropertyDrawerBase<TEnum> : SerializedDictionaryPropertyDrawerBase<TEnum> where TEnum : Enum
    {
        protected sealed override bool TryGetNewSafeKey(in List<TEnum> existingKeys, out TEnum safeKey)
        {
            foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
            {
                if (!existingKeys.Contains(value))
                {
                    safeKey = value;
                    return true;
                }
            }
            safeKey = default;
            return false;
        }
    }
}