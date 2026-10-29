#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>One-time authoring and repeatable validation. Never runs in a player build.</summary>
public static partial class UIV5Authoring
{
    const string Views = "Assets/Resources_moved/UI/";
    const string Templates = "Assets/Prefabs/UI/Templates/";
    static readonly Color Paper = new(.93f,.94f,.90f), Muted = new(.65f,.74f,.71f), Gold = new(.85f,.75f,.53f);
    static readonly Color Surface = new(.09f,.18f,.21f,1f), LineColor = new(.56f,.67f,.58f,.28f);
    static TMP_FontAsset font;
    static UIListRowView rowPrefab;
    static readonly List<string> report = new();
    static readonly string[] Names = { "StartUI", "InventoryUI", "TownNpcUI", "QuestJournalUI", "PortalUI", "MainUI", "DungeonUI", "SettingsUI", "PauseUI", "LoadingUI" };

    [MenuItem("RPG Silent/UI/V5/Author all prefabs")]
    public static string AuthorAll()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("UI authoring requires an idle editor.");
        if (!File.Exists("Docs/UIImplementation/2026-10-29-v5/legacy-capture.json")) throw new InvalidOperationException("Materialize the legacy views first.");
        report.Clear();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/UI/NotoSansSC-UI SDF.asset");
        if (font == null) throw new InvalidOperationException("Create the licensed UI font before authoring.");
        Directory.CreateDirectory(Templates);
        AuthorRowTemplate();
        ConnectArtwork();
        foreach (var name in Names)
        {
            var path = Views + name + ".prefab";
            var go = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var ctx = new ViewContext(go);
                ctx.Capture();
                Stretch(go.transform as RectTransform);
                switch (name)
                {
                    case "InventoryUI": AuthorInventory(ctx); break;
                    case "TownNpcUI": AuthorTown(ctx); break;
                    case "QuestJournalUI": AuthorJournal(ctx); break;
                    case "PortalUI": AuthorPortal(ctx); break;
                    case "MainUI": AuthorHud(ctx); break;
                    case "DungeonUI": AuthorDungeon(ctx); break;
                    case "SettingsUI": AuthorSettings(ctx); break;
                    case "StartUI": AuthorStart(ctx); break;
                    case "PauseUI": AuthorPause(ctx); break;
                    case "LoadingUI": AuthorLoading(ctx); break;
                }
                ctx.FinalizePrefab();
                BakeNativeLayouts(go);
                if(name=="PortalUI")SaveSubView(ctx,ctx.R("Scroll View/Viewport/Content/Item"),"Row_Destination");
                foreach(var creation in go.GetComponentsInChildren<CharacterCreationView>(true))SaveSubView(ctx,(RectTransform)creation.transform,"Page_CharacterCreation");
                foreach(var selection in go.GetComponentsInChildren<CharacterSelectionView>(true))SaveSubView(ctx,(RectTransform)selection.transform,"Page_CharacterSelection");
                foreach(var page in go.GetComponentsInChildren<MonoBehaviour>(true).Where(x=>x is ScreenPage||x is SoundPage||x is ControllerPage||x is GamePage))SaveSubView(ctx,(RectTransform)page.transform,"Page_Settings"+page.GetType().Name.Replace("Page",""));
                PrefabUtility.SaveAsPrefabAsset(go,path);
                report.Add(name + ": " + ctx.Count + " serialized bindings");
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines("Docs/UIImplementation/2026-10-29-v5/prefab-authoring.txt",report);
        return string.Join("\n",report);
    }

