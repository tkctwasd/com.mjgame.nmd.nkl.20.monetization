// using System;
// using System.Linq;
// #if UNITY_EDITOR
// using System.Reflection;
// using UnityEditor;
// #endif
// using UnityEngine;

// public enum EButtonEnableMode
// {
//     Always,
//     Editor,
//     Playmode
// }


// [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
// public class ButtonAttribute : Attribute
// {
//     public string Text { get; private set; }
//     public float SpaceBefore { get; private set; }
//     public EButtonEnableMode SelectedEnableMode { get; private set; }

//     public ButtonAttribute()
//     {
//         Text = null;
//         SpaceBefore = 0f;
//         SelectedEnableMode = EButtonEnableMode.Always;
//     }

//     public ButtonAttribute(string labelOrSpace)
//     {
//         if (float.TryParse(labelOrSpace, out float space))
//         {
//             SpaceBefore = space;
//             Text = null;
//         }
//         else
//         {
//             Text = labelOrSpace;
//             SpaceBefore = 0f;
//         }

//         SelectedEnableMode = EButtonEnableMode.Always;
//     }
//     public ButtonAttribute(float space)
//     {
//         Text = null;
//         SpaceBefore = space;
//         SelectedEnableMode = EButtonEnableMode.Always;
//     }

//     public ButtonAttribute(string text, float space)
//     {
//         Text = text;
//         SpaceBefore = space;
//         SelectedEnableMode = EButtonEnableMode.Always;
//     }
//     public ButtonAttribute(string text, EButtonEnableMode mode)
//     {
//         Text = text;
//         SpaceBefore = 0f;
//         SelectedEnableMode = mode;
//     }

//     public ButtonAttribute(float space, EButtonEnableMode mode)
//     {
//         Text = null;
//         SpaceBefore = space;
//         SelectedEnableMode = mode;
//     }

//     public ButtonAttribute(string text, float space, EButtonEnableMode mode)
//     {
//         Text = text;
//         SpaceBefore = space;
//         SelectedEnableMode = mode;
//     }
// }


// #if UNITY_EDITOR
// [CustomEditor(typeof(UnityEngine.Object), true)]
// [CanEditMultipleObjects]
// public class ButtonAttributeDrawer : Editor
// {
//     private static System.Collections.Generic.Dictionary<string, float> _floatParameterValues = new System.Collections.Generic.Dictionary<string, float>();

//     public override void OnInspectorGUI()
//     {
//         base.OnInspectorGUI();

//         var methods = target.GetType()
//             .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
//             .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), true).Length > 0);

//         foreach (var method in methods)
//         {
//             var attr = (ButtonAttribute)method.GetCustomAttributes(typeof(ButtonAttribute), true)[0];

//             if (attr.SpaceBefore > 0)
//                 GUILayout.Space(attr.SpaceBefore);

//             string label = string.IsNullOrEmpty(attr.Text) ? ObjectNames.NicifyVariableName(method.Name) : attr.Text;

//             bool isEnabled = attr.SelectedEnableMode switch
//             {
//                 EButtonEnableMode.Always => true,
//                 EButtonEnableMode.Editor => !Application.isPlaying,
//                 EButtonEnableMode.Playmode => Application.isPlaying,
//                 _ => true
//             };

//             GUI.enabled = isEnabled;

//             var parameters = method.GetParameters();
//             bool hasFloatParameter = parameters.Length == 1 && parameters[0].ParameterType == typeof(float);

//             EditorGUILayout.BeginHorizontal();

//             if (hasFloatParameter)
//             {

//                 if (!_floatParameterValues.ContainsKey(method.Name))
//                 {
//                     _floatParameterValues[method.Name] = 0f;
//                 }
//                 _floatParameterValues[method.Name] = EditorGUILayout.FloatField("Value:", _floatParameterValues[method.Name]);
//             }

//             if (GUILayout.Button(label))
//             {
//                 foreach (var t in targets)
//                 {
//                     CallMethod(t, method);
//                     EditorUtility.SetDirty(t);
//                 }
//             }
//             EditorGUILayout.EndHorizontal();

//             GUI.enabled = true;
//         }
//     }

//     private void CallMethod(object target, MethodInfo method)
//     {
//         var parameters = method.GetParameters();
//         object[] args;

//         if (parameters.Length == 1 && parameters[0].ParameterType == typeof(float))
//         {
//             float valueToPass = _floatParameterValues.ContainsKey(method.Name) ? _floatParameterValues[method.Name] : 0f;
//             args = new object[] { valueToPass };
//         }
//         else
//         {
//             args = parameters.Select(p => GetDefaultValue(p.ParameterType)).ToArray();
//         }

//         try
//         {
//             method.Invoke(target, args);
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"❌ Error invoking {method.Name}: {e.InnerException?.Message ?? e.Message}");
//         }
//     }
//     private object GetDefaultValue(Type type)
//     {
//         if (type == typeof(string)) return "";
//         if (type == typeof(int)) return 0;
//         if (type == typeof(float)) return 0f;
//         if (type == typeof(bool)) return false;
//         if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
//         if (type.IsValueType) return Activator.CreateInstance(type);
//         return null;
//     }
// }

