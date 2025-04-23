using SkillSystem.Editor.Core;
using SkillSystem.Functions;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Functions
{
    [CustomPropertyDrawer(typeof(SSEventFunction))]
    public class SSEventFunctionDrawer : SSPropertyDrawer
    {
        private SerializedProperty _targetEvent;
        
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Function white rect when foldout
            Rect foldoutWhiteRect = new Rect(position.x - 17 ,position.y - EditorGUIUtility.singleLineHeight - 1,
                2, GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(foldoutWhiteRect, SSGUIStyle.GetFunctionFoldoutColor());
            
            Space(10);
            DrawProperty(position, _targetEvent, "Target Event");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            if (property.isExpanded)
            {
                height += 38;
                
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_targetEvent);
            }
            
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _targetEvent = property.FindPropertyRelative("targetEvent");
        }
    }
}