    [MenuItem("RPG Silent/UI/V5/Create licensed UI font")]
    public static string CreateFont()
    {
        Directory.CreateDirectory("Assets/Fonts/UI");
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/UI/NotoSansSC-UI SDF.asset");
        if (existing != null) return "UI font already exists.";
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/UI/NotoSansCJKsc-Regular.otf");
        if (source == null) throw new InvalidOperationException("Import NotoSansCJKsc-Regular.otf and its OFL license first.");
        font = TMP_FontAsset.CreateFontAsset(source,48,5,GlyphRenderMode.SDFAA,4096,4096,AtlasPopulationMode.Dynamic,true);
        font.name = "NotoSansSC-UI SDF";
        font.isMultiAtlasTexturesEnabled = true;
        var characters = new HashSet<char>();
        foreach (var path in Directory.EnumerateFiles("Assets/Scripts","*.cs",SearchOption.AllDirectories))
            foreach (var ch in File.ReadAllText(path)) if (ch >= 32 && (ch < 127 || ch >= 0x2e80 && ch <= 0x9fff || ch >= 0x2000 && ch <= 0x2bff || "←→↑↓−×·…？！：“”《》【】（）％／，。；、".Contains(ch))) characters.Add(ch);
        // Include text in the authored prefabs and data assets, including names absent from code.
        foreach (var path in Directory.EnumerateFiles("Assets/Resources","*.asset",SearchOption.AllDirectories))
        {
            var text = Regex.Replace(File.ReadAllText(path),@"\\u([0-9a-fA-F]{4})",m => ((char)Convert.ToInt32(m.Groups[1].Value,16)).ToString());
            foreach (var ch in text) if (ch >= 0x2e80 && ch <= 0x9fff) characters.Add(ch);
        }
        string missing;
        font.TryAddCharacters(new string(characters.OrderBy(c=>c).ToArray()),out missing);
        AssetDatabase.CreateAsset(font,"Assets/Fonts/UI/NotoSansSC-UI SDF.asset");
        AssetDatabase.AddObjectToAsset(font.material,font);
        foreach (var atlas in font.atlasTextures) if (atlas != null && !AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas,font);
        EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
        return "Authored " + font.characterTable.Count + " characters; unavailable glyphs: " + missing;
    }

