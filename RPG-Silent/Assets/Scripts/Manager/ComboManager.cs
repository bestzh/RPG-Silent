using UnityEngine;

public class ComboManager : MonoBehaviour
{
    private PlayerAnimationController animationController;
    private bool isComboQueued = false;

    private void Awake()
    {
        animationController = GetComponent<PlayerAnimationController>();
    }

    private void Update()
    {
        if (animationController.IsPlayingAnimation())
        {
            if (Input.GetMouseButtonDown(0)) // 左键点击连击
            {
                TryQueueCombo();
            }
        }
    }

    private void TryQueueCombo()
    {
        var currentAnimName = animationController.CurrentAnimationName();
        if (currentAnimName == null) return;

        var currentAnimData = animationController.GetAnimationData(currentAnimName);
        if (currentAnimData == null || string.IsNullOrEmpty(currentAnimData.NextComboName))
            return;

        isComboQueued = true;
    }

    public void TryExecuteCombo()
    {
        if (!isComboQueued) return;

        var currentAnimName = animationController.CurrentAnimationName();
        var currentAnimData = animationController.GetAnimationData(currentAnimName);
        if (currentAnimData == null) return;

        if (!string.IsNullOrEmpty(currentAnimData.NextComboName))
        {
            animationController.PlayAnimation(currentAnimData.NextComboName);
        }

        isComboQueued = false;
    }
}
