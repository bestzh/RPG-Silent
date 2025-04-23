using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Core
{
    public abstract class SSPropertyDrawer : PropertyDrawer
    {
        private float _currentLineHeight;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            _currentLineHeight = position.y;
            
            SetProperties(property);

            DrawAllProperties(position, property, label);

            EditorGUI.EndProperty();
        }
        
        protected abstract void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label);
        
        protected void DrawProperty(Rect position, SerializedProperty property, string label)
        {
            Rect variableLabelRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUIUtility.singleLineHeight), position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(variableLabelRect, new GUIContent(label), SSGUIStyle.GetLabelGUIStyle());
            EditorGUI.indentLevel++;
            Rect variableRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(property)), position.width, EditorGUI.GetPropertyHeight(property));
            EditorGUI.PropertyField(variableRect, property);
            EditorGUI.indentLevel--;
        }
        
        protected float GetPropertyArrayHeight(SerializedProperty property)
        {
            float height = 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                height += EditorGUI.GetPropertyHeight(element);
            }

            return height;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }
        
        protected float GetSetCurrentLineHeight(float amount)
        {
            _currentLineHeight += amount;
            
            return _currentLineHeight - amount;
        }

        protected void Space(float amount = 18)
        {
            _currentLineHeight += amount;
        }

        protected abstract void SetProperties(SerializedProperty property);
    }
}