    sealed class ViewContext
    {
        public readonly GameObject Root;
        public readonly List<UIViewReferences.Entry> Entries = new();
        readonly Dictionary<Transform,Dictionary<string,Transform>> lookup = new();
        public int Count => Entries.Count;
        public ViewContext(GameObject root) { Root=root; }
        public void Capture()
        {
            var existing=Root.GetComponent<UIViewReferences>();
            if(existing!=null && existing.Entries.Count>0)
            {foreach(var entry in existing.Entries)if(entry.Scope!=null&&entry.Target!=null)Add(entry.Scope,entry.Id,entry.Target.transform);return;}
            foreach (var p in Root.GetComponentsInChildren<RectTransform>(true))
                foreach (Transform t in p) Add(p,t.name,t);
        }
        public void Add(Transform scope,string id,Transform target)
        {
            if (!lookup.TryGetValue(scope,out var ids)) lookup[scope]=ids=new Dictionary<string,Transform>();
            if (ids.TryGetValue(id,out var prior)) { if (prior!=target&&id != "Rule") report.Add(Root.name + ": duplicate decorative ID " + id + " under " + scope.name); return; }
            ids[id]=target; Entries.Add(new UIViewReferences.Entry{Scope=scope,Id=id,Target=target.gameObject});
        }
        public Transform Find(Transform scope,string id)
        {
            var current=scope;
            foreach (var part in id.Split('/'))
            { if (!lookup.TryGetValue(current,out var local) || !local.TryGetValue(part,out current)) throw new InvalidOperationException(Root.name + " missing authored binding: " + id + " under " + scope.name); }
            return current;
        }
        public RectTransform R(string path) => (RectTransform)Find(Root.transform,path);
        public RectTransform R(Transform scope,string id) => (RectTransform)Find(scope,id);
        public RectTransform Group(Transform scope,string id,Transform parent=null)
        { if(lookup.TryGetValue(scope,out var d)&&d.TryGetValue(id,out var old))return (RectTransform)old;var r=NewRect(parent??scope,"Group_"+id); Add(scope,id,r); return r; }
        public RectTransform Text(Transform scope,string id,string value,float size=22,Transform parent=null)
        { if(lookup.TryGetValue(scope,out var d)&&d.TryGetValue(id,out var old))return (RectTransform)old;var t=NewText(parent??scope,"Text_"+id,value,size); Add(scope,id,t.transform); return t.rectTransform; }
        public RectTransform Button(Transform scope,string id,string value,Transform parent=null)
        { if(lookup.TryGetValue(scope,out var d)&&d.TryGetValue(id,out var old))return (RectTransform)old;var b=NewButton(parent??scope,"Button_"+id,value); Add(scope,id,b.transform); Add(b.transform,"Label",b.GetComponentInChildren<TMP_Text>().transform); return (RectTransform)b.transform; }
        public void FinalizePrefab()
        {
            AddModalTextScrolling(Root);
            foreach(var dropdown in Root.GetComponentsInChildren<TMP_Dropdown>(true))StyleDropdown(dropdown);
            foreach (var text in Root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font=font; text.fontSharedMaterial=font.material; text.raycastTarget=false;
                float authored=text.enableAutoSizing?text.fontSizeMax:text.fontSize;text.enableAutoSizing=true;text.fontSizeMax=authored;text.fontSizeMin=Mathf.Max(11,authored*.8f);text.textWrappingMode=TextWrappingModes.Normal;
                text.overflowMode=TextOverflowModes.Ellipsis; text.color = text.color.a == 0 ? Paper : text.color;
            }
            foreach (var image in Root.GetComponentsInChildren<RawImage>(true)) if (image.GetComponent<CharacterModelPreview>() == null) image.raycastTarget=false;
            foreach(var image in Root.GetComponentsInChildren<Image>(true))image.raycastTarget=image.GetComponent<Selectable>()!=null||image.GetComponent<ScrollRect>()!=null||image.name.Contains("Shade")||image.name.Contains("Confirmation");
            foreach(var selectable in Root.GetComponentsInChildren<Selectable>(true))if(selectable.targetGraphic!=null)selectable.targetGraphic.raycastTarget=true;
            foreach(var shade in Root.GetComponentsInChildren<RectTransform>(true).Where(x=>x.name.Contains("Shade")&&x.GetComponent<Image>()!=null))Stretch(shade);
            foreach (var component in Root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;
                var serialized=new SerializedObject(component);
                var iterator=serialized.GetIterator();
                while (iterator.NextVisible(true)) if (iterator.propertyType==SerializedPropertyType.ObjectReference && iterator.objectReferenceValue is TMP_FontAsset) iterator.objectReferenceValue=font;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var button in Root.GetComponentsInChildren<Button>(true)) StyleButton(button);
            foreach(var text in Root.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name.Contains("TMP")||t.name=="Text"))
                text.name=text.GetComponentInParent<Selectable>()!=null?"Text_Label":"Text_"+Regex.Replace(text.transform.parent.name,@"^(Group|Image|RawImage)_","");
            foreach (var t in Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (t == Root.transform) continue;
                var name=Regex.Replace(t.name,@"[^\p{L}\p{N}_-]","_");
                if(name=="Quite")name="Quit";if(name=="Lift")name="BindingScope_SettingsNavigation";
                string prefix=t.GetComponent<Button>()!=null?"Button_":t.GetComponent<TMP_InputField>()!=null?"Input_":t.GetComponent<TMP_Dropdown>()!=null?"Dropdown_":t.GetComponent<ScrollRect>()!=null?"ScrollView_":t.GetComponent<Slider>()!=null?"Slider_":t.GetComponent<Toggle>()!=null?"Toggle_":t.GetComponent<TMP_Text>()!=null?"Text_":t.GetComponent<RawImage>()!=null?"RawImage_":t.GetComponent<Image>()!=null?"Image_":t.GetComponent<UIListView>()!=null?"Content_":"Group_";
                if (!Regex.IsMatch(name,@"^(Button|Input|Dropdown|ScrollView|Slider|Toggle|Text|RawImage|Image|Group|Content)_")) name=prefix+name;
                t.name=name;
            }
            // Unique sibling names are useful both for designers and automated scene inspection.
            foreach (var p in Root.GetComponentsInChildren<RectTransform>(true))
            {
                var seen=new HashSet<string>(); int i=0;
                foreach (Transform child in p) { string name=child.name; if (!seen.Add(name)) child.name=name+"_"+(++i).ToString("D2"); }
            }
            var refs=Root.GetComponent<UIViewReferences>()??Root.AddComponent<UIViewReferences>();
            Entries.RemoveAll(entry=>entry.Scope==null||entry.Target==null);
            refs.SetAuthoringEntries(Entries.ToArray());
        }
    }

    static RectTransform NewRect(Transform parent,string name)
    { var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false); return r; }
    static TMP_Text NewText(Transform parent,string name,string value,float size=22)
    { var t=NewRect(parent,name).gameObject.AddComponent<TextMeshProUGUI>(); t.font=font; t.fontSize=size; t.color=Paper; t.text=value; t.raycastTarget=false; return t; }
    static Button NewButton(Transform parent,string name,string value)
    {
        var r=NewRect(parent,name); var image=r.gameObject.AddComponent<Image>(); image.color=ProgressionUIWidgets.Card;
        var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=image;
        var text=NewText(r,"Text_Label",value,20); Stretch(text.rectTransform,10,4,10,4); text.alignment=TextAlignmentOptions.Center;
        StyleButton(b); return b;
    }
    static void StyleButton(Button b)
    {
        if(b.GetComponent<UiAudioFeedback>()==null)b.gameObject.AddComponent<UiAudioFeedback>();
        var caption=b.GetComponentInChildren<TMP_Text>(true);if(caption!=null&&(caption.enableAutoSizing?caption.fontSizeMax:caption.fontSize)>32)Size(caption,24);
        var colors=b.colors; colors.normalColor=Color.white;colors.highlightedColor=new Color(1.18f,1.18f,1.12f);colors.selectedColor=new Color(1.12f,1.12f,1.05f);colors.pressedColor=new Color(.8f,.84f,.78f);colors.disabledColor=new Color(.52f,.58f,.56f,.7f);b.colors=colors;
        var navigation=b.navigation; navigation.mode=Navigation.Mode.Automatic;b.navigation=navigation;
        if(b.image!=null && b.GetComponent<UIEquipmentSlotView>()==null && !b.name.Contains("Item") && !b.name.Contains("SkillSlot"))
        {
            bool primary=b.name=="Start"||b.name=="Button_Start"||b.name.Contains("Confirm")||b.name.Contains("TrainSelected")||b.name.Contains("StartAdventure")||b.name.Contains("ContinueCharacter")||b.name.Contains("EnterDungeon")||b.name.Contains("Wear");
            b.image.sprite=primary?Icon("sy-button-bg"):null;b.image.type=primary?Image.Type.Sliced:Image.Type.Simple;
            b.image.color=primary?new Color(.83f,.72f,.49f):ProgressionUIWidgets.Card;
            var label=b.GetComponentInChildren<TMP_Text>(true);if(label!=null)label.color=primary?new Color(.1f,.18f,.18f):Paper;
        }
    }
    static void StyleDropdown(TMP_Dropdown dropdown)
    {
        if(dropdown.captionText!=null){Size(dropdown.captionText,22,Paper);Stretch(dropdown.captionText.rectTransform,18,4,36,4);dropdown.captionText.alignment=TextAlignmentOptions.MidlineLeft;}
        if(dropdown.image!=null){dropdown.image.sprite=null;dropdown.image.color=ProgressionUIWidgets.Card;}
        foreach(var arrow in dropdown.GetComponentsInChildren<Image>(true).Where(i=>i.transform.parent==dropdown.transform&&i.name.Contains("Arrow")))arrow.color=Gold;
        var template=dropdown.template;if(template==null)return;
        template.anchorMin=Vector2.zero;template.anchorMax=new Vector2(1,0);template.pivot=new Vector2(.5f,1);template.anchoredPosition=new Vector2(0,-4);template.sizeDelta=new Vector2(0,240);
        var background=template.GetComponent<Image>();if(background!=null){background.sprite=null;background.color=Surface;}
        var scroll=template.GetComponent<ScrollRect>();if(scroll!=null&&scroll.content!=null)
        {
            var content=scroll.content;content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=Vector2.zero;
            Vertical(content,4,2);var fitter=content.GetComponent<ContentSizeFitter>()??content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            foreach(var toggle in content.GetComponentsInChildren<Toggle>(true))
            {
                Element(toggle.gameObject,44);var label=toggle.GetComponentInChildren<TMP_Text>(true);if(label!=null){Stretch(label.rectTransform,28,3,12,3);Size(label,21,Paper);label.alignment=TextAlignmentOptions.MidlineLeft;}
                foreach(var image in toggle.GetComponentsInChildren<Image>(true))
                {if(image==toggle.graphic){Edge(image.rectTransform,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(7,0),new Vector2(15,15));image.color=Gold;}else{Stretch(image.rectTransform);image.sprite=null;image.color=ProgressionUIWidgets.Card;}}
            }
        }
    }
    static void Stretch(RectTransform r,float left=0,float top=0,float right=0,float bottom=0)
    { r.localScale=Vector3.one;r.localRotation=Quaternion.identity;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top); }
    static void Box(RectTransform r,float x,float y,float w,float h,Transform parent=null)
    {
        // Long text is wrapped once in an authored native ScrollRect. On repeat
        // authoring, position its viewport and keep the text as fitted content.
        var scroll=r.parent!=null?r.parent.GetComponent<ScrollRect>():null;
        if(r.GetComponent<TMP_Text>()!=null&&scroll!=null&&scroll.content==r&&scroll.viewport==r.parent)r=scroll.viewport;
        if(parent!=null)r.SetParent(parent,false);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
    }
    static void Center(RectTransform r,float w,float h)
    { r.localScale=Vector3.one;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(w,h); }
    static void Edge(RectTransform r,Vector2 anchor,Vector2 pivot,Vector2 offset,Vector2 size)
    { r.localScale=Vector3.one;r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=offset;r.sizeDelta=size; }
    static Image Back(Transform parent,string name,Color color)
    { var old=parent.Find(name);var r=old!=null?(RectTransform)old:NewRect(parent,name);Stretch(r);var i=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i; }
    static void Separator(Transform parent,float x,float y,float w)
    { var name="Image_SectionRule_"+(int)x+"_"+(int)y;var old=parent.Find(name);var r=old!=null?(RectTransform)old:NewRect(parent,name);Box(r,x,y,w,1);var i=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();i.color=LineColor;i.raycastTarget=false; }
    static void Size(TMP_Text t,float size,Color? color=null)
    {t.fontSize=size;t.fontSizeMax=size;t.fontSizeMin=Mathf.Max(11,size*.8f);if(color.HasValue)t.color=color.Value;}
    static LayoutElement Element(GameObject go,float height=-1,float width=-1,float flexibleWidth=-1)
    {var e=go.GetComponent<LayoutElement>()??go.AddComponent<LayoutElement>();e.minWidth=0;e.minHeight=0;e.preferredHeight=height;e.flexibleHeight=height>=0?0:-1;e.preferredWidth=width>=0?width:flexibleWidth>0?0:-1;e.flexibleWidth=flexibleWidth>=0?flexibleWidth:width>=0?0:-1;return e;}
    static VerticalLayoutGroup Vertical(Transform target,int padding=0,float spacing=12)
    {var g=target.GetComponent<VerticalLayoutGroup>()??target.gameObject.AddComponent<VerticalLayoutGroup>();g.padding=new RectOffset(padding,padding,padding,padding);g.spacing=spacing;g.childControlWidth=true;g.childControlHeight=true;g.childForceExpandWidth=true;g.childForceExpandHeight=false;return g;}
    static HorizontalLayoutGroup Horizontal(Transform target,int padding=0,float spacing=12)
    {var g=target.GetComponent<HorizontalLayoutGroup>()??target.gameObject.AddComponent<HorizontalLayoutGroup>();g.padding=new RectOffset(padding,padding,padding,padding);g.spacing=spacing;g.childControlWidth=true;g.childControlHeight=true;g.childForceExpandWidth=false;g.childForceExpandHeight=true;return g;}
    static void Field(UnityEngine.Object target,string name,UnityEngine.Object value)
    {var so=new SerializedObject(target);var p=so.FindProperty(name);if(p==null)throw new InvalidOperationException(target.name+" has no serialized field "+name);p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void ArrayField(UnityEngine.Object target,string name,UnityEngine.Object[] values)
    {var so=new SerializedObject(target);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static void NativeList(RectTransform rows)
    {
        rows.anchorMin=new Vector2(0,1);rows.anchorMax=Vector2.one;rows.pivot=new Vector2(.5f,1);rows.anchoredPosition=Vector2.zero;rows.sizeDelta=Vector2.zero;
        Vertical(rows,8,12);var fit=rows.GetComponent<ContentSizeFitter>()??rows.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var view=rows.GetComponent<UIListView>()??rows.gameObject.AddComponent<UIListView>();Field(view,"rowPrefab",rowPrefab);
    }
    static void AuthorRowTemplate()
    {
        var go=NewRect(null,"Row_ServiceOrJournal").gameObject;
        try
        {
            var background=go.AddComponent<Image>(); background.color=ProgressionUIWidgets.Card;
            var selection=go.AddComponent<Button>();selection.targetGraphic=background;
            var root=Vertical(go.transform,18,9);var minimum=Element(go);minimum.minHeight=100;
            var head=NewRect(go.transform,"Group_Header");Horizontal(head,0,14);Element(head.gameObject,46);
            var icon=NewRect(head,"Image_Icon").gameObject.AddComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;Element(icon.gameObject,44,44);
            var title=NewText(head,"Text_Title","条目名称",24);Element(title.gameObject,-1,-1,1);
            var description=NewText(go.transform,"Text_Description","条目说明、条件与成本",19);description.overflowMode=TextOverflowModes.Overflow;
            var actions=NewRect(go.transform,"Group_Actions");Horizontal(actions,0,10);Element(actions.gameObject,46);
            var buttons=new List<Button>();var labels=new List<TMP_Text>();
            for(int i=0;i<4;i++){var b=NewButton(actions,"Button_Action"+i,"操作");Element(b.gameObject,46,-1,1);buttons.Add(b);labels.Add(b.GetComponentInChildren<TMP_Text>());}
            var view=go.AddComponent<UIListRowView>();Field(view,"title",title);Field(view,"description",description);Field(view,"icon",icon);Field(view,"background",background);Field(view,"selection",selection);Field(view,"actionGroup",actions.gameObject);ArrayField(view,"actions",buttons.Cast<UnityEngine.Object>().ToArray());ArrayField(view,"actionLabels",labels.Cast<UnityEngine.Object>().ToArray());
            var saved=PrefabUtility.SaveAsPrefabAsset(go,Templates+"Row_ServiceOrJournal.prefab");rowPrefab=saved.GetComponent<UIListRowView>();
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static void BakeNativeLayouts(GameObject root)
    {
        var active=root.GetComponentsInChildren<RectTransform>(true).ToDictionary(r=>r.gameObject,r=>r.gameObject.activeSelf);
        try
        {
            foreach(var go in active.Keys)go.SetActive(true);
            Canvas.ForceUpdateCanvases();
            foreach(var group in root.GetComponentsInChildren<LayoutGroup>(true))LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
        }
        finally{foreach(var pair in active)pair.Key.SetActive(pair.Value);}
    }
    static void AddModalTextScrolling(GameObject root)
    {
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if(text.rectTransform.rect.height<150||text.GetComponentInParent<ScrollRect>(true)!=null)continue;
            bool modal=false;for(var p=text.transform.parent;p!=null&&p!=root.transform;p=p.parent)
                if(p.name.Contains("Shade")||p.name.Contains("Window")||p.name.Contains("Confirmation")||p.name.Contains("Result")||p.name.Contains("Review")){modal=true;break;}
            if(!modal)continue;
            var original=text.rectTransform;var viewport=NewRect(original.parent,"ScrollView_"+text.name);
            viewport.anchorMin=original.anchorMin;viewport.anchorMax=original.anchorMax;viewport.pivot=original.pivot;viewport.anchoredPosition=original.anchoredPosition;viewport.sizeDelta=original.sizeDelta;viewport.SetSiblingIndex(original.GetSiblingIndex());
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=original;scroll.horizontal=false;scroll.scrollSensitivity=32;scroll.movementType=ScrollRect.MovementType.Clamped;
            original.SetParent(viewport,false);original.anchorMin=new Vector2(0,1);original.anchorMax=Vector2.one;original.pivot=new Vector2(.5f,1);original.anchoredPosition=Vector2.zero;original.sizeDelta=Vector2.zero;
            var fit=original.gameObject.GetComponent<ContentSizeFitter>()??original.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;text.overflowMode=TextOverflowModes.Overflow;
        }
    }
}
#endif
