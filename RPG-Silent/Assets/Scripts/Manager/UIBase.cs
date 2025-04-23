using UnityEngine;

public abstract class UIBase : MonoBehaviour
{
    public string UIName { get; private set; }

    public void Init(string uiName)
    {
        UIName = uiName;
        OnInit();
    }

    protected virtual void OnInit() { }
    public virtual void OnOpen(params object[] args) { }
    public virtual void OnClose() { }
}
