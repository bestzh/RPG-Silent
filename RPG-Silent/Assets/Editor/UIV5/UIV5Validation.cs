#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class UIV5Authoring
{
    [MenuItem("RPG Silent/UI/V5/Serialize canvas and connect prefab")]
    public static string ConfigureCanvas()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Editor must be idle.");
        if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Preserve the current scene before editing the UI entry scene.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity",OpenSceneMode.Single);
        var canvas=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Canvas>(true)).First(x=>x.isRootCanvas);
        var scaler=canvas.GetComponent<CanvasScaler>()??canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;scaler.referencePixelsPerUnit=100;
        canvas.pixelPerfect=false;canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.Normal|AdditionalCanvasShaderChannels.Tangent;
        Directory.CreateDirectory("Assets/Prefabs/UI");
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvas.gameObject,"Assets/Prefabs/UI/CanvasRoot.prefab",InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "CanvasRoot.prefab connected in StartScene: ScaleWithScreenSize / 1920×1080 / Expand. No runtime adaptation script.";
    }

    [MenuItem("RPG Silent/UI/V5/Validate prefab bindings")]
    public static string ValidatePrefabs()
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Run the authoring validator in Edit Mode.");
        var errors=new List<string>();var evidence=new List<string>();
        foreach(var name in Names)
        {
            var path=Views+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var refs=root.GetComponent<UIViewReferences>();if(refs==null)throw new InvalidOperationException("Missing UIViewReferences");
                foreach(var entry in refs.Entries)
                    if(entry.Scope==null||entry.Target==null)errors.Add(name+": missing target for "+entry.Id);
                    else if(refs.Find(entry.Scope,entry.Id)!=entry.Target.transform)errors.Add(name+": wrong target for "+entry.Id);
                foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))if(text.font==null||text.font.name!="NotoSansSC-UI SDF")errors.Add(name+": missing UI font on "+text.name);
                foreach(var parent in root.GetComponentsInChildren<RectTransform>(true))
                {
                    var children=parent.Cast<Transform>().ToArray();
                    if(children.Select(t=>t.name).Distinct().Count()!=children.Length)errors.Add(name+": duplicate names under "+parent.name);
                }
                var flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
                Action<object,string> invoke=(owner,method)=>owner.GetType().GetMethod(method,flags)?.Invoke(owner,null);
                foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if(behaviour is SettingsUI||behaviour is ScreenPage||behaviour is SoundPage||behaviour is ControllerPage||behaviour is GamePage||behaviour is SkillSlotUI||behaviour is WeaponEquipmentUI)invoke(behaviour,"Awake");
                root.GetComponent<UIBase>().Init("UI/"+name);
                var hud=root.GetComponent<MainUI>();if(hud!=null)
                {
                    foreach(var method in new[]{"BuildQuestHud","BuildCombatHud","BuildTargetHud","BuildAdventureHud","BuildJourneyHud","BuildLootHud","BuildTutorialHud","BuildEquipmentEffectsHud"})invoke(hud,method);
                    foreach(var slot in hud.skillSlots)slot.Bind(null,"");
                }
                if(name=="InventoryUI")
                {
                    if(root.GetComponentsInChildren<UIEquipmentSlotView>(true).Length!=7)errors.Add("Inventory requires five armor and two weapon slots.");
                    if(root.GetComponentsInChildren<GridLayoutGroup>(true).All(g=>g.constraintCount!=6))errors.Add("Inventory bag grid must have six columns.");
                }
                foreach(var list in root.GetComponentsInChildren<UIListView>(true))
                    if(list.GetComponent<VerticalLayoutGroup>()==null||list.GetComponent<ContentSizeFitter>()==null)errors.Add(name+": list lacks native layout "+list.name);
                evidence.Add(name+": "+refs.Entries.Count+" bindings, "+root.GetComponentsInChildren<Button>(true).Length+" buttons, Init passed");
            }
            catch(Exception ex){errors.Add(name+": "+(ex.InnerException??ex).ToString());}
            finally{foreach(var preview in root.GetComponentsInChildren<CharacterModelPreview>(true))preview.Hide();PrefabUtility.UnloadPrefabContents(root);}
        }
        var canvas=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CanvasRoot.prefab");var scaler=canvas!=null?canvas.GetComponent<CanvasScaler>():null;
        if(scaler==null||scaler.uiScaleMode!=CanvasScaler.ScaleMode.ScaleWithScreenSize||scaler.referenceResolution!=new Vector2(1920,1080)||scaler.screenMatchMode!=CanvasScaler.ScreenMatchMode.Expand)errors.Add("CanvasRoot serialized scaling contract failed.");
        File.WriteAllLines("Docs/UIImplementation/2026-10-29-v5/prefab-validation.txt",evidence.Concat(errors));
        if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
        return string.Join("\n",evidence)+"\nCanvas contract passed.";
    }
}
#endif
