using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Core
{
    public static class SSGUIStyle
    {
        public static GUIStyle GetLabelGUIStyle()
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.white }
            };

            return labelStyle;
        }

        public static GUIStyle GetFoldoutLabelGUIStyle()
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.foldout)
            {
                normal = { textColor = Color.white },
                fontStyle = EditorStyles.label.fontStyle
            };
            
            return labelStyle;
        }

        public static GUIStyle GetFunctionVerticalScopeGUIStyle()
        {
            GUIStyle customStyle = new GUIStyle(EditorStyles.objectFieldThumb);
            return customStyle;
        }
        


        public static Color GetSpaceIndicatorColor()
        {
            return Color.black;
        }
        
        public static Color GetFunctionFoldoutColor()
        {
            return Color.white;
        }

        public static string GetDeleteIconText()
        {
            return "✖";
        }
        
        public static Color GetFunctionColor(string functionName)
        {
            switch (functionName)
            {
                case "AnimationFunction": return new Color(0.5f, 0.9f, 0.3f);
                case "InstantiateFunction": return new Color(0f, 0.7f, 0.8f);
                case "DestroyFunction": return new Color(1f, 0.3f, 0.1f);
                case "MoveFunction": return new Color(0.2f, 0.8f, 0.6f);
                case "RotateFunction": return new Color(1f, 0.9f, 0.3f);
                case "SkillFunction": return new Color(0.3f, 0.3f, 0.7f);
                case "EventFunction": return new Color(0.6f, 0.4f, 0.6f); 
                case "VisualFXFunction": return new Color(0.9f, 0.6f, 0f); 
                case "SoundFXFunction": return new Color(1f, 0.4f, 0.5f);
                default: return Color.white;
            }
        }
    }
}