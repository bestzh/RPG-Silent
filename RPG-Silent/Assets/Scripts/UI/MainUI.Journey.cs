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
        if (questDirectionText != null)
            return;
        var root = UIView.Find(transform, "TopLeftPanel");
        var font = classNameText.font;
        questDirectionText = UIView.Text(questTracker, "QuestNavigation", "");
        questDirectionText.alignment = TextAlignmentOptions.Center;
        questDirectionText.color = new Color(1, .85f, .4f);
        questDirectionArrow = UIView.Text(questTracker, "QuestDirectionArrow", "▲");
        questDirectionArrow.alignment = TextAlignmentOptions.Center;
        questDirectionArrow.color = questDirectionText.color;
        journeyNotice = UIView.Text(root, "JourneyNotice", "");
        journeyNotice.alignment = TextAlignmentOptions.Center;
        journeyNotice.color = new Color(1, .91f, .63f);
        townClock = UIView.Text(root, "TownClock", "");
        if (mapOverlay != null)
        {
            trackedMapPin = UIView.Rect(mapOverlay, "TrackedDestinationPin");
            trackedMapPin.localRotation = Quaternion.Euler(0, 0, 45);
            var pin = trackedMapPin.gameObject.GetComponent<Image>();
            pin.color = new Color(1, .85f, .3f);
            pin.raycastTarget = false;
        }
    }

    void RefreshJourneyHud()
    {
        if (questDirectionText == null || _progress == null || Time.unscaledTime < nextJourneyUpdate)
            return;
        nextJourneyUpdate = Time.unscaledTime + .1f;
        bool show = _gameSettings?.CurrentSettings.ShowHud ?? true;
        questNavigation = _progress.QuestDirection(combatPlayer);
        questDirectionText.gameObject.SetActive(show && _progress.TrackedQuest != null);
        questDirectionText.text = questNavigation.Label + (questNavigation.HasTarget ? $"\n{questNavigation.Distance:0} 米 · 金色标记" : "");
        questDirectionArrow.gameObject.SetActive(show && questNavigation.HasTarget);
        if (journeyCamera == null)
            journeyCamera = Camera.main;
        if (questNavigation.HasTarget && journeyCamera != null && combatPlayer != null)
        {
            var delta = questNavigation.Target.position - combatPlayer.transform.position;
            delta.y = 0;
            var forward = journeyCamera.transform.forward;
            forward.y = 0;
            float angle = Vector3.SignedAngle(forward, delta, Vector3.up);
            questDirectionArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        townClock.gameObject.SetActive(show && combatPlayer != null && !combatPlayer.CombatAllowed);
        townClock.text = _progress.TownTimeLabel + (_progress.TownHour >= 6 && _progress.TownHour < 20 ? " · 日间" : " · 夜间");
        journeyNotice.gameObject.SetActive(show);
        journeyNotice.alpha = _gameSettings?.CurrentSettings.FeedbackIntensity ?? 1;
        if (Time.unscaledTime >= noticeUntil)
        {
            string next = _progress.TakeNotice();
            journeyNotice.text = next ?? "";
            if (next != null)
                noticeUntil = Time.unscaledTime + 4;
        }
    }

    void AddTrackedMapMarker(ref int index)
    {
        if (trackedMapPin == null)
            return;
        trackedMapPin.gameObject.SetActive(questNavigation.HasTarget);
        if (!questNavigation.HasTarget)
            return;
        var delta = questNavigation.Target.position - combatPlayer.transform.position;
        var offset = Vector2.ClampMagnitude(new Vector2(delta.x, delta.z), 45) / 45;
        var half = mapOverlay.rect.size * .38f;
        trackedMapPin.anchoredPosition = new Vector2(offset.x * half.x, offset.y * half.y);
        trackedMapPin.SetAsLastSibling();
    }
}
