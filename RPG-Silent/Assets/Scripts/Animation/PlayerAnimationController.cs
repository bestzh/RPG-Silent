using UnityEngine;
using System.Collections.Generic;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    
    [Header("动作数据列表")]
    public List<AnimationData> animations;

    private Dictionary<string, AnimationData> animationDict = new Dictionary<string, AnimationData>();
    private AnimationData currentAnimation = null;
    private float currentDuration = 0f;
    private float currentTimer = 0f;
    private bool isPlaying = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // 初始化表
        foreach (var anim in animations)
        {
            if (anim != null && !animationDict.ContainsKey(anim.AnimationName))
            {
                animationDict.Add(anim.AnimationName, anim);
            }
        }
    }

    private void Update()
    {
        if (isPlaying)
        {
            currentTimer += Time.deltaTime;
            if (currentTimer >= currentDuration)
            {
                EndCurrentAnimation();
            }
        }
    }
    public AnimationData GetCurrentAnimation()
    {
        return currentAnimation;
    }
    public AnimationData GetAnimationData(string name)
    {
        if (animationDict.TryGetValue(name, out var anim))
        {
            return anim;
        }
        else
        {
            Debug.LogWarning($"动画 {name} 未找到！");
            return null;
        }
    }

    public bool PlayAnimation(string name, float durationOverride = -1f)
    {
        if (!animationDict.TryGetValue(name, out var anim))
        {
            Debug.LogWarning($"动画 {name} 未找到！");
            return false;
        }

        if (isPlaying && currentAnimation != null && !currentAnimation.CanInterrupt)
        {
            return false;
        }

        if (isPlaying && currentAnimation == null)
        {
            return false;
        }

        animator.CrossFadeInFixedTime(anim.AnimatorStateName, 0.06f);
        currentAnimation = anim;
        currentDuration = durationOverride > 0f ? durationOverride : anim.Duration;
        currentTimer = 0f;
        isPlaying = true;
        return true;
    }

    private void EndCurrentAnimation()
    {
        CancelAnimation();
        ReturnToMoveTree();
    }

    public void CancelAnimation()
    {
        isPlaying = false;
        currentAnimation = null;
        currentDuration = 0f;
        currentTimer = 0f;
    }
    public void ReturnToMoveTree()
    {
        animator.CrossFadeInFixedTime("MoveTree", 0.08f);
    }

    public void StopAnimation() => EndCurrentAnimation();

    public bool IsPlayingAnimation() => isPlaying;
    public float NormalizedProgress => isPlaying && currentDuration > 0f ? Mathf.Clamp01(currentTimer / currentDuration) : 1f;
    public string CurrentAnimationName() => currentAnimation != null ? currentAnimation.AnimationName : null;
}
