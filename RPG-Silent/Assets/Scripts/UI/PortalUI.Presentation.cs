using RPGSilent.Domain;
using UnityEngine.UI;

public partial class PortalUI
{
    readonly Button[] modeButtons = new Button[6];
    void BindV5Modes()
    {
        var group = UIView.Find(transform, "ChallengeModes");
        for (int i = 0; i < modeButtons.Length; i++)
        {
            int index = i;
            modeButtons[i] = UIView.Button(group, "Mode" + i, DungeonChallengeRules.Label((DungeonChallenge)i), () =>
            {
                challenge = (DungeonChallenge)index;
                OnDifficultyChanged(difficultyDropdown.value);
            });
        }
    }
    void RefreshV5Modes()
    {
        UIView.Find(transform, "ChallengeModes").gameObject.SetActive(!_selectedDungeon.IsTraining);
        for (int i = 0; i < modeButtons.Length; i++)
            modeButtons[i].image.color = (int)challenge == i ? ProgressionUIWidgets.Accent : ProgressionUIWidgets.Card;
    }
}
