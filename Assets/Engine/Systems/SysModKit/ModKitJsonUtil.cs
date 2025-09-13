// This file is stripped for runtime/player builds.
// The original ModKitJsonEditorUtil is editor-only.

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Collections;

public static class ModKitJsonEditorUtil
{
    public static object DrawObject(string label, object obj, Type objType)
    {
        if (obj == null)
        {
            // Try to instantiate nested objects if null
            if (!objType.IsAbstract && objType.GetConstructor(Type.EmptyTypes) != null)
                obj = Activator.CreateInstance(objType);
            else
                return null;
        }

        if (objType == typeof(string))
            return EditorGUILayout.TextField(label, (string)obj);

        if (objType == typeof(int))
            return EditorGUILayout.IntField(label, (int)obj);

        if (objType == typeof(float))
            return EditorGUILayout.FloatField(label, (float)obj);

        if (objType == typeof(bool))
            return EditorGUILayout.Toggle(label, (bool)obj);

        if (objType == typeof(Color32))
            return (Color32)(Color)EditorGUILayout.ColorField(label, (Color32)obj);

        if (objType.IsEnum)
            return EditorGUILayout.EnumPopup(label, (Enum)obj);

        if (objType.IsArray)
        {
            return DrawArray(label, (Array)obj, objType.GetElementType());
        }

        // Complex/nested object
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        foreach (var field in objType.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = field.GetValue(obj);
            var newValue = DrawObject(field.Name, value, field.FieldType);
            if (!Equals(value, newValue))
                field.SetValue(obj, newValue);
        }
        EditorGUI.indentLevel--;

        return obj;
    }

    private static Array DrawArray(string label, Array arr, Type elemType)
    {
        int size = arr != null ? arr.Length : 0;
        int newSize = EditorGUILayout.IntField(label + " Size", size);

        if (newSize != size)
        {
            var newArr = Array.CreateInstance(elemType, newSize);
            if (arr != null)
                Array.Copy(arr, newArr, Math.Min(size, newSize));
            arr = newArr;
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < (arr?.Length ?? 0); i++)
        {
            var value = arr.GetValue(i);
            var newValue = DrawObject($"{label}[{i}]", value, elemType);
            arr.SetValue(newValue, i);
        }
        EditorGUI.indentLevel--;

        return arr;
    }
}
#endif
