using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public partial class MainUI
{
    TMP_Text questDirectionText, questDirectionArrow, journeyNotice, townClock;
    QuestNavigation questNavigation;
    float nextJourneyUpdate, noticeUntil;
    Camera journeyCamera;
    RectTransform trackedMapPin;
    void BuildJourneyHud()
    {
        if (questDirectionText != null) return;
        var root = transform.Find("TopLeftPanel"); var font = classNameText.font;
        questTracker.sizeDelta = new Vector2(390,290);
        questDirectionText = W.Text(questTracker, font, "QuestNavigation", "", 56, 204, 318, 74, 20);
        questDirectionText.alignment = TextAlignmentOptions.Center; questDirectionText.color = new Color(1,.85f,.4f);
        questDirectionArrow = W.Text(questTracker, font, "QuestDirectionArrow", "▲", 10, 220, 36, 36, 30);
        questDirectionArrow.rectTransform.pivot = new Vector2(.5f,.5f);questDirectionArrow.rectTransform.anchoredPosition = new Vector2(28,-238);
        questDirectionArrow.alignment = TextAlignmentOptions.Center; questDirectionArrow.color = questDirectionText.color;
        journeyNotice = W.Text(root, font, "JourneyNotice", "", 0, 0, 720, 110, 24);
        Anchor(journeyNotice.rectTransform, new Vector2(.5f, .23f), Vector2.zero);
        journeyNotice.alignment = TextAlignmentOptions.Center; journeyNotice.color = new Color(1,.91f,.63f);
        townClock = W.Text(root, font, "TownClock", "", 0, 0, 390, 32, 18);
        townClock.rectTransform.anchorMin = townClock.rectTransform.anchorMax = townClock.rectTransform.pivot = Vector2.one;
        townClock.rectTransform.anchoredPosition = new Vector2(-24,-370);
        if(tutorialButton!=null)tutorialButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(-220,-710);
        if(mapOverlay!=null)
        {
            trackedMapPin=W.Rect(mapOverlay,"TrackedDestinationPin",0,0,10,10);
            Anchor(trackedMapPin,new Vector2(.5f,.5f),Vector2.zero);trackedMapPin.localRotation=Quaternion.Euler(0,0,45);
            var pin=trackedMapPin.gameObject.AddComponent<Image>();pin.color=new Color(1,.85f,.3f);pin.raycastTarget=false;
        }
    }
    void RefreshJourneyHud()
    {
        if (questDirectionText == null || _progress == null || Time.unscaledTime < nextJourneyUpdate) return;
        nextJourneyUpdate = Time.unscaledTime + .1f;
        bool show = _gameSettings?.CurrentSettings.ShowHud ?? true;
        questNavigation = _progress.QuestDirection(combatPlayer);
        questDirectionText.gameObject.SetActive(show && _progress.TrackedQuest != null);
        questDirectionText.text = questNavigation.Label + (questNavigation.HasTarget ? $"\n{questNavigation.Distance:0} 米 · 金色标记" : "");
        questDirectionArrow.gameObject.SetActive(show && questNavigation.HasTarget);
        if (journeyCamera == null) journeyCamera = Camera.main;
        if (questNavigation.HasTarget && journeyCamera != null && combatPlayer != null)
        {
            var delta = questNavigation.Target.position - combatPlayer.transform.position; delta.y = 0;
            var forward = journeyCamera.transform.forward; forward.y = 0;
            float angle = Vector3.SignedAngle(forward, delta, Vector3.up);
            questDirectionArrow.rectTransform.localRotation = Quaternion.Euler(0,0,-angle);
        }
        townClock.gameObject.SetActive(show && combatPlayer != null && !combatPlayer.CombatAllowed);
        townClock.text = _progress.TownTimeLabel + (_progress.TownHour >= 6 && _progress.TownHour < 20 ? " · 日间" : " · 夜间");
        journeyNotice.gameObject.SetActive(show);
        if (Time.unscaledTime >= noticeUntil)
        {
            string next = _progress.TakeNotice(); journeyNotice.text = next ?? "";
            if (next != null) noticeUntil = Time.unscaledTime + 4;
        }
    }
    void AddTrackedMapMarker(ref int index)
    {
        if(trackedMapPin==null)return;
        trackedMapPin.gameObject.SetActive(questNavigation.HasTarget);
        if (!questNavigation.HasTarget) return;
        var delta = questNavigation.Target.position - combatPlayer.transform.position;
        var offset = Vector2.ClampMagnitude(new Vector2(delta.x,delta.z),45) / 45;
        var half = mapOverlay.rect.size * .38f;
        trackedMapPin.anchoredPosition = new Vector2(offset.x*half.x, offset.y*half.y);
        trackedMapPin.SetAsLastSibling();
    }
}
