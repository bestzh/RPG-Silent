using SkillSystem.Editor.Core;
using SkillSystem.Functions;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Functions
{
    [CustomPropertyDrawer(typeof(SSInstantiateFunction))]
    public class SSInstantiateFunctionDrawer : SSPropertyDrawer
    {
        SerializedProperty _objectToInstantiate;
        
        SerializedProperty _parentTransform;
        
        SerializedProperty _position;
        
        SerializedProperty _rotation;
        
        SerializedProperty _saveObjectToDictionary;
        
        SerializedProperty _dictionaryKeyName;
        
        protected override void DrawAllProperties(Rect position, SerializedProperty property, GUIContent label)
        {
            //Function white rect when foldout
            Rect foldoutWhiteRect = new Rect(position.x - 17 ,position.y - EditorGUIUtility.singleLineHeight - 1,
                2, GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(foldoutWhiteRect, SSGUIStyle.GetFunctionFoldoutColor());
            
            Space(10);
            DrawProperty(position, _objectToInstantiate, "Object To Instantiate");
            
            Space(10);
            DrawProperty(position, _parentTransform, "Parent Transform");
            
            Space(10);
            DrawProperty(position, _position, "Position");
            
            Space(10);
            DrawProperty(position, _rotation, "Rotation");
            
            Space(10);
            Rect saveObjectToDictionaryLabelRect = new Rect(position.x, GetSetCurrentLineHeight(0), position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(saveObjectToDictionaryLabelRect, "Save Object To Dictionary", SSGUIStyle.GetLabelGUIStyle());
            Rect saveObjectToDictionaryRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_saveObjectToDictionary)), position.width, EditorGUI.GetPropertyHeight(_saveObjectToDictionary));
            EditorGUI.PropertyField(saveObjectToDictionaryRect, _saveObjectToDictionary, new GUIContent(" "));

            if (_saveObjectToDictionary.boolValue)
            {
                EditorGUI.indentLevel++;
                
                //Variable space indicatorRect
                Rect spaceIndicatorRect = new Rect(position.x - 14 + EditorGUI.indentLevel * 15, GetSetCurrentLineHeight(0),
                    2, EditorGUI.GetPropertyHeight(_dictionaryKeyName));
                EditorGUI.DrawRect(spaceIndicatorRect, SSGUIStyle.GetSpaceIndicatorColor());
                
                Rect dictionaryKeyNameRect = new Rect(position.x, GetSetCurrentLineHeight(EditorGUI.GetPropertyHeight(_dictionaryKeyName)), position.width, EditorGUI.GetPropertyHeight(_dictionaryKeyName));
                EditorGUI.PropertyField(dictionaryKeyNameRect, _dictionaryKeyName, new GUIContent("Dictionary Key Name"));
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            if (property.isExpanded)
            {
                height = 132;
                SetProperties(property);
                
                height += EditorGUI.GetPropertyHeight(_objectToInstantiate);
                height += EditorGUI.GetPropertyHeight(_parentTransform);
                height += EditorGUI.GetPropertyHeight(_position);
                height += EditorGUI.GetPropertyHeight(_rotation);
                height += EditorGUI.GetPropertyHeight(_saveObjectToDictionary);
                if (_saveObjectToDictionary.boolValue) height += EditorGUI.GetPropertyHeight(_dictionaryKeyName);
                
            }
            return height;
        }
        

        protected override void SetProperties(SerializedProperty property)
        {
            _objectToInstantiate = property.FindPropertyRelative("objectToInstantiate");
            _parentTransform = property.FindPropertyRelative("parentTransform");
            _position = property.FindPropertyRelative("position");
            _rotation = property.FindPropertyRelative("rotation");
            _saveObjectToDictionary = property.FindPropertyRelative("saveObjectToDictionary");
            _dictionaryKeyName = property.FindPropertyRelative("dictionaryKeyName");
        }
    }
}
