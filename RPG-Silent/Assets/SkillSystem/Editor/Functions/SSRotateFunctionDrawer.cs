using SkillSystem.Editor.Core;
using SkillSystem.Functions;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Functions
{
    [CustomPropertyDrawer(typeof(SSRotateFunction))]
    public class SSRotateFunctionDrawer : SSPropertyDrawer
    {
        SerializedProperty _rotateType;
        
        SerializedProperty _targetTransform;
        SerializedProperty _targetRigidbody;
        SerializedProperty _targetRigidbody2D;
        
        SerializedProperty _rotation;
        SerializedProperty _vector;
        SerializedProperty _value;
        
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Function white rect when foldout
            Rect foldoutWhiteRect = new Rect(position.x - 17 ,position.y - EditorGUIUtility.singleLineHeight - 1,
                2, GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(foldoutWhiteRect, SSGUIStyle.GetFunctionFoldoutColor());
            
            Space(10);
            Rect rotateTypeLabelRect = new Rect(position.x, GetSetCurrentLineHeight(0), position.width, EditorGUI.GetPropertyHeight(_rotateType));
            EditorGUI.LabelField(rotateTypeLabelRect, "Rotate Type", SSGUIStyle.GetLabelGUIStyle());
            EditorGUI.indentLevel++;
            Rect rotateTypeRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_rotateType)), position.width, EditorGUI.GetPropertyHeight(_rotateType));
            EditorGUI.PropertyField(rotateTypeRect, _rotateType, new GUIContent(" "));
            EditorGUI.indentLevel--;

            if (_rotateType.intValue == (int)SSRotateFunction.RotateType.SetRotation || _rotateType.intValue == (int)SSRotateFunction.RotateType.AddRotation)
            {
                Space(10);
                DrawProperty(position, _targetTransform, "Target Transform");
            
                Space(10);
                DrawProperty(position, _rotation, "Rotation");
            }
            else if (_rotateType.intValue == (int)SSRotateFunction.RotateType.SetTorque ||
                     _rotateType.intValue == (int)SSRotateFunction.RotateType.AddTorque)
            {
                Space(10);
                DrawProperty(position, _targetRigidbody, "Target Rigidbody");
            
                Space(10);
                DrawProperty(position, _vector, "Vector");
            }
            else if (_rotateType.intValue == (int)SSRotateFunction.RotateType.SetTorque2D ||
                     _rotateType.intValue == (int)SSRotateFunction.RotateType.AddTorque2D)
            {
                Space(10);
                DrawProperty(position, _targetRigidbody2D, "Target Rigidbody2D");
            
                Space(10);
                DrawProperty(position, _value, "Value");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            if (property.isExpanded)
            {
                height = 76;
                
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_rotateType);

                if (_rotateType.intValue == (int)SSRotateFunction.RotateType.SetRotation ||
                    _rotateType.intValue == (int)SSRotateFunction.RotateType.AddRotation)
                {
                    height += EditorGUI.GetPropertyHeight(_targetTransform);
                    height += EditorGUI.GetPropertyHeight(_rotation);
                }
                
                else if (_rotateType.intValue == (int)SSRotateFunction.RotateType.SetTorque ||
                         _rotateType.intValue == (int)SSRotateFunction.RotateType.AddTorque)
                {
                    height += EditorGUI.GetPropertyHeight(_targetRigidbody);
                    height += EditorGUI.GetPropertyHeight(_vector);
                }
                
                else if (_rotateType.intValue == (int)SSRotateFunction.RotateType.SetTorque2D ||
                         _rotateType.intValue == (int)SSRotateFunction.RotateType.AddTorque2D)
                {
                    height += EditorGUI.GetPropertyHeight(_targetRigidbody2D);
                    height += EditorGUI.GetPropertyHeight(_value);
                }
            }
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _rotateType = property.FindPropertyRelative("rotateType");
            _targetTransform = property.FindPropertyRelative("targetTransform");
            _targetRigidbody = property.FindPropertyRelative("targetRigidbody");
            _targetRigidbody2D = property.FindPropertyRelative("targetRigidbody2D");
            _rotation = property.FindPropertyRelative("rotation");
            _vector = property.FindPropertyRelative("vector");
            _value = property.FindPropertyRelative("value");
        }
    }
}