using SkillSystem.Editor.Core;
using SkillSystem.Functions;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Functions
{
    [CustomPropertyDrawer(typeof(SSVisualFXFunction))]
    public class SSVisualFXFunctionDrawer : SSPropertyDrawer
    {
        SerializedProperty _vfxType;
        SerializedProperty _particleSystem;
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Function white rect when foldout
            Rect foldoutWhiteRect = new Rect(position.x - 17 ,position.y - EditorGUIUtility.singleLineHeight - 1,
                2, GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(foldoutWhiteRect, SSGUIStyle.GetFunctionFoldoutColor());
            
            Space(10);
            Rect moveTypeLabelRect = new Rect(position.x, GetSetCurrentLineHeight(0), position.width, EditorGUI.GetPropertyHeight(_vfxType));
            EditorGUI.LabelField(moveTypeLabelRect, "VFX Type", SSGUIStyle.GetLabelGUIStyle());
            EditorGUI.indentLevel++;
            Rect moveTypeRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_vfxType)), position.width, EditorGUI.GetPropertyHeight(_vfxType));
            EditorGUI.PropertyField(moveTypeRect, _vfxType, new GUIContent(" "));
            EditorGUI.indentLevel--;
            
            Space(10);
            DrawProperty(position, _particleSystem, "Particle System");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            if (property.isExpanded)
            {
                height = 48;
                
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_vfxType);
                height += EditorGUI.GetPropertyHeight(_particleSystem);
            }
            return height;
        }

        protected override void SetProperties(SerializedProperty property)
        {
            _vfxType = property.FindPropertyRelative("vfxType");
            _particleSystem = property.FindPropertyRelative("particleSystem");
        }
    }
}