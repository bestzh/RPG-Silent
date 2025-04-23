using System;
using System.Collections.Generic;
using SkillSystem.Core;
using SkillSystem.Functions;
using UnityEditor;
using UnityEngine;

namespace SkillSystem.Editor.Core
{
    [CustomEditor(typeof(SSSkill))]
    public class SSSkillEditor : UnityEditor.Editor
    {
        private enum FunctionType { AddFunction, Animation, Instantiate, Destroy, Move, Rotate, Skill, Event, VFX, SFX }
        
        private SSSkill _skill;
        
        private SerializedObject _so;

        private SerializedProperty _type;

        private SerializedProperty _skillName;
        private SerializedProperty _autoPlayOnStart;
        private SerializedProperty _playOnRealtime;
        private SerializedProperty _skillSpeed;
        private SerializedProperty _skillCooldown;
        private SerializedProperty _maxCharge;
        private SerializedProperty _saveToDictionary;
        private SerializedProperty _dictionaryKeyName;

        private SerializedProperty _actions;

        private bool _isExpanded;
        private bool _isDragging;
        private bool _isDraggingFunction;

        private int _scrollBarWidth;
        
        private void OnEnable()
        {
            _skill = (SSSkill)target;
            _so = serializedObject;

            _type = _so.FindProperty("type");
            
            _skillName = _so.FindProperty("skillName");
            _autoPlayOnStart = _so.FindProperty("autoPlayOnStart");
            _playOnRealtime = _so.FindProperty("playOnRealtime");
            _skillSpeed = _so.FindProperty("skillSpeed");
            _skillCooldown = _so.FindProperty("skillCooldown");
            _maxCharge = _so.FindProperty("maxCharge");
            _saveToDictionary = _so.FindProperty("saveToDictionary");
            _dictionaryKeyName = _so.FindProperty("dictionaryKeyName");
            
            _actions = _so.FindProperty("actions");
        }

        public override void OnInspectorGUI()
        {
            _so.Update();
            SetScrollBarWidth();
            ShowSkillProperties();
            DrawActionList();
            ShowDebugOptions();
            _so.ApplyModifiedProperties();
        }

