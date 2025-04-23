using SkillSystem.Editor.Core;
using SkillSystem.Functions;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Functions
{
    [CustomPropertyDrawer(typeof(SSAnimationFunction))]
    public class SSAnimationFunctionDrawer : SSPropertyDrawer
    {
        SerializedProperty _targetAnimator;

        SerializedProperty _animateType;
        SerializedProperty _parameterName;

        SerializedProperty _crossFadeTime;
        SerializedProperty _triggerValue;
        SerializedProperty _boolValue;
        SerializedProperty _integerValue;
        SerializedProperty _floatValue;
        SerializedProperty _speedValue;
        
        
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Function white rect when foldout
            Rect foldoutWhiteRect = new Rect(position.x - 17 ,position.y - EditorGUIUtility.singleLineHeight - 1,
                2, GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(foldoutWhiteRect, SSGUIStyle.GetFunctionFoldoutColor());
            
            Space(10);
            Rect animateTypeLabelRect = new Rect(position.x, GetSetCurrentLineHeight(0), position.width, EditorGUI.GetPropertyHeight(_animateType));
            EditorGUI.LabelField(animateTypeLabelRect, "Animate Type", SSGUIStyle.GetLabelGUIStyle());
            EditorGUI.indentLevel++;
            Rect animateTypeRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_animateType)), position.width, EditorGUI.GetPropertyHeight(_animateType));
            EditorGUI.PropertyField(animateTypeRect, _animateType, new GUIContent(" "));
            EditorGUI.indentLevel--;
            
            Space(10);
            DrawProperty(position, _targetAnimator, "Target Animator");
            
            string paraName = "Parameter Name";
            if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.Play) paraName = "Animation Name";
            if (_animateType.intValue != (int)SSAnimationFunction.AnimateType.SetSpeed)
            {
                Space(10);
                DrawProperty(position, _parameterName, paraName);
            }

            Space(10);
            if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.Play)
                DrawProperty(position, _crossFadeTime, "Cross Fade Duration");
            
            else if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.SetTrigger)
                DrawProperty(position, _triggerValue, "Trigger Value");
            
            else if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.SetBool)
                DrawProperty(position, _boolValue, "Bool Value");
            
            else if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.SetInteger)
                DrawProperty(position, _integerValue, "Integer Value");
            
            else if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.SetFloat)
                DrawProperty(position, _floatValue, "Float Value");
            
            else if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.SetSpeed)
                DrawProperty(position, _speedValue, "Speed");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            if (property.isExpanded)
            {
                height = 76;
                
                SetProperties(property);

                height += EditorGUI.GetPropertyHeight(_targetAnimator);
                height += EditorGUI.GetPropertyHeight(_animateType);
                if (_animateType.intValue != (int)SSAnimationFunction.AnimateType.SetSpeed)
                {
                    height += 28;
                    height += EditorGUI.GetPropertyHeight(_parameterName);
                }
                
                if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.Play)
                    height += EditorGUI.GetPropertyHeight(_crossFadeTime);

                else if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.SetTrigger)
                    height += EditorGUI.GetPropertyHeight(_triggerValue);

                else if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.SetBool)
                    height += EditorGUI.GetPropertyHeight(_boolValue);

                else if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.SetInteger)
                    height += EditorGUI.GetPropertyHeight(_integerValue);

                else if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.SetFloat)
                    height += EditorGUI.GetPropertyHeight(_floatValue);
                
                else if (_animateType.intValue == (int)SSAnimationFunction.AnimateType.SetSpeed)
                    height += EditorGUI.GetPropertyHeight(_speedValue);
            }
            return height;
        }
        
        protected override void SetProperties(SerializedProperty property)
        {
            _targetAnimator = property.FindPropertyRelative("targetAnimator");
            _animateType = property.FindPropertyRelative("animateType");
            _parameterName = property.FindPropertyRelative("parameterName");
            _crossFadeTime = property.FindPropertyRelative("crossFadeTime");
            _triggerValue = property.FindPropertyRelative("triggerValue");
            _boolValue = property.FindPropertyRelative("boolValue");
            _integerValue = property.FindPropertyRelative("integerValue");
            _floatValue = property.FindPropertyRelative("floatValue");
            _speedValue = property.FindPropertyRelative("speedValue");
        }
    }
}
