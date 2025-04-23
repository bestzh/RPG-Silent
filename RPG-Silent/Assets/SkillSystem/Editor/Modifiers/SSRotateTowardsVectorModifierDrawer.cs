using SkillSystem.Editor.Core;
using SkillSystem.Modifiers;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Modifiers
{
    [CustomPropertyDrawer(typeof(SSRotateTowardsVectorModifier))]
    public class SSRotateTowardsVectorModifierDrawer : SSPropertyDrawer
    {
        SerializedProperty _vectorForward;
        SerializedProperty _vectorUp;

        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            // Foldout title
            Rect labelRect = new Rect(position.x, GetSetCurrentLineHeight(0), position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, "Rotate Towards Vector Modifier", SSGUIStyle.GetLabelGUIStyle());
            Rect foldoutRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUIUtility.singleLineHeight), position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, " ", true);

            if (property.isExpanded)
            {
                //Variable space indicator rect
                Rect spaceIndicatorRect = new Rect(position.x + 1 + EditorGUI.indentLevel * 15, position.y + 18,
                    2, GetPropertyHeight(property, label) - 18);
                EditorGUI.DrawRect(spaceIndicatorRect, SSGUIStyle.GetSpaceIndicatorColor());
            
                EditorGUI.indentLevel++;
                
                DrawProperty(position, _vectorForward, "Vector Forward");
                DrawProperty(position, _vectorUp, "Vector Up");
                
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 18;

            if (property.isExpanded)
            {
                height = 54;
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_vectorForward);
                height += EditorGUI.GetPropertyHeight(_vectorUp);
            }
            
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _vectorForward = property.FindPropertyRelative("vectorForward");
            _vectorUp = property.FindPropertyRelative("vectorUp");
        }
    }
}