        private void ShowSkillProperties()
        {
            using (new GUILayout.VerticalScope(EditorStyles.objectFieldThumb))
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical();
                EditorGUILayout.Space(3);
                _isExpanded = EditorGUILayout.Foldout(_isExpanded, " " + _skillName.stringValue, true, SSGUIStyle.GetFoldoutLabelGUIStyle());
                EditorGUILayout.EndVertical();
                
                
                //Limit Actions for Single Type
                if (_type.enumValueIndex == (int)SSSkill.SkillType.Single && _actions.arraySize > 1)
                {
                    while (_actions.arraySize > 1)
                    {
                        _actions.DeleteArrayElementAtIndex(_actions.arraySize - 1);
                    }
                    EditorGUILayout.HelpBox("Single type skills can only have one action.", MessageType.Warning);
                }
                
                //Add and clear action button
                if (GUILayout.Button("Add Action"))
                {
                    if (_type.enumValueIndex == (int)SSSkill.SkillType.Single && _actions.arraySize > 0)
                    {
                        EditorGUILayout.HelpBox("Single type skills can only have one action.", MessageType.Warning);
                    }
                    else
                    {
                        var action = new SSAction();
                        _skill.actions.Add(action);
                    }
                }
                
                
                if (GUILayout.Button("Clear All Actions",GUILayout.MaxWidth(120)) && EditorUtility.DisplayDialog("Clear All Actions", "Are you sure you want to clear all actions?", "Yes", "No"))
                {
                    _skill.actions.Clear();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);

                if (_isExpanded)
                {
                    EditorGUI.indentLevel++;
                    
                    //Skill properties color rect
                    Rect colorRect = GUILayoutUtility.GetLastRect();
                    colorRect.y -= 20;
                    colorRect.height = EditorGUIUtility.singleLineHeight * 10 + 4;
                    if (_saveToDictionary.boolValue) colorRect.height += EditorGUIUtility.singleLineHeight;
                    colorRect.width = 2;
                    EditorGUI.DrawRect(colorRect, Color.white);
                    
                    EditorGUILayout.PropertyField(_skillName, new GUIContent("Skill name"));
                    EditorGUILayout.PropertyField(_type, new GUIContent("Skill type"));
                    
                    EditorGUILayout.PropertyField(_autoPlayOnStart, new GUIContent("Autoplay on start"));
                    
                    EditorGUILayout.PropertyField(_playOnRealtime, new GUIContent("Play on realtime"));
                    
                    EditorGUILayout.PropertyField(_skillSpeed, new GUIContent("Skill speed"));
                    
                    EditorGUILayout.PropertyField(_skillCooldown, new GUIContent("Skill cooldown"));
                    
                    EditorGUILayout.PropertyField(_maxCharge, new GUIContent("Max skill charge"));
                    
                    EditorGUILayout.PropertyField(_saveToDictionary, new GUIContent("Save skill to Dictionary"));
                    
                    if (_saveToDictionary.boolValue)
                    {
                        EditorGUILayout.PropertyField(_dictionaryKeyName, new GUIContent("Dictionary key name"));
                    }
                    
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawActionList()
        {
            if (_skill.actions.Count == 0) return;
            
            Event evt = Event.current;
            
            for (int i = 0; i < _actions.arraySize; i++)
            {
                using (new GUILayout.VerticalScope(EditorStyles.selectionRect))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.indentLevel++;
                    SerializedProperty currentAction = _actions.GetArrayElementAtIndex(i);
                    
                    Rect rect = GUILayoutUtility.GetRect(20, 20, GUILayout.ExpandWidth(false));
                    Rect dragRect = new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height);

                    GUIStyle centeredStyle = new GUIStyle(EditorStyles.iconButton);
                    centeredStyle.alignment = TextAnchor.MiddleCenter;
                    
                    GUI.Box(dragRect, "☰", centeredStyle);
                    HandleDragAndDrop(i, dragRect, evt);
                    
                    SerializedProperty actionName = currentAction.FindPropertyRelative("actionName");
                    Rect rect2 = new Rect(rect.x + 20, rect.y + 1, EditorGUIUtility.currentViewWidth - 100, EditorGUIUtility.singleLineHeight);
                    
                    currentAction.isExpanded = EditorGUI.Foldout(rect2,currentAction.isExpanded, actionName.stringValue, true, SSGUIStyle.GetFoldoutLabelGUIStyle());
                    
                    SerializedProperty actionDuration = currentAction.FindPropertyRelative("actionDuration");

                    float duration = 0f;
                    SerializedProperty functions = currentAction.FindPropertyRelative("functions");
                    for (int j = 0; j < functions.arraySize; j++)
                    {
                        duration += functions.GetArrayElementAtIndex(j).FindPropertyRelative("delay").floatValue;
                    }
                    
                    GUIStyle rightAlignedStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
                    
                    Rect rect3 = new Rect(EditorGUIUtility.currentViewWidth - 260 - _scrollBarWidth, rect.y + 1, 200, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(rect3, $"Duration {duration}s +", rightAlignedStyle);
                    Rect rect4 = rect3;
                    rect4.width = 62;
                    rect4.x += 190;
                    float dur = EditorGUI.FloatField(rect4, actionDuration.floatValue);
                    actionDuration.floatValue = Mathf.Clamp(dur, 0f, float.MaxValue);
                     
                    EditorGUILayout.EndHorizontal();

                    if (currentAction.isExpanded)
                    {
                        EditorGUILayout.Space(5);
                        
                        SSAction action = _skill.actions[i];
                        
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Action Name", GUILayout.MinWidth(150));
                        actionName.stringValue = EditorGUILayout.TextField(actionName.stringValue,GUILayout.MaxWidth(300));
                        EditorGUILayout.EndHorizontal();
                        
                        // Chain
                        if (_type.enumValueIndex == (int)SSSkill.SkillType.Chain && i < _actions.arraySize - 1)
                        {
                            
                            SerializedProperty resetActionIndexDelay = currentAction.FindPropertyRelative("resetActionIndexDelay");
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Reset Action Index Delay", GUILayout.MinWidth(150));
                            float delay = EditorGUILayout.FloatField(resetActionIndexDelay.floatValue,GUILayout.MaxWidth(300));
                            resetActionIndexDelay.floatValue = Mathf.Clamp(delay, 0f, float.MaxValue);
                            EditorGUILayout.EndHorizontal();
                        }
                        
                        // Draw Functions
                        if (functions is { arraySize: > 0 })
                        {
                            DrawFunctionList(i, functions, action.functions);
                        }

                        EditorGUI.indentLevel--;
                        // Add Function button
                        EditorGUILayout.BeginHorizontal();
                        FunctionType functionType = FunctionType.AddFunction;
                        functionType = (FunctionType)EditorGUILayout.EnumPopup(functionType);
                        if (functionType != FunctionType.AddFunction)
                        {
                            switch (functionType)
                            {
                                case FunctionType.Animation:
                                    action.functions.Add(new SSAnimationFunction());
                                    break;
                                case FunctionType.Instantiate:
                                    action.functions.Add(new SSInstantiateFunction());
                                    break;
                                case FunctionType.Destroy:
                                    action.functions.Add(new SSDestroyFunction());
                                    break;
                                case FunctionType.Move:
                                    action.functions.Add(new SSMoveFunction());
                                    break;
                                case FunctionType.Rotate:
                                    action.functions.Add(new SSRotateFunction());
                                    break;
                                case FunctionType.Skill:
                                    action.functions.Add(new SSSkillFunction());
                                    break;
                                case FunctionType.Event:
                                    action.functions.Add(new SSEventFunction());
                                    break;
                                case FunctionType.VFX:
                                    action.functions.Add(new SSVisualFXFunction());
                                    break;
                                case FunctionType.SFX:
                                    action.functions.Add(new SSSoundFXFunction());
                                    break;
                            }
                        }
                        
                        // Remove Action button
                        if (GUILayout.Button("Remove Action"))
                        {
                            _actions.DeleteArrayElementAtIndex(i);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else EditorGUI.indentLevel--;
                }
            }
        }
        
        private void DrawFunctionList(int actionIndex, SerializedProperty functions, List<SSFunction> functionList)
        {
            Event evt = Event.current;
            
            for (int i = 0; i < functions.arraySize; i++)
            {
                using (new GUILayout.VerticalScope(SSGUIStyle.GetFunctionVerticalScopeGUIStyle()))
                {
                    EditorGUILayout.BeginHorizontal();
                    SerializedProperty currentFunction = functions.GetArrayElementAtIndex(i);
                    
                    Rect rect = GUILayoutUtility.GetRect(20, 20, GUILayout.ExpandWidth(false));
                    Rect dragRect = new Rect(rect.x + 12, rect.y + 2, rect.width, rect.height);

                    GUIStyle dragStyle = new GUIStyle(EditorStyles.iconButton);
                    dragStyle.alignment = TextAnchor.MiddleCenter;
                    GUI.Box(dragRect, "☰", dragStyle);

                    HandleFunctionDragAndDrop(actionIndex, functions, i, dragRect, evt);

                    DrawFunction(currentFunction, functionList[i], functionList);
                    
                    Rect buttonRect = GUILayoutUtility.GetRect(20, 20, GUILayout.ExpandWidth(false));
                    buttonRect.height += EditorGUI.GetPropertyHeight(functions.GetArrayElementAtIndex(i));
                    if (GUI.Button(buttonRect, SSGUIStyle.GetDeleteIconText()))
                    {
                        functions.DeleteArrayElementAtIndex(i);
                        functionList.RemoveAt(i);
                        _so.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void DrawFunction(SerializedProperty functionProperty, SSFunction function, List<SSFunction> functionList)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel++;
            string functionName = function.GetType().Name;
            functionName = functionName.Substring(2);
            Color functionColor = SSGUIStyle.GetFunctionColor(functionName);
            
            //Function foldout rect
            Rect foldoutRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            foldoutRect.width = EditorGUIUtility.labelWidth * 2.23f - 80;
            functionProperty.isExpanded = EditorGUI.Foldout(foldoutRect, functionProperty.isExpanded, functionName, true);
            EditorGUI.indentLevel--;
            
            // Right click menu
            if (Event.current.type == EventType.ContextClick && foldoutRect.Contains(Event.current.mousePosition))
            {
                HandleFunctionContextMenu(function, functionList);
            }
            
            //Function color rect
            Rect functionColorRect = GUILayoutUtility.GetLastRect();
            functionColorRect.x += EditorGUI.indentLevel - 25;
            functionColorRect.y -= 2f;
            functionColorRect.height = EditorGUI.GetPropertyHeight(functionProperty, true) + EditorGUIUtility.singleLineHeight + 6;
            functionColorRect.width = 4;
            EditorGUI.DrawRect(functionColorRect, functionColor);
            
            //Function delay
            Rect delayRect = GUILayoutUtility.GetLastRect();
            delayRect.height = EditorGUIUtility.singleLineHeight;
            delayRect.width = EditorGUIUtility.fieldWidth;
            delayRect.x += EditorGUIUtility.currentViewWidth - delayRect.width - 79 - _scrollBarWidth;
            
            Rect delayRect2 = delayRect;
            delayRect2.x -= delayRect.width - 10;
            
            float delay = EditorGUI.FloatField(delayRect ,functionProperty.FindPropertyRelative("delay").floatValue);
            functionProperty.FindPropertyRelative("delay").floatValue = Mathf.Clamp(delay, 0f, float.MaxValue);
            
            EditorGUI.LabelField(delayRect2, new GUIContent("Delay"));
            
            EditorGUILayout.EndHorizontal();
            if (functionProperty.isExpanded)
            {
                EditorGUILayout.PropertyField(functionProperty, true);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ShowDebugOptions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Next Action"))
            {
                if (!Application.isPlaying) return;
                _skill.PlayNextAction();
            }
            if (GUILayout.Button("Stop Action"))
            {
                if (!Application.isPlaying) return;
                _skill.StopAction();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void HandleDragAndDrop(int index, Rect itemRect, Event evt)
        {
            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (itemRect.Contains(evt.mousePosition))
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.SetGenericData("DraggedItem", index);
                        DragAndDrop.StartDrag("Dragging item");
                        _isDragging = true;
                        evt.Use();
                    }
                    break;

                case EventType.DragUpdated:
                    if (itemRect.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    }
                    break;

                case EventType.DragPerform:
                    if (itemRect.Contains(evt.mousePosition) && _isDragging)
                    {
                        DragAndDrop.AcceptDrag();
                        int targetIndex = index;
                        int fromIndex = (int)DragAndDrop.GetGenericData("DraggedItem");

                        if (fromIndex != targetIndex)
                        {
                            if (fromIndex == targetIndex || fromIndex < 0 || targetIndex < 0 || fromIndex >= _actions.arraySize || targetIndex >= _actions.arraySize)
                                return;

                            _actions.MoveArrayElement(fromIndex, targetIndex);
            
                            (_skill.actions[fromIndex], _skill.actions[targetIndex]) = (_skill.actions[targetIndex], _skill.actions[fromIndex]);
                            
                            _actions.GetArrayElementAtIndex(fromIndex).isExpanded = false;
                            _actions.GetArrayElementAtIndex(targetIndex).isExpanded = false;
                        }

                        _isDragging = false;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    _isDragging = false;
                    break;
            }
        }
        
        private void HandleFunctionDragAndDrop(int actionIndex, SerializedProperty functions, int functionIndex, Rect itemRect, Event evt)
        {
            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (itemRect.Contains(evt.mousePosition))
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.SetGenericData("DraggedFunctionIndex", functionIndex);
                        DragAndDrop.SetGenericData("DraggedActionIndex", actionIndex);
                        DragAndDrop.StartDrag("Dragging function");
                        _isDraggingFunction = true;
                        evt.Use();
                    }
                    break;

                case EventType.DragUpdated:
                    if (itemRect.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    }
                    break;

                case EventType.DragPerform:
                    if (itemRect.Contains(evt.mousePosition) && _isDraggingFunction)
                    {
                        int draggedFunctionIndex = (int)DragAndDrop.GetGenericData("DraggedFunctionIndex");
                        int draggedActionIndex = (int)DragAndDrop.GetGenericData("DraggedActionIndex");
                        
                        if (draggedActionIndex == actionIndex && draggedFunctionIndex != functionIndex)
                        {
                            functions.MoveArrayElement(draggedFunctionIndex, functionIndex);
                            functions.GetArrayElementAtIndex(draggedFunctionIndex).isExpanded = false;
                            functions.GetArrayElementAtIndex(functionIndex).isExpanded = false;
                        }

                        _isDraggingFunction = false;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    _isDraggingFunction = false;
                    break;
            }
        }
        
        private void HandleFunctionContextMenu(SSFunction function, List<SSFunction> functionList)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Copy"), false, () => CopyFunction(function));
            
            if (SSDictionary.CopyFunction != null)
            {
                menu.AddItem(new GUIContent("Paste"), false, () => PasteFunction(function, functionList));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }
            
            menu.AddItem(new GUIContent("Duplicate"), false, () =>  DuplicateFunction(function, functionList));
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Reset"), false, () => ResetFunction(function, functionList));
            
            menu.AddItem(new GUIContent("Remove"), false, () => RemoveFunction(function, functionList));
            
            
            menu.ShowAsContext();
        }
        
        private void CopyFunction(SSFunction function)
        {
            SSDictionary.CopyFunction = function;
        }

        private void PasteFunction(SSFunction function, List<SSFunction> functionList)
        {
            if (SSDictionary.CopyFunction != null)
            {
                int index = functionList.IndexOf(function);
                
                Type type = SSDictionary.CopyFunction.GetType();
                
                string json = JsonUtility.ToJson(SSDictionary.CopyFunction);
                object copy = JsonUtility.FromJson(json, type);
                
                SSFunction copyFunction = copy as SSFunction;
                functionList.Insert(index + 1, copyFunction);
            }
        }

        private void DuplicateFunction(SSFunction function, List<SSFunction> functionList)
        {
            if (function != null)
            {
                int index = functionList.IndexOf(function);
                
                Type type = function.GetType();
                
                string json = JsonUtility.ToJson(function);
                object copy = JsonUtility.FromJson(json, type);
                
                SSFunction copyFunction = copy as SSFunction;
                functionList.Insert(index + 1, copyFunction);
            }
        }

        private void ResetFunction(SSFunction function, List<SSFunction> functionList)
        {
            int index = functionList.IndexOf(function);
            Type type = function.GetType();
            SSFunction resetFunction = (SSFunction)Activator.CreateInstance(type);
            
            functionList[index] = resetFunction;
        }

        private void RemoveFunction(SSFunction function, List<SSFunction> functionList)
        {
            functionList.Remove(function);
        }
        
        private void SetScrollBarWidth()
        {
            float contentWidth = GUILayoutUtility.GetRect(1,1, GUILayout.ExpandWidth(true)).xMax;
            float viewWidth = EditorGUIUtility.currentViewWidth;
            
            if (10 > viewWidth - contentWidth) _scrollBarWidth = 0;
            else _scrollBarWidth = 13;
        }
    }
}
