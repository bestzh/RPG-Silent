using SkillSystem.Editor.Core;
using SkillSystem.Functions;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Functions
{
    [CustomPropertyDrawer(typeof(SSSoundFXFunction))]
    public class SSSoundFXFunctionDrawer : SSPropertyDrawer
    {
        SerializedProperty _sfxType;
        SerializedProperty _audioSource;
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Function white rect when foldout
            Rect foldoutWhiteRect = new Rect(position.x - 17 ,position.y - EditorGUIUtility.singleLineHeight - 1,
                2, GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(foldoutWhiteRect, SSGUIStyle.GetFunctionFoldoutColor());
            
            Space(10);
            Rect moveTypeLabelRect = new Rect(position.x, GetSetCurrentLineHeight(0), position.width, EditorGUI.GetPropertyHeight(_sfxType));
            EditorGUI.LabelField(moveTypeLabelRect, "SFX Type", SSGUIStyle.GetLabelGUIStyle());
            EditorGUI.indentLevel++;
            Rect moveTypeRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_sfxType)), position.width, EditorGUI.GetPropertyHeight(_sfxType));
            EditorGUI.PropertyField(moveTypeRect, _sfxType, new GUIContent(" "));
            EditorGUI.indentLevel--;
            
            Space(10);
            DrawProperty(position, _audioSource, "Audio Source");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            if (property.isExpanded)
            {
                height = 48;
                
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_sfxType);
                height += EditorGUI.GetPropertyHeight(_audioSource);
            }
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _sfxType = property.FindPropertyRelative("sfxType");
            _audioSource = property.FindPropertyRelative("audioSource");
        }
    }
}