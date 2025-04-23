using SkillSystem.Editor.Core;
using SkillSystem.Enums;
using SkillSystem.Structs;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Structs
{
    [CustomPropertyDrawer(typeof(SSVariableWithValue<>))]
    public class SSVariableWithValueDrawer : SSPropertyDrawer
    {
        SerializedProperty _variableType;
        
        SerializedProperty _variableValue;
        SerializedProperty _dictionaryName;


        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Variable space indicatorRect
            Rect spaceIndicatorRect = new Rect(position.x - 14 + EditorGUI.indentLevel * 15, position.y,
                2, GetPropertyHeight(property, label));
            EditorGUI.DrawRect(spaceIndicatorRect, SSGUIStyle.GetSpaceIndicatorColor());
            
            
            Rect variableTypeRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_variableType) + 2), position.width, EditorGUI.GetPropertyHeight(_variableType));
            EditorGUI.PropertyField(variableTypeRect, _variableType, new GUIContent("Get Variable"));

            if (_variableType.intValue == (int)SSVariableTypeWithValue.ByValue)
            {
                Rect variableValueRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_variableValue)), position.width, EditorGUI.GetPropertyHeight(_variableValue));
                EditorGUI.PropertyField(variableValueRect, _variableValue, new GUIContent("Value"));
            }
            else if (_variableType.intValue == (int)SSVariableTypeWithReference.ByDictionaryKeyName)
            {
                Rect dictionaryNameRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_dictionaryName)), position.width, EditorGUI.GetPropertyHeight(_dictionaryName));
                EditorGUI.PropertyField(dictionaryNameRect, _dictionaryName, new GUIContent("Dictionary Key Name"));
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 2;
            
            SetProperties(property);
            
            height += EditorGUI.GetPropertyHeight(_variableType);
            
            if (_variableType.intValue == (int)SSVariableTypeWithValue.ByValue) height += EditorGUI.GetPropertyHeight(_variableValue);
            else if (_variableType.intValue == (int)SSVariableTypeWithValue.ByDictionaryKeyName) height += EditorGUI.GetPropertyHeight(_dictionaryName);
            
            return height;
        }
        
        protected override void SetProperties(SerializedProperty property)
        {
            _variableType = property.FindPropertyRelative("variableType");
            
            _variableValue = property.FindPropertyRelative("variableValue");
            _dictionaryName = property.FindPropertyRelative("dictionaryName");
        }
    }
}