// #endif

using System;
using System.Linq;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine;

public enum EButtonEnableMode
{
    Always,
    Editor,
    Playmode
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ButtonAttribute : Attribute
{
    public string Text { get; private set; }
    public float SpaceBefore { get; private set; }
    public EButtonEnableMode SelectedEnableMode { get; private set; }

    public ButtonAttribute()
    {
        Text = null;
        SpaceBefore = 0f;
        SelectedEnableMode = EButtonEnableMode.Always;
    }

    public ButtonAttribute(string labelOrSpace)
    {
        if (float.TryParse(labelOrSpace, out float space))
        {
            SpaceBefore = space;
            Text = null;
        }
        else
        {
            Text = labelOrSpace;
            SpaceBefore = 0f;
        }
        SelectedEnableMode = EButtonEnableMode.Always;
    }

    public ButtonAttribute(float space)
    {
        Text = null;
        SpaceBefore = space;
        SelectedEnableMode = EButtonEnableMode.Always;
    }

    public ButtonAttribute(string text, float space)
    {
        Text = text;
        SpaceBefore = space;
        SelectedEnableMode = EButtonEnableMode.Always;
    }

    public ButtonAttribute(string text, EButtonEnableMode mode)
    {
        Text = text;
        SpaceBefore = 0f;
        SelectedEnableMode = mode;
    }

    public ButtonAttribute(float space, EButtonEnableMode mode)
    {
        Text = null;
        SpaceBefore = space;
        SelectedEnableMode = mode;
    }

    public ButtonAttribute(int space, EButtonEnableMode mode)
    {
        Text = null;
        SpaceBefore = space;
        SelectedEnableMode = mode;
    }

    public ButtonAttribute(string text, float space, EButtonEnableMode mode)
    {
        Text = text;
        SpaceBefore = space;
        SelectedEnableMode = mode;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UnityEngine.Object), true)]
