using TMPro;
using UnityEngine;

/// <summary>Friendly summons and optional elites must be distinguishable at a glance.</summary>
public sealed class EnemyAllegianceLabel : MonoBehaviour
{
    EnemyController enemy;
    TextMeshPro label;
    Camera viewer;
    void Start()
    {
        enemy=GetComponent<EnemyController>();
        var go=new GameObject("Allegiance label");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*2.2f;
        label=go.AddComponent<TextMeshPro>();label.font=Resources.Load<LootPresentation>("Loot/Presentation")?.Font;
        label.fontSize=3;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(5,1);
        label.color=enemy.IsCompanion?new Color(.4f,1,.6f):new Color(1,.6f,.25f);
        label.text=enemy.IsCompanion?"友方兽灵":"精英";
        viewer=Camera.main;
    }
    void LateUpdate()
    {
        if(label==null||viewer==null)return;
        label.gameObject.SetActive(enemy!=null&&!enemy.IsDead&&Vector3.Distance(viewer.transform.position,transform.position)<24);
        label.transform.rotation=viewer.transform.rotation;
    }
}
