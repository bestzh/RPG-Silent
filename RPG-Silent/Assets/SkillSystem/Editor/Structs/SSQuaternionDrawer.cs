using SkillSystem.Editor.Core;
using SkillSystem.Enums;
using SkillSystem.Modifiers;
using SkillSystem.Structs;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Structs
{
    [CustomPropertyDrawer(typeof(SSQuaternion))]
    public class SSQuaternionDrawer : SSPropertyDrawer
    {
        SerializedProperty _variableType;
        
        SerializedProperty _variableValue;
        SerializedProperty _variableReference;
        SerializedProperty _dictionaryName;
        
        SerializedProperty _modifiers;
        
        private enum ModifierType {AddModifier, SaveToDictionary, Multiply, Normalize, Randomize, Rotate, RotateTowardsVector}
        private ModifierType _modifierType;
        
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Variable space indicatorRect
            Rect spaceIndicatorRect = new Rect(position.x - 14 + EditorGUI.indentLevel * 15, position.y,
                2, GetPropertyHeight(property, label));
            EditorGUI.DrawRect(spaceIndicatorRect, SSGUIStyle.GetSpaceIndicatorColor());
            
            Rect variableTypeRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_variableType) + 2), position.width, EditorGUI.GetPropertyHeight(_variableType));
            EditorGUI.PropertyField(variableTypeRect, _variableType, new GUIContent("Get Variable"));

            if (_variableType.intValue == (int)SSVariableType.ByValue)
            {
                Rect variableValueRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_variableValue) + 2), position.width, EditorGUI.GetPropertyHeight(_variableValue));
                EditorGUI.PropertyField(variableValueRect, _variableValue, new GUIContent("Value"));
            }
            else if (_variableType.intValue == (int)SSVariableType.ByReference)
            {
                Rect variableReferenceRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_variableReference) + 2), position.width, EditorGUI.GetPropertyHeight(_variableReference));
                EditorGUI.PropertyField(variableReferenceRect, _variableReference, new GUIContent("Reference"));
            }
            else if (_variableType.intValue == (int)SSVariableType.ByDictionaryKeyName)
            {
                Rect dictionaryNameRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_dictionaryName) + 2), position.width, EditorGUI.GetPropertyHeight(_dictionaryName));
                EditorGUI.PropertyField(dictionaryNameRect, _dictionaryName, new GUIContent("Dictionary Key Name"));
            }
            
            DrawModifiers(position);
        }
        private void DrawModifiers(Rect position)
        {
            Rect modifierIndicatorRect = new Rect(position.x + 1 + EditorGUI.indentLevel * 15, GetSetCurrentLineHeight(0) + 18,
                2, GetPropertyArrayHeight(_modifiers) + 18);
            EditorGUI.DrawRect(modifierIndicatorRect, SSGUIStyle.GetSpaceIndicatorColor());
            
            Rect modifiersLabelRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUIUtility.singleLineHeight), position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(modifiersLabelRect, "Modifiers", SSGUIStyle.GetLabelGUIStyle());
            
            EditorGUI.indentLevel++;
            Rect modifiersRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUIUtility.singleLineHeight), position.width, EditorGUIUtility.singleLineHeight);
            _modifierType = (ModifierType)EditorGUI.EnumPopup(modifiersRect, _modifierType);
            
            if (_modifierType != ModifierType.AddModifier)
            {
                _modifiers.arraySize++;
                SerializedProperty newElement = _modifiers.GetArrayElementAtIndex(_modifiers.arraySize - 1);
                switch (_modifierType)
                {
                    case ModifierType.SaveToDictionary:
                        newElement.managedReferenceValue = new SSSaveToDictionaryModifier();
                        break;
                    case ModifierType.Multiply:
                        newElement.managedReferenceValue = new SSVector3MultiplyModifier();
                        break;
                    case ModifierType.Normalize:
                        newElement.managedReferenceValue = new SSNormalizeModifier();
                        break;
                    case ModifierType.Randomize:
                        newElement.managedReferenceValue = new SSRandomizeModifier();
                        break;
                    case ModifierType.Rotate:
                        newElement.managedReferenceValue = new SSRotateModifier();
                        break;
                    case ModifierType.RotateTowardsVector:
                        newElement.managedReferenceValue = new SSRotateTowardsVectorModifier();
                        break;
                }
                _modifierType = ModifierType.AddModifier;
            }
            
            EditorGUI.indentLevel++;
            for (int i = 0; i < _modifiers.arraySize; i++)
            {
                SerializedProperty element = _modifiers.GetArrayElementAtIndex(i);
                
                Rect elementRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(element)), position.width - 20, EditorGUI.GetPropertyHeight(element));
                EditorGUI.PropertyField(elementRect, element);
                
                Rect removeButtonRect = new Rect(elementRect.xMax, elementRect.y, 20, elementRect.height);
                if (GUI.Button(removeButtonRect, SSGUIStyle.GetDeleteIconText()))
                {
                    _modifiers.DeleteArrayElementAtIndex(i);
                }
            }
            
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 22;
            
            SetProperties(property);
            
            height += EditorGUI.GetPropertyHeight(_variableType);
            
            if (_variableType.intValue == (int)SSVariableType.ByReference) height += EditorGUI.GetPropertyHeight(_variableReference);
            else if (_variableType.intValue == (int)SSVariableType.ByValue) height += EditorGUI.GetPropertyHeight(_variableValue);
            else if (_variableType.intValue == (int)SSVariableType.ByDictionaryKeyName) height += EditorGUI.GetPropertyHeight(_dictionaryName);
            
            height += EditorGUI.GetPropertyHeight(_modifiers);
            height += GetPropertyArrayHeight(_modifiers);
            
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _variableType = property.FindPropertyRelative("variableType");
            
            _variableValue = property.FindPropertyRelative("variableValue");
            _variableReference = property.FindPropertyRelative("variableReference");
            _dictionaryName = property.FindPropertyRelative("dictionaryName");
            
            _modifiers = property.FindPropertyRelative("modifiers");
        }
    }
}