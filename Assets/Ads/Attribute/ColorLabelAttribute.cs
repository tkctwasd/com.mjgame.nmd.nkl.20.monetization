#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ColorLabelAttribute : PropertyAttribute
{
    public readonly float r, g, b;

    public ColorLabelAttribute(float r = 0f, float g = 1f, float b = 0.3f)
    {
        this.r = r;
        this.g = g;
        this.b = b;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ColorLabelAttribute))]
public class ColorLabelDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ColorLabelAttribute attr = (ColorLabelAttribute)attribute;

        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = new Color(attr.r, attr.g, attr.b);

        float labelWidth = EditorGUIUtility.labelWidth;
        Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
        Rect fieldRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, position.height);

        EditorGUI.LabelField(labelRect, label.text, style);
        EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
    }
}
#endif 