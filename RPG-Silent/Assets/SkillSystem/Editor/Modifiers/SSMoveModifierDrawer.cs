using SkillSystem.Editor.Core;
using SkillSystem.Modifiers;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Modifiers
{
    [CustomPropertyDrawer(typeof(SSMoveModifier))]
    public class SSMoveModifierDrawer : SSPropertyDrawer
    {
        SerializedProperty _vector;

        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            // Foldout title
            Rect labelRect = new Rect(position.x, GetSetCurrentLineHeight(0), position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, "Move Modifier", SSGUIStyle.GetLabelGUIStyle());
            Rect foldoutRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUIUtility.singleLineHeight), position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, " ", true);

            if (property.isExpanded)
            {
                //Variable space indicator rect
                Rect spaceIndicatorRect = new Rect(position.x + 1 + EditorGUI.indentLevel * 15, position.y + 18,
                    2, GetPropertyHeight(property, label) - 18);
                EditorGUI.DrawRect(spaceIndicatorRect, SSGUIStyle.GetSpaceIndicatorColor());
            
                EditorGUI.indentLevel++;
                Rect vectorRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_vector)), position.width, EditorGUI.GetPropertyHeight(_vector));
                EditorGUI.PropertyField(vectorRect, _vector, new GUIContent("Vector"));
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 18;

            if (property.isExpanded)
            {
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_vector);
            }
            
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _vector = property.FindPropertyRelative("vector");
        }
    }
}