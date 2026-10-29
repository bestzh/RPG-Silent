using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class UiAudioFeedback : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    Button button;
    UIBase owner;
    public void OnPointerClick(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) Play(); }
    public void OnSubmit(BaseEventData eventData) => Play();
    void Play()
    {
        if (button == null) button = GetComponent<Button>();
        if (!button.IsInteractable()) return;
        if (owner == null) owner = GetComponentInParent<UIBase>();
        if (owner != null) owner.Audio?.Play(GameSound.Button, Vector3.zero);
    }
}
