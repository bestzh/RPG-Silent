using UnityEngine;
using System.Collections.Generic;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    
    [Header("动作数据列表")]
    public List<AnimationData> animations;

    private Dictionary<string, AnimationData> animationDict = new Dictionary<string, AnimationData>();
    private AnimationData currentAnimation = null;
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
        if (isPlaying && currentAnimation != null)
        {
            currentTimer += Time.deltaTime;
            if (currentTimer >= currentAnimation.Duration)
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

    public bool PlayAnimation(string name)
    {
        if (!animationDict.TryGetValue(name, out var anim))
        {
            Debug.LogWarning($"动画 {name} 未找到！");
            return false;
        }

        if (isPlaying && !currentAnimation.CanInterrupt)
        {
            return false;
        }

        animator.Play(anim.AnimatorStateName);
        currentAnimation = anim;
        currentTimer = 0f;
        isPlaying = true;
        return true;
    }

    private void EndCurrentAnimation()
    {
        isPlaying = false;
        currentAnimation = null;
        currentTimer = 0f;
        ReturnToMoveTree();
    }
    public void ReturnToMoveTree()
    {
        animator.Play("MoveTree");
    }

    public bool IsPlayingAnimation() => isPlaying;
    public string CurrentAnimationName() => currentAnimation != null ? currentAnimation.AnimationName : null;
}
