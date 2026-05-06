using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class ShowIfAttribute : PropertyAttribute
{
    public string conditionField;
    public int enumValue;

    public ShowIfAttribute(string conditionField, int enumValue)
    {
        this.conditionField = conditionField;
        this.enumValue = enumValue;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
            return EditorGUI.GetPropertyHeight(property, label, true);

        // Ẩn hoàn toàn (không để lại khoảng trống)
        return 0f;
    }

    private bool ShouldShow(SerializedProperty property)
    {
        ShowIfAttribute showIf = (ShowIfAttribute)attribute;

        // Tính toán đường dẫn đầy đủ đến field điều kiện
        string fullPath = GetConditionPath(property, showIf.conditionField);

        SerializedProperty conditionProp = property.serializedObject.FindProperty(fullPath);

        if (conditionProp == null)
            return true; // Nếu không tìm thấy điều kiện → hiển thị mặc định (an toàn)

        if (conditionProp.propertyType == SerializedPropertyType.Enum)
        {
            return conditionProp.enumValueIndex == showIf.enumValue;
        }

        if (conditionProp.propertyType == SerializedPropertyType.Boolean)
        {
            return conditionProp.boolValue;
        }

        return true;
    }

    /// <summary>
    /// Dựng lại path đầy đủ của conditionField, hoạt động được cả trong class Serializable.
    /// </summary>
    private string GetConditionPath(SerializedProperty property, string conditionField)
    {
        string path = property.propertyPath;
        int lastDot = path.LastIndexOf('.');
        if (lastDot >= 0)
        {
            // ví dụ: Door.doorType → Door.rotateAxis
            path = path.Substring(0, lastDot + 1) + conditionField;
        }
        else
        {
            // field cấp cao nhất
            path = conditionField;
        }

        return path;
    }
}
#endif
