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
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ColorLabelAttribute attr = (ColorLabelAttribute)attribute;

        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = new Color(attr.r, attr.g, attr.b);

        EditorGUI.BeginProperty(position, label, property);

        // Nếu là List hoặc Array
        if (property.isArray && property.propertyType != SerializedPropertyType.String)
        {
            DrawArray(position, property, label, style);
        }
        else
        {
            DrawNormal(position, property, label, style);
        }

        EditorGUI.EndProperty();
    }

    void DrawNormal(Rect position, SerializedProperty property, GUIContent label, GUIStyle style)
    {
        float labelWidth = EditorGUIUtility.labelWidth;

        Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
        Rect fieldRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, position.height);

        EditorGUI.LabelField(labelRect, label.text, style);
        EditorGUI.PropertyField(fieldRect, property, GUIContent.none, true);
    }

    void DrawArray(Rect position, SerializedProperty property, GUIContent label, GUIStyle baseStyle)
{
    ColorLabelAttribute attr = (ColorLabelAttribute)attribute;

    GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
    foldoutStyle.fontStyle = FontStyle.Bold;

    Color color = new Color(attr.r, attr.g, attr.b);

    foldoutStyle.normal.textColor = color;
    foldoutStyle.onNormal.textColor = color;
    foldoutStyle.focused.textColor = color;
    foldoutStyle.onFocused.textColor = color;

    Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

    property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true, foldoutStyle);

    if (!property.isExpanded) return;

    EditorGUI.indentLevel++;

    float yOffset = position.y + EditorGUIUtility.singleLineHeight;

    for (int i = 0; i < property.arraySize; i++)
    {
        var element = property.GetArrayElementAtIndex(i);
        float height = EditorGUI.GetPropertyHeight(element, true);

        Rect elementRect = new Rect(position.x, yOffset, position.width, height);

        EditorGUI.PropertyField(elementRect, element, new GUIContent($"Element {i}"), true);

        yOffset += height + 2;
    }

    EditorGUI.indentLevel--;
}
}
#endif