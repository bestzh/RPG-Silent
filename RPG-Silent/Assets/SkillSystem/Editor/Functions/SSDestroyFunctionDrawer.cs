using SkillSystem.Editor.Core;
using SkillSystem.Functions;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Functions
{
    [CustomPropertyDrawer(typeof(SSDestroyFunction))]
    public class SSDestroyFunctionDrawer : SSPropertyDrawer
    {
        SerializedProperty _targetObject;
        SerializedProperty _destroyDelay;
        
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Function white rect when foldout
            Rect foldoutWhiteRect = new Rect(position.x - 17 ,position.y - EditorGUIUtility.singleLineHeight - 1,
                2, GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(foldoutWhiteRect, SSGUIStyle.GetFunctionFoldoutColor());
            
            Space(10);
            DrawProperty(position, _targetObject, "Object To Destroy");
            
            Space(10);
            DrawProperty(position, _destroyDelay, "Delay");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            if (property.isExpanded)
            {
                height = 66;
                
                SetProperties(property);

                height += EditorGUI.GetPropertyHeight(_targetObject);
                height += EditorGUI.GetPropertyHeight(_destroyDelay);
            }
            
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _targetObject = property.FindPropertyRelative("targetObject");
            _destroyDelay = property.FindPropertyRelative("destroyDelay");
        }
    }
}