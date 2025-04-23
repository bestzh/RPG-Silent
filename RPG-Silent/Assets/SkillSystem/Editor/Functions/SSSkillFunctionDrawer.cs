using SkillSystem.Editor.Core;
using SkillSystem.Functions;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Functions
{
    [CustomPropertyDrawer(typeof(SSSkillFunction))]
    public class SSSkillFunctionDrawer : SSPropertyDrawer
    {
        SerializedProperty _targetSkill;
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Function white rect when foldout
            Rect foldoutWhiteRect = new Rect(position.x - 17 ,position.y - EditorGUIUtility.singleLineHeight - 1,
                2, GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(foldoutWhiteRect, SSGUIStyle.GetFunctionFoldoutColor());
            
            Space(10);
            DrawProperty(position, _targetSkill, "Target Skill");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            if (property.isExpanded)
            {
                height = 38;
                
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_targetSkill);
            }
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _targetSkill = property.FindPropertyRelative("targetSkill");
        }
    }
}