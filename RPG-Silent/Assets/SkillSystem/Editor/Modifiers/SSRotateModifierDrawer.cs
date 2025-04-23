using SkillSystem.Editor.Core;
using SkillSystem.Modifiers;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Modifiers
{
    [CustomPropertyDrawer(typeof(SSRotateModifier))]
    public class SSRotateModifierDrawer : SSPropertyDrawer
    {
        SerializedProperty _rotation;

        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            // Foldout title
            Rect labelRect = new Rect(position.x, GetSetCurrentLineHeight(0), position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, "Rotate Modifier", SSGUIStyle.GetLabelGUIStyle());
            Rect foldoutRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUIUtility.singleLineHeight), position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, " ", true);

            if (property.isExpanded)
            {
                //Variable space indicator rect
                Rect spaceIndicatorRect = new Rect(position.x + 1 + EditorGUI.indentLevel * 15, position.y + 18,
                    2, GetPropertyHeight(property, label) - 18);
                EditorGUI.DrawRect(spaceIndicatorRect, SSGUIStyle.GetSpaceIndicatorColor());
            
                EditorGUI.indentLevel++;
                Rect rotationRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_rotation)), position.width, EditorGUI.GetPropertyHeight(_rotation));
                EditorGUI.PropertyField(rotationRect, _rotation, new GUIContent("Rotation"));
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 18;

            if (property.isExpanded)
            {
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_rotation);
            }
            
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _rotation = property.FindPropertyRelative("rotation");
        }
    }
}