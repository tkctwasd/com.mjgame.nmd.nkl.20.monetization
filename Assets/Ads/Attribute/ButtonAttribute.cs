using System;
using System.Linq;
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
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var methods = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttributes(typeof(ButtonAttribute), true).Length > 0);

        foreach (var method in methods)
        {
            var attr = (ButtonAttribute)method.GetCustomAttributes(typeof(ButtonAttribute), true)[0];

            if (attr.SpaceBefore > 0)
                GUILayout.Space(attr.SpaceBefore);

            string label = string.IsNullOrEmpty(attr.Text) ? ObjectNames.NicifyVariableName(method.Name) : attr.Text;

            bool isEnabled = attr.SelectedEnableMode switch
            {
                EButtonEnableMode.Always => true,
                EButtonEnableMode.Editor => !Application.isPlaying,
                EButtonEnableMode.Playmode => Application.isPlaying,
                _ => true
            };

            GUI.enabled = isEnabled;
            if (GUILayout.Button(label))
            {
                foreach (var t in targets)
                {
                    CallMethod(t, method);
                    EditorUtility.SetDirty(t);
                }
            }
            GUI.enabled = true;
        }
    }

    private void CallMethod(object target, MethodInfo method)
    {
        var parameters = method.GetParameters();
        object[] args = parameters.Select(p => GetDefaultValue(p.ParameterType)).ToArray();

        try
        {
            if (method.IsStatic)
                method.Invoke(null, args);
            else
                method.Invoke(target, args);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error invoking {method.Name}: {e.InnerException?.Message ?? e.Message}");
        }
    }

    private object GetDefaultValue(Type type)
    {
        if (type == typeof(string)) return "";
        if (type == typeof(int)) return 0;
        if (type == typeof(float)) return 0f;
        if (type == typeof(bool)) return false;
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
        if (type.IsValueType) return Activator.CreateInstance(type);
        return null;
    }
}
#endif