using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Prefab-owned review surface. The caller owns the transaction and validation.</summary>
public sealed class UIConfirmationView : MonoBehaviour
{
    [SerializeField] TMP_Text title, details;
    [SerializeField] Button confirm, cancel;
    public bool IsOpen => gameObject.activeSelf;
    public void Show(string heading, string body, UnityAction submit, UnityAction dismiss)
    {
        title.text = heading; details.text = body;
        confirm.onClick.RemoveAllListeners(); cancel.onClick.RemoveAllListeners();
        confirm.onClick.AddListener(() => { Hide(); submit?.Invoke(); });
        cancel.onClick.AddListener(() => { Hide(); dismiss?.Invoke(); });
        gameObject.SetActive(true); transform.SetAsLastSibling();
    }
    public void Hide() => gameObject.SetActive(false);
    public void Dismiss() { if (IsOpen) cancel.onClick.Invoke(); }
}
