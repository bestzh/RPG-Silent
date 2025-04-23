using SkillSystem.Editor.Core;
using SkillSystem.Modifiers;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Modifiers
{
    [CustomPropertyDrawer(typeof(SSVector3MultiplyModifier))]
    public class SSVector3MultiplyModifierDrawer : SSPropertyDrawer
    {
        SerializedProperty _multiplier;

        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            // Foldout title
            Rect labelRect = new Rect(position.x, GetSetCurrentLineHeight(0), position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, "Multiply Modifier", SSGUIStyle.GetLabelGUIStyle());
            Rect foldoutRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUIUtility.singleLineHeight), position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, " ", true);

            if (property.isExpanded)
            {
                //Variable space indicator rect
                Rect spaceIndicatorRect = new Rect(position.x + 1 + EditorGUI.indentLevel * 15, position.y + 18,
                    2, GetPropertyHeight(property, label) - 18);
                EditorGUI.DrawRect(spaceIndicatorRect, SSGUIStyle.GetSpaceIndicatorColor());
            
                EditorGUI.indentLevel++;
                Rect multiplierRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_multiplier)), position.width, EditorGUI.GetPropertyHeight(_multiplier));
                EditorGUI.PropertyField(multiplierRect, _multiplier, new GUIContent("Multiplier"));
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 18;

            if (property.isExpanded)
            {
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_multiplier);
            }
            
            return height;
        }


        protected override void SetProperties(SerializedProperty property)
        {
            _multiplier = property.FindPropertyRelative("multiplier");
        }
    }
}