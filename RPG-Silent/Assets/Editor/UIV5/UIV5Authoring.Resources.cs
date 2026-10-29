#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RPGSilent.Domain;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class UIV5Authoring
{
    [Serializable] class DesignResource { public string id,path,guid; }
    [Serializable] class DesignResources { public DesignResource[] entries; }
    static Dictionary<string,DesignResource> designResources;
    static Sprite Icon(string id)
    {
        if(designResources==null)designResources=JsonUtility.FromJson<DesignResources>(File.ReadAllText("Tools/UIV5/Resources/resource-map.json")).entries.GroupBy(x=>x.id).ToDictionary(x=>x.Key,x=>x.First());
        if(!designResources.TryGetValue(id,out var entry))throw new InvalidOperationException("No approved resource: "+id);
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(entry.path);
        if(sprite==null)throw new InvalidOperationException("Missing imported Sprite: "+entry.path);
        return sprite;
    }
    [MenuItem("RPG Silent/UI/V5/Connect approved item and skill artwork")]
    public static string ConnectArtwork()
    {
        int count=0;var changes=new List<string>();
        foreach(var skill in Resources.LoadAll<SkillData>("ClassSkills"))
        {string id="skill-"+skill.name;skill.Icon=Icon(id);EditorUtility.SetDirty(skill);changes.Add(AssetDatabase.GetAssetPath(skill)+" -> "+id);count++;}
        foreach(var item in Resources.LoadAll<ItemDefinition>("Items"))
        {
            string id=null;
            if(item.Kind==ItemKind.Equipment)id=item.Slot switch{EquipmentSlot.Head=>"gear-head",EquipmentSlot.Chest=>item.Id.StartsWith("warden-")?"gear-candidate":"gear-chest",EquipmentSlot.Feet=>"gear-feet",EquipmentSlot.Ring=>"gear-ring",_=>"gear-amulet"};
            else if(item.Kind==ItemKind.Weapon)
            {
                if(item.Id.EndsWith("greatsword"))id="gear-greatsword";else if(item.Id.EndsWith("sword"))id="gear-sword";
                else if(item.Id.EndsWith("bow"))item.Icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/Icons_Weapons/ICON_SM_Prop_Bow_01.png");
                else if(item.Id.EndsWith("staff"))item.Icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/Icons_Weapons/ICON_SM_Wep_Staff_01.png");
            }
            else id=item.Id switch{"forge-shard"=>"item-ore","crystal-shard"=>"item-crystal","potion-health"=>"item-potion-red","potion-energy"=>"item-potion-blue","potion-cleanse"=>"item-potion-green","potion-guard"=>"item-potion-green",_=>item.IsGem?"item-crystal":null};
            if(id!=null)item.Icon=Icon(id);
            if(item.Icon==null)throw new InvalidOperationException("No actual artwork for "+item.Id);
            EditorUtility.SetDirty(item);changes.Add(AssetDatabase.GetAssetPath(item)+" -> "+AssetDatabase.GetAssetPath(item.Icon));count++;
        }
        AssetDatabase.SaveAssets();File.WriteAllLines("Docs/UIImplementation/2026-10-29-v5/artwork-connections.txt",changes);
        return count+" item/skill icon references connected; stats, prices, IDs and save data are unchanged.";
    }
    static void ButtonGlyph(ViewContext c,RectTransform button,string id)
    {
        var icon=c.Group(button,"NavigationIcon");Box(icon,16,15,32,32);var image=icon.GetComponent<Image>()??icon.gameObject.AddComponent<Image>();image.sprite=Icon(id);image.color=new Color(.68f,.78f,.68f);image.preserveAspect=true;image.raycastTarget=false;
        var label=button.GetComponentInChildren<TMPro.TMP_Text>();Stretch(label.rectTransform,66,5,12,5);label.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
    }
}
#endif
