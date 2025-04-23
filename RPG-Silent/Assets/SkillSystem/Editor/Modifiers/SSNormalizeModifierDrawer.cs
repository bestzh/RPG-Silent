using SkillSystem.Editor.Core;
using SkillSystem.Modifiers;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Modifiers
{
    [CustomPropertyDrawer(typeof(SSNormalizeModifier))]
    public class SSNormalizeModifierDrawer : SSPropertyDrawer
    {
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            // Foldout title
            Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, "Normalize Modifier", SSGUIStyle.GetLabelGUIStyle());
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 18;
            
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            
        }
    }
}