[CanEditMultipleObjects]
public class ButtonAttributeDrawer : Editor
{
    // Cache giá trị tham số theo MethodSignature -> (ParamName -> object)
    // MethodSignature gồm: targetType.FullName + "." + methodName + "(" + string.Join(",", paramTypes) + ")"
    private static readonly Dictionary<string, Dictionary<string, object>> _paramCache
        = new Dictionary<string, Dictionary<string, object>>();

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Lấy tất cả method có [Button]
        var methods = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), true).Length > 0)
            .OrderBy(m => m.Name);

        foreach (var method in methods)
        {
            var attr = (ButtonAttribute)method.GetCustomAttributes(typeof(ButtonAttribute), true)[0];

            if (attr.SpaceBefore > 0f)
                GUILayout.Space(attr.SpaceBefore);

            string label = string.IsNullOrEmpty(attr.Text)
                ? ObjectNames.NicifyVariableName(method.Name)
                : attr.Text;

            bool isEnabled = attr.SelectedEnableMode switch
            {
                EButtonEnableMode.Always => true,
                EButtonEnableMode.Editor => !Application.isPlaying,
                EButtonEnableMode.Playmode => Application.isPlaying,
                _ => true
            };

            var parameters = method.GetParameters();

            // Tạo signature duy nhất cho method này (đảm bảo không đụng cache giữa các overload)
            string methodSignature = BuildMethodSignature(target.GetType(), method);

            if (!_paramCache.ContainsKey(methodSignature))
                _paramCache[methodSignature] = new Dictionary<string, object>();

            // Vẽ UI tham số (nếu có)
            if (parameters.Length > 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(!isEnabled))
                {
                    DrawParametersUI(_paramCache[methodSignature], parameters, targets);
                    // Nút Execute
                    if (GUILayout.Button("Execute"))
                    {
                        InvokeOnTargets(method, parameters, _paramCache[methodSignature]);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                using (new EditorGUI.DisabledScope(!isEnabled))
                {
                    if (GUILayout.Button(label))
                    {
                        InvokeOnTargets(method, parameters, null);
                    }
                }
            }
        }
    }

    private static string BuildMethodSignature(Type targetType, MethodInfo method)
    {
        var paramTypes = method.GetParameters().Select(p => p.ParameterType.FullName);
        return $"{targetType.FullName}.{method.Name}({string.Join(",", paramTypes)})";
    }

    private static void DrawParametersUI(Dictionary<string, object> cache, ParameterInfo[] parameters, UnityEngine.Object[] contextTargets)
    {
        foreach (var p in parameters)
        {
            string key = p.Name;

            if (!cache.ContainsKey(key))
            {
                cache[key] = GetDefaultForType(p.ParameterType);
            }

            object current = cache[key];
            Type t = p.ParameterType;

            // Hỗ trợ enum
            if (t.IsEnum)
            {
                var enumValue = current is Enum e ? e : (Enum)Enum.GetValues(t).GetValue(0);
                var newEnum = EditorGUILayout.EnumPopup(ObjectNames.NicifyVariableName(key), enumValue);
                cache[key] = newEnum;
                continue;
            }

            // Hỗ trợ các Unity Object
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            {
                var newObj = EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(key), current as UnityEngine.Object, t, true);
                cache[key] = newObj;
                continue;
            }

            // Các kiểu built-in/phổ biến
            if (t == typeof(int))
            {
                int v = current is int iv ? iv : 0;
                cache[key] = EditorGUILayout.IntField(ObjectNames.NicifyVariableName(key), v);
            }
            else if (t == typeof(float))
            {
                float v = current is float fv ? fv : 0f;
                cache[key] = EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(key), v);
            }
            else if (t == typeof(bool))
            {
                bool v = current is bool bv && bv;
                cache[key] = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(key), v);
            }
            else if (t == typeof(string))
            {
                string v = current as string ?? string.Empty;
                cache[key] = EditorGUILayout.TextField(ObjectNames.NicifyVariableName(key), v);
            }
            else if (t == typeof(Vector2))
            {
                Vector2 v = current is Vector2 v2 ? v2 : Vector2.zero;
                cache[key] = EditorGUILayout.Vector2Field(ObjectNames.NicifyVariableName(key), v);
            }
            else if (t == typeof(Vector3))
            {
                Vector3 v = current is Vector3 v3 ? v3 : Vector3.zero;
                cache[key] = EditorGUILayout.Vector3Field(ObjectNames.NicifyVariableName(key), v);
            }
            else if (t == typeof(Vector4))
            {
                Vector4 v = current is Vector4 v4 ? v4 : Vector4.zero;
                cache[key] = EditorGUILayout.Vector4Field(ObjectNames.NicifyVariableName(key), v);
            }
            else if (t == typeof(Color))
            {
                Color v = current is Color c ? c : Color.white;
                cache[key] = EditorGUILayout.ColorField(ObjectNames.NicifyVariableName(key), v);
            }
            else if (t == typeof(AnimationCurve))
            {
                AnimationCurve v = current as AnimationCurve ?? AnimationCurve.Linear(0, 0, 1, 1);
                cache[key] = EditorGUILayout.CurveField(ObjectNames.NicifyVariableName(key), v);
            }
            else
            {
                // Kiểu chưa hỗ trợ UI cụ thể -> hiển thị read-only và dùng default khi invoke
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.LabelField($"{ObjectNames.NicifyVariableName(key)} ({t.Name})", "Unsupported parameter type");
                }
                // đảm bảo có default value để Invoke không null-ref
                cache[key] = GetDefaultForType(t);
            }
        }
    }

    private void InvokeOnTargets(MethodInfo method, ParameterInfo[] parameters, Dictionary<string, object> cache)
    {
        foreach (var t in targets)
        {
            object[] args = BuildArgs(parameters, cache);
            try
            {
                method.Invoke(t, args);
                EditorUtility.SetDirty(t);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error invoking {method.Name} on {t.name}: {e.InnerException?.Message ?? e.Message}");
            }
        }
    }

    private static object[] BuildArgs(ParameterInfo[] parameters, Dictionary<string, object> cache)
    {
        if (parameters == null || parameters.Length == 0) return Array.Empty<object>();
        var args = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (cache != null && cache.TryGetValue(p.Name, out var value))
            {
                args[i] = Coerce(value, p.ParameterType);
            }
            else
            {
                args[i] = GetDefaultForType(p.ParameterType);
            }
        }
        return args;
    }

    private static object Coerce(object value, Type targetType)
    {
        if (value == null) return GetDefaultForType(targetType);

        if (targetType.IsAssignableFrom(value.GetType()))
            return value;

        try
        {
            if (targetType.IsEnum)
            {
                if (value is int iv) return Enum.ToObject(targetType, iv);
                if (value is string s) return Enum.Parse(targetType, s, true);
            }
            // Convert cho kiểu cơ bản
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return GetDefaultForType(targetType);
        }
    }

    private static object GetDefaultForType(Type t)
    {
        if (t == typeof(string)) return "";
        if (t == typeof(int)) return 0;
        if (t == typeof(float)) return 0f;
        if (t == typeof(bool)) return false;
        if (t == typeof(Vector2)) return Vector2.zero;
        if (t == typeof(Vector3)) return Vector3.zero;
        if (t == typeof(Vector4)) return Vector4.zero;
        if (t == typeof(Color)) return Color.white;
        if (t == typeof(AnimationCurve)) return AnimationCurve.Linear(0, 0, 1, 1);
        if (t.IsEnum) return Enum.GetValues(t).GetValue(0);
        if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return null;
        if (t.IsValueType) return Activator.CreateInstance(t);
        return null;
    }
}
#endif