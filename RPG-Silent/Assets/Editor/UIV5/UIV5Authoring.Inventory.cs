#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class UIV5Authoring
{
    static RectTransform Put(ViewContext c,Transform scope,string id,float x,float y,float w,float h,Transform parent=null,float size=0)
    {
        var r=c.R(scope,id);Box(r,x,y,w,h,parent);
        var b=r.GetComponent<Button>();if(b!=null&&r.GetComponent<UIEquipmentSlotView>()==null)
        {var label=b.GetComponentInChildren<TMP_Text>(true);if(label!=null){Stretch(label.rectTransform,8,3,8,3);if(size>0)Size(label,size);}}
        var text=r.GetComponent<TMP_Text>();if(text!=null&&size>0)Size(text,size);
        return r;
    }
    static void PageDecor(ViewContext c,RectTransform page,string subtitle,string background="BG2")
    {
        Center(page,1920,1080);var surface=page.GetComponent<Image>();if(surface!=null){surface.color=Color.clear;surface.raycastTarget=false;}
        var existing=page.Find("RawImage_PageBackground");var rect=existing!=null?(RectTransform)existing:NewRect(page,"RawImage_PageBackground");Stretch(rect);
        var raw=rect.GetComponent<RawImage>()??rect.gameObject.AddComponent<RawImage>();raw.texture=Resources.Load<Texture2D>("BG/"+background);raw.color=new Color(.34f,.46f,.44f);raw.raycastTarget=false;raw.transform.SetAsFirstSibling();
        c.Add(page,"Backdrop",raw.transform);
        var tint=Back(page,"Image_Atmosphere",new Color(.06f,.15f,.18f,.90f));tint.transform.SetSiblingIndex(1);
        c.Add(page,"Atmosphere",tint.transform);
        var brand=c.Text(page,"V5Brand","S I L E N T",26);Box(brand,72,28,260,44);Size(brand.GetComponent<TMP_Text>(),26,Gold);
        var sub=c.Text(page,"V5Subtitle",subtitle,18);Box(sub,76,198,1700,42);Size(sub.GetComponent<TMP_Text>(),18,Muted);
        var footer=c.Text(page,"V5Footer","Esc 优先取消当前输入、确认或试穿，再返回游戏",15);Box(footer,76,1030,1750,32);Size(footer.GetComponent<TMP_Text>(),15,Muted);
        if(c.Root.GetComponent<StartUI>()!=null)footer.GetComponent<TMP_Text>().text="选择职业与外观，或继续已有角色的独立进度";
        else if(c.Root.GetComponent<SettingsUI>()!=null)footer.GetComponent<TMP_Text>().text="应用后保存设置 · 返回撤销未应用修改";
        else if(c.Root.GetComponent<PortalUI>()!=null)footer.GetComponent<TMP_Text>().text="选择目的地、模式与难度 · Esc 返回主城";
        else if(c.Root.GetComponent<QuestJournalUI>()!=null)footer.GetComponent<TMP_Text>().text="Esc 优先取消放弃确认，再关闭手札";
        else if(c.Root.GetComponent<PauseUI>()!=null)footer.GetComponent<TMP_Text>().text="保存进度后可返回主菜单或退出游戏";
        else if(c.Root.GetComponent<LoadingUI>()!=null)footer.GetComponent<TMP_Text>().text="载入失败时可重试，或返回之前的页面";
    }
    static void AuthorInventory(ViewContext c)
    {
        var p=c.R("Inventory");p.name="Group_InventoryPage";PageDecor(c,p,"当前穿戴始终可见；点选物品比较，确认后才会更换。");
        Stretch(c.R("Shade"));c.R("Shade").GetComponent<Image>().color=Surface;
        Put(c,p,"Heading",72,112,1000,76,null,52);
        Put(c,p,"Capacity",1100,137,744,48,null,20);
        Put(c,p,"Close",1748,29,100,42,null,17);
        Put(c,p,"EquipmentTitle",72,230,550,42,null,26);Separator(p,72,272,550);
        Put(c,p,"PreviewTitle",410,287,212,38,null,18);
        Put(c,p,"CharacterPreview",404,334,218,410);
        Put(c,p,"RotateLeft",404,751,60,32,null,13);Put(c,p,"ResetView",470,751,86,32,null,13);Put(c,p,"RotateRight",562,751,60,32,null,13);
        Put(c,p,"PreviewHelp",410,792,212,46,null,14);Put(c,p,"PreviewCaption",410,842,212,70,null,14);
        var trialActions=c.Group(p,"TrialActions");Box(trialActions,72,912,550,44);Horizontal(trialActions,0,10);
        foreach(var id in new[]{"CancelTryOn","ConfirmOutfit"}){var r=c.R(p,id);r.SetParent(trialActions,false);Element(r.gameObject,40,-1,1);Stretch(r.GetComponentInChildren<TMP_Text>().rectTransform,6,2,6,2);Size(r.GetComponentInChildren<TMP_Text>(),16);}
        for(int i=0;i<7;i++)
        {
            var r=i<5?c.R(p,"Equipment"+i):c.Button(p,"WeaponSlot"+(i-5),"");
            Box(r,72,i<5?285+i*75:690+(i-5)*75,320,70);
            AuthorEquipmentSlot(c,r,i<5?"防具槽":"武器槽");
        }
        var weaponLabel=c.Text(p,"WeaponsCaption","两 把 武 器",15);Box(weaponLabel,72,660,320,28);Size(weaponLabel.GetComponent<TMP_Text>(),15,Muted);
        Put(c,p,"Summary",72,842,320,70,null,17);
        var bagTitle=c.Text(p,"BagHeading","背包物品",26);Box(bagTitle,662,230,650,42);Separator(p,662,272,650);
        for(int i=0;i<7;i++)Put(c,p,"Tab"+i,662+i*94,286,88,43,null,17);
        Put(c,p,"Search",662,345,202,36);Put(c,p,"ClearSearch",872,345,64,36,null,14);
        for(int i=0;i<4;i++)Put(c,p,"Sort"+i,1036+i*69,345,67,36,null,14);
        var grid=c.Group(p,"BagGrid");Box(grid,662,395,650,474);
        var layout=grid.GetComponent<GridLayoutGroup>()??grid.gameObject.AddComponent<GridLayoutGroup>();layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount=6;layout.cellSize=new Vector2(100,111);layout.spacing=new Vector2(10,10);layout.childAlignment=TextAnchor.UpperLeft;
        for(int i=0;i<24;i++)
        {
            var cell=c.R(p,"Item"+i);cell.SetParent(grid,false);cell.localScale=Vector3.one;
            Box(c.R(cell,"Icon"),28,24,44,44);Box(c.R(cell,"Label"),4,73,92,36);Size(c.R(cell,"Label").GetComponent<TMP_Text>(),13);
            Box(c.R(cell,"Badge"),4,2,92,20);Size(c.R(cell,"Badge").GetComponent<TMP_Text>(),11);
        }
        Put(c,p,"Page",662,884,300,34,null,16);Put(c,p,"Previous",1194,884,52,34,null,13);Put(c,p,"Next",1258,884,52,34,null,13);
        c.R(p,"Previous").GetComponentInChildren<TMP_Text>().text="←";c.R(p,"Next").GetComponentInChildren<TMP_Text>().text="→";
        string[] tools={"MultiTryOn","Loadouts","BatchMode","BulkSell","BulkDiscard"};
        for(int i=0;i<tools.Length;i++)Put(c,p,tools[i],662+i*132,928,124,40,null,15);
        Put(c,p,"Collect",990,884,190,34,null,15);Put(c,p,"BatchConfirm",990,884,190,34,null,15);
        Put(c,p,"ReviewBulk",990,884,190,34,null,15);
        Separator(p,1352,230,1);
        Put(c,p,"DetailsHeading",1378,230,280,42,null,26);Put(c,p,"LockItem",1774,232,74,38,null,14);
        Put(c,p,"EquipmentEffectDetails",1610,280,238,36,null,15);
        var viewport=c.Group(p,"DetailsViewport");Box(viewport,1378,326,470,470);
        var detailHit=viewport.GetComponent<Image>()??viewport.gameObject.AddComponent<Image>();detailHit.color=Color.clear;
        if(viewport.GetComponent<RectMask2D>()==null)viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.GetComponent<ScrollRect>()??viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;scroll.viewport=viewport;
        var content=c.Group(p,"DetailsContent",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;content.anchoredPosition=Vector2.zero;Vertical(content,0,12);var fitter=content.GetComponent<ContentSizeFitter>()??content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=content;
        foreach(var id in new[]{"ItemTitle","Description","Affixes","Gem","Comparison","SetBonus"})
        {
            var r=c.R(p,id);r.SetParent(content,false);Element(r.gameObject,id=="ItemTitle"?48:id=="Comparison"?150:id=="Affixes"?100:id=="SetBonus"?100:-1);
            var t=r.GetComponent<TMP_Text>();Size(t,id=="ItemTitle"?28:17);t.overflowMode=TextOverflowModes.Overflow;
        }
        var itemHeader=c.Group(p,"SelectedItemHeader",content);Element(itemHeader.gameObject,74);Horizontal(itemHeader,0,16);itemHeader.SetAsFirstSibling();
        var itemIcon=c.Group(p,"SelectedItemIcon",itemHeader);Element(itemIcon.gameObject,72,72);var iconImage=itemIcon.GetComponent<Image>()??itemIcon.gameObject.AddComponent<Image>();iconImage.preserveAspect=true;iconImage.raycastTarget=false;
        var itemTitle=c.R(p,"ItemTitle");itemTitle.SetParent(itemHeader,false);Element(itemTitle.gameObject,-1,-1,1);
        var replacement=c.Text(p,"ReplacementSummary","正在查看当前穿戴",18,content);Element(replacement.gameObject,64);replacement.SetSiblingIndex(2);
        var itemTable=AuthorStatTable(c,p,"ItemStatTable",content);itemTable.transform.SetSiblingIndex(3);
        var outfitTable=AuthorStatTable(c,p,"OutfitStatTable",content);outfitTable.transform.SetSiblingIndex(4);outfitTable.gameObject.SetActive(false);
        var mainActions=c.Group(p,"ItemActions");Box(mainActions,1378,821,470,54);Horizontal(mainActions,0,10);
        foreach(var id in new[]{"Wear","Secondary","Remove","QuickConsumable:0","QuickConsumable:1","QuickConsumable:2"})
        {var r=c.R(p,id);r.SetParent(mainActions,false);Element(r.gameObject,54,-1,1);Stretch(r.GetComponentInChildren<TMP_Text>().rectTransform,6,3,6,3);Size(r.GetComponentInChildren<TMP_Text>(),16);}
        var danger=c.Group(p,"ItemSecondaryActions");Box(danger,1378,892,470,40);Horizontal(danger,0,10);
        foreach(var id in new[]{"Sell","Dismantle","Discard"}){var r=c.R(p,id);r.SetParent(danger,false);Element(r.gameObject,40,-1,1);Stretch(r.GetComponentInChildren<TMP_Text>().rectTransform,6,2,6,2);Size(r.GetComponentInChildren<TMP_Text>(),16);}
        Put(c,p,"Status",88,975,1740,48,null,18);
        var notice=Back(p,"Image_StatusWash",new Color(.3f,.4f,.26f,.2f));Box((RectTransform)notice.transform,72,970,1776,48);notice.transform.SetSiblingIndex(2);
        foreach(var id in new[]{"PresetShade","DiscardShade","BulkShade"}){var shade=c.R(p,id);shade.SetParent(c.Root.transform,false);Stretch(shade);}
        var presetWindow=c.R(c.R(p,"PresetShade"),"Window");var presetRows=c.R(presetWindow,"Rows");
        var presetViewport=c.Group(presetWindow,"PresetViewport");Box(presetViewport,28,150,1004,420);
        var presetHit=presetViewport.GetComponent<Image>()??presetViewport.gameObject.AddComponent<Image>();presetHit.color=Color.clear;
        if(presetViewport.GetComponent<RectMask2D>()==null)presetViewport.gameObject.AddComponent<RectMask2D>();var presetScroll=presetViewport.GetComponent<ScrollRect>()??presetViewport.gameObject.AddComponent<ScrollRect>();presetScroll.horizontal=false;presetScroll.movementType=ScrollRect.MovementType.Clamped;presetScroll.viewport=presetViewport;presetScroll.content=presetRows;presetRows.SetParent(presetViewport,false);NativeList(presetRows);
        foreach(var duplicate in presetViewport.GetComponentsInChildren<ScrollRect>(true).Where(x=>x.transform!=presetViewport).ToArray())UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
    }
    static void AuthorEquipmentSlot(ViewContext c,RectTransform r,string caption)
    {
        var view=r.GetComponent<UIEquipmentSlotView>()??r.gameObject.AddComponent<UIEquipmentSlotView>();
        var name=c.R(r,"Label");Box(name,78,32,232,30);Size(name.GetComponent<TMP_Text>(),19);name.GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Left;
        var slot=c.Text(r,"SlotName",caption,14);Box(slot,78,5,170,24);Size(slot.GetComponent<TMP_Text>(),14,Muted);
        var state=c.Text(r,"SlotState","",12);Box(state,235,5,80,24);Size(state.GetComponent<TMP_Text>(),12,Gold);
        var icon=c.Group(r,"EquipmentIcon");Box(icon,10,9,52,52);var image=icon.GetComponent<Image>()??icon.gameObject.AddComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;
        var replacement=c.Group(r,"ReplacementMark");Box(replacement,0,0,3,70);var line=replacement.GetComponent<Image>()??replacement.gameObject.AddComponent<Image>();line.color=Gold;line.raycastTarget=false;line.enabled=false;
        var bg=r.GetComponent<Image>();bg.color=new Color(.22f,.34f,.32f,.4f);
        Field(view,"slot",slot.GetComponent<TMP_Text>());Field(view,"itemName",name.GetComponent<TMP_Text>());Field(view,"state",state.GetComponent<TMP_Text>());Field(view,"icon",image);Field(view,"replacement",line);
    }
    static UIStatTableView AuthorStatTable(ViewContext c,Transform scope,string id,Transform parent)
    {
        var table=c.Group(scope,id,parent);Vertical(table,0,0);Element(table.gameObject,178);
        var view=table.GetComponent<UIStatTableView>()??table.gameObject.AddComponent<UIStatTableView>();
        TMP_Text[] headings=new TMP_Text[3];var cells=new TMP_Text[4,3];
        for(int row=-1;row<4;row++)
        {
            var line=c.Group(table,"Row"+(row+1));Horizontal(line,0,8);Element(line.gameObject,row<0?30:36);
            var label=c.Text(line,"Name",row<0?"装备属性":new[]{"生命","能量","伤害","减伤"}[row],row<0?13:19);Element(label.gameObject,-1,90);
            for(int col=0;col<3;col++){var t=c.Text(line,"Value"+col,row<0?new[]{"基础","强化","合计"}[col]:"0",row<0?12:19).GetComponent<TMP_Text>();t.alignment=TextAlignmentOptions.Right;Element(t.gameObject,-1,-1,1);if(row<0)headings[col]=t;else cells[row,col]=t;}
        }
        Field(view,"firstHeading",headings[0]);Field(view,"secondHeading",headings[1]);Field(view,"thirdHeading",headings[2]);
        var so=new SerializedObject(view);var rows=so.FindProperty("rows");rows.arraySize=4;string[] names={"First","Second","Third"};for(int i=0;i<4;i++)for(int j=0;j<3;j++)rows.GetArrayElementAtIndex(i).FindPropertyRelative(names[j]).objectReferenceValue=cells[i,j];so.ApplyModifiedPropertiesWithoutUndo();return view;
    }
}
#endif
