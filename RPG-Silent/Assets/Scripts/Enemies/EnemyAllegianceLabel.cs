using TMPro;
using UnityEngine;

/// <summary>Friendly summons and optional elites must be distinguishable at a glance.</summary>
public sealed class EnemyAllegianceLabel : MonoBehaviour
{
    EnemyController enemy;
    TextMeshPro label;
    Camera viewer;
    float nextRefresh;
    void Start()
    {
        enemy=GetComponent<EnemyController>();
        var go=new GameObject("Allegiance label");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*2.2f;
        label=go.AddComponent<TextMeshPro>();label.font=Resources.Load<LootPresentation>("Loot/Presentation")?.Font;
        label.fontSize=2.6f;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(9,2.5f);
        label.raycastTarget=false;go.transform.localPosition=Vector3.up*3.05f;
        label.color=enemy.IsCompanion?new Color(.4f,1,.6f):new Color(1,.6f,.25f);
        label.text="";
        viewer=Camera.main;
    }
    void LateUpdate()
    {
        if(label==null||viewer==null)return;
        label.gameObject.SetActive(enemy!=null&&enemy.isActiveAndEnabled&&!enemy.IsDead&&!enemy.CombatLocked&&Vector3.Distance(viewer.transform.position,transform.position)<30);
        label.transform.rotation=viewer.transform.rotation;
        if(Time.unscaledTime<nextRefresh)return;
        nextRefresh=Time.unscaledTime+.1f;
        label.color=enemy.IsCompanion?new Color(.4f,1,.6f):enemy.IsEnraged?new Color(1,.35f,.2f):new Color(1,.8f,.45f);
        string title=enemy.IsCompanion?"友方兽灵":enemy.IsElite?EnemyTraitState.Name(enemy.EliteTrait):"";
        string action=enemy.AttackWarningHint;
        label.text=title+(title.Length>0?"\n":"")+(action.Length>0?action:enemy.TraitHint.Length>0?enemy.TraitHint:enemy.CombatHint);
    }
}
