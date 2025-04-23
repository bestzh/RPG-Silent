using SkillSystem.Editor.Core;
using SkillSystem.Functions;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Functions
{
    [CustomPropertyDrawer(typeof(SSMoveFunction))]
    public class SSMoveFunctionDrawer : SSPropertyDrawer
    { 
        SerializedProperty _moveType;
        SerializedProperty _targetTransform;
        SerializedProperty _targetRigidbody;
        SerializedProperty _targetRigidbody2D;
        
        SerializedProperty _vector;
        
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Function white rect when foldout
            Rect foldoutWhiteRect = new Rect(position.x - 17 ,position.y - EditorGUIUtility.singleLineHeight - 1,
                2, GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(foldoutWhiteRect, SSGUIStyle.GetFunctionFoldoutColor());
            
            Space(10);
            Rect moveTypeLabelRect = new Rect(position.x, GetSetCurrentLineHeight(0), position.width, EditorGUI.GetPropertyHeight(_moveType));
            EditorGUI.LabelField(moveTypeLabelRect, "Move Type", SSGUIStyle.GetLabelGUIStyle());
            EditorGUI.indentLevel++;
            Rect moveTypeRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_moveType)), position.width, EditorGUI.GetPropertyHeight(_moveType));
            EditorGUI.PropertyField(moveTypeRect, _moveType, new GUIContent(" "));
            EditorGUI.indentLevel--;
            
            Space(10);
            if (_moveType.intValue == (int)SSMoveFunction.MoveType.SetPosition || 
                _moveType.intValue == (int)SSMoveFunction.MoveType.AddPosition)
            {
                DrawProperty(position, _targetTransform, "Target Transform");
            }
            else if (_moveType.intValue == (int)SSMoveFunction.MoveType.SetVelocity ||
                     _moveType.intValue == (int)SSMoveFunction.MoveType.AddForce)
            {
                DrawProperty(position, _targetRigidbody, "Target Rigidbody");
            }
            else if (_moveType.intValue == (int)SSMoveFunction.MoveType.SetVelocity2D ||
                     _moveType.intValue == (int)SSMoveFunction.MoveType.AddForce2D)
            {
                DrawProperty(position, _targetRigidbody2D, "Target Rigidbody2D");
            }
            
            
            Space(10);
            DrawProperty(position, _vector, "Vector");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            if (property.isExpanded)
            {
                height = 76;
                
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_moveType);
                
                if (_moveType.intValue == (int)SSMoveFunction.MoveType.SetPosition ||
                    _moveType.intValue == (int)SSMoveFunction.MoveType.AddPosition)
                    height += EditorGUI.GetPropertyHeight(_targetTransform);
                else if (_moveType.intValue == (int)SSMoveFunction.MoveType.SetVelocity ||
                         _moveType.intValue == (int)SSMoveFunction.MoveType.AddForce)
                    height += EditorGUI.GetPropertyHeight(_targetRigidbody);
                else if (_moveType.intValue == (int)SSMoveFunction.MoveType.SetVelocity2D ||
                         _moveType.intValue == (int)SSMoveFunction.MoveType.AddForce2D)
                    height += EditorGUI.GetPropertyHeight(_targetRigidbody2D);

                height += EditorGUI.GetPropertyHeight(_vector);
            }
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _moveType = property.FindPropertyRelative("moveType");
            _targetTransform = property.FindPropertyRelative("targetTransform");
            _targetRigidbody = property.FindPropertyRelative("targetRigidbody");
            _targetRigidbody2D = property.FindPropertyRelative("targetRigidbody2D");
            _vector = property.FindPropertyRelative("vector");
        }
    }
}