using System;
using RPGSilent.Combat;
using RPGSilent.Domain;
using UnityEngine;
using VContainer;

/// <summary>
/// 技能系统主控：槽位装配、输入、冷却、MP 消耗、统一释放流程。
/// 命中与伤害复用 AttackExecutor；UI 通过 IPlayerSkillReader / PlayerSkillAccess 订阅。
/// </summary>
public class PlayerSkillController : MonoBehaviour, IPlayerSkillReader
{
    public const int MaxSlots = 10;

    [Header("Energy")]
    public float energyRecoveryRate = 10f;

    [Header("Slots（前 3 格对应按键 1/2/3，后续可扩展到 10）")]
    [SerializeField] private SkillData[] equippedSkills = new SkillData[MaxSlots];

    [Inject] private PlayerStats         _stats;
    [Inject] private IPlayerInputActions _input;
    [Inject] private PlayerSkillAccess   _skillAccess;
    [Inject] private PlayerProgressService _progress;

    private PlayerController          _player;
    private PlayerAnimationController _animationController;
    private AttackExecutor            _attackExecutor;

    private readonly float[] _cooldownRemaining = new float[MaxSlots];
    private readonly float[] _cooldownDuration  = new float[MaxSlots];

    private bool      _isCasting;
    private float     _castTimer;
    private float     _castDuration;
    private SkillData _castingSkill;
    private bool      _waitingForAnimationEvent;
    private bool      _effectResolved;
    private bool      _animationStarted;
    private AttackContext _attackContext;

    public bool IsCasting => _isCasting;
    public bool CanLink => !_isCasting || (_effectResolved && _castDuration <= 0f && _animationStarted
        && _animationController != null && _animationController.NormalizedProgress >= .8f);
    public bool CanQueueSlot(int slot) => IsValidSlot(slot) && equippedSkills[slot] != null
        && (_progress == null || _progress.GetSkillRank(equippedSkills[slot]) > 0)
        && _cooldownRemaining[slot] <= CombatActionBuffer.Lifetime
        && _stats != null && _stats.CurrentEnergy >= (_progress?.SkillEnergyCost(equippedSkills[slot]) ?? equippedSkills[slot].EnergyCost);
    public bool TryFinishForLink()
    {
        if (!CanLink) return false;
        if (_isCasting) ClearCastingState();
        return true;
    }

    public int SlotCount => MaxSlots;

    public event Action      OnSlotsChanged;
    public event Action<int> OnSlotCooldownChanged;

    private void Awake()
    {
        _player              = GetComponent<PlayerController>();
        _animationController = GetComponent<PlayerAnimationController>();
        _attackExecutor      = GetComponent<AttackExecutor>();

        if (equippedSkills == null || equippedSkills.Length != MaxSlots)
        {
            var resized = new SkillData[MaxSlots];
            if (equippedSkills != null)
                Array.Copy(equippedSkills, resized, Math.Min(equippedSkills.Length, MaxSlots));
            equippedSkills = resized;
        }
    }

    private void OnEnable()
    {
        BindRuntimeServices();
    }

    private void Start()
    {
        // 防止注入晚于 OnEnable 时漏绑 UI
        BindRuntimeServices();
    }

    private void OnDisable()
    {
        if (_input != null)
            _input.SkillSlotTriggered -= OnSkillSlotTriggered;

        _skillAccess?.Unbind(this);
        ClearCastingState();
    }

    private void BindRuntimeServices()
    {
        _skillAccess?.Bind(this);

        if (_input == null) return;
        _input.SkillSlotTriggered -= OnSkillSlotTriggered;
        _input.SkillSlotTriggered += OnSkillSlotTriggered;
    }

    private void Update()
    {
        RecoverEnergy();
        TickCooldowns();
        TickCasting();
    }

    // ── IPlayerSkillReader ─────────────────────────────────────────────────────

    public SkillData GetEquippedSkill(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return null;
        return equippedSkills[slotIndex];
    }

    public float GetCooldownNormalized(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return 0f;
        float duration = _cooldownDuration[slotIndex];
        if (duration <= 0f) return 0f;
        return Mathf.Clamp01(_cooldownRemaining[slotIndex] / duration);
    }

    public string GetHotkeyDisplay(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return string.Empty;
        return _input != null ? _input.GetSkillSlotDisplay(slotIndex) : (slotIndex + 1).ToString();
    }

    public void Refresh()
    {
        OnSlotsChanged?.Invoke();
        for (int i = 0; i < MaxSlots; i++)
            OnSlotCooldownChanged?.Invoke(i);
    }

    // ── 装配 API（供后续学习/配置系统调用）────────────────────────────────────

    public void EquipSkill(int slotIndex, SkillData skill)
    {
        if (!IsValidSlot(slotIndex)) return;
        if (skill != null && _progress != null && _progress.GetSkillRank(skill) == 0) return;
        equippedSkills[slotIndex] = skill;
        _cooldownRemaining[slotIndex] = 0f;
        _cooldownDuration[slotIndex]  = 0f;
        OnSlotsChanged?.Invoke();
        OnSlotCooldownChanged?.Invoke(slotIndex);
    }

    // ── 释放入口 ───────────────────────────────────────────────────────────────

    public bool TryCastSlot(int slotIndex)
    {
        if (!CanCast()) return false;
        if (!IsValidSlot(slotIndex)) return false;

        SkillData skill = equippedSkills[slotIndex];
        if (skill == null) return false;
        if (_progress != null && _progress.GetSkillRank(skill) == 0) return false;
        if (_cooldownRemaining[slotIndex] > 0f) return false;
        if (_stats == null || !_stats.TrySpendEnergy(_progress?.SkillEnergyCost(skill) ?? skill.EnergyCost)) return false;
        TryFinishForLink();
        _player?.CancelCharge();
        _player?.Defense?.SuppressGuard();
        if (_player != null && _player.StateMachine.CurrentState is AttackState)
            _player.StateMachine.ChangeState(new IdleState(_player));
        BeginCast(slotIndex, skill);
        return true;
    }

    public void InterruptSkill(bool force = false) => TryInterruptSkill(force);

    public void ResetForScene()
    {
        InterruptSkill(force: true);
        for (int i = 0; i < MaxSlots; i++)
        {
            _cooldownRemaining[i] = 0f;
            _cooldownDuration[i] = 0f;
            OnSlotCooldownChanged?.Invoke(i);
        }
    }

    public bool TryInterruptSkill(bool force = false)
    {
        if (!_isCasting || _castingSkill == null) return true;
        if (!force && !_castingSkill.CanBeInterrupted) return false;

        Debug.Log($"[PlayerSkillController] 施法中断：{_castingSkill.SkillName}");
        ClearCastingState();
        return true;
    }

    /// <summary>
    /// 技能的唯一动画释放入口；重复事件和动画结束兜底共用同一上下文。
    /// </summary>
    public void NotifySkillHitEvent(AnimationEvent animationEvent = null)
    {
        if (!_waitingForAnimationEvent || !OwnsAnimationEvent(animationEvent)) return;
        ResolveSkillEffect();
    }

    public void NotifySkillAttackEnd(AnimationEvent animationEvent = null)
    {
        if (_effectResolved && OwnsAnimationEvent(animationEvent))
            _attackExecutor?.EndAttack(_attackContext);
    }

    private bool OwnsAnimationEvent(AnimationEvent animationEvent)
    {
        if (!_isCasting || !_animationStarted || _castingSkill?.Animation == null || _player == null) return false;
        // 淡入时上一段动画的迟到事件不能触发新技能。
        AnimatorStateInfo state = animationEvent != null && animationEvent.isFiredByAnimator
            ? animationEvent.animatorStateInfo
            : _player.animator.GetCurrentAnimatorStateInfo(0);
        string expected = _castingSkill.Animation.AnimatorStateName;
        return state.IsName(expected) || state.shortNameHash == Animator.StringToHash(expected);
    }

    public bool IsWaitingForSkillHit => _isCasting && _waitingForAnimationEvent;

    // ── 内部流程 ───────────────────────────────────────────────────────────────

    private void OnSkillSlotTriggered(int slotIndex)
    {
        if (_player != null) _player.RequestSkill(slotIndex);
        else TryCastSlot(slotIndex);
    }

    private bool CanCast()
    {
        if (_input != null && !_input.GameplayInputEnabled) return false;
        if (!CanLink) return false;
        if (_player != null && (!_player.isActiveAndEnabled || !_player.CanReceiveCombatEffects || !_player.IsGrounded)) return false;
        if (_player != null && _player.StateMachine.CurrentState is AttackState attack && !attack.CanLink) return false;
        if (_player != null && _player.Weapons != null && _player.Weapons.IsBusy) return false;
        if (_player != null && (_player.IsDead || !_player.CombatAllowed)) return false;
        if (_player != null && (_player.IsRolling || _player.IsJumping)) return false;
        if (_player != null && _player.StateMachine?.CurrentState is HurtState) return false;
        return true;
    }

    private void BeginCast(int slotIndex, SkillData skill)
    {
        _isCasting                = true;
        _castingSkill             = skill;
        _castTimer                = 0f;
        _castDuration             = Mathf.Max(0f, skill.CastTime);
        _waitingForAnimationEvent = false;
        _effectResolved           = false;
        _animationStarted         = false;

        StartCooldown(slotIndex, skill.CooldownTime);

        _attackContext = _attackExecutor?.CreateScaledAttack(skill.AttackProfile, _progress?.SkillDamageMultiplier(skill) ?? 1f);
        if (_attackContext != null) { _attackContext.IsSkill = true; _attackContext.Element = skill.Element; }

        SpawnCastEffect(skill);

        // 吟唱结束后再播放技能动画，避免命中事件早于允许释放的时间。
        if (_castDuration <= 0f) StartSkillExecution();
    }

    private void StartSkillExecution()
    {
        SkillData skill = _castingSkill;
        if (skill.Animation != null && _animationController != null)
            _animationStarted = _animationController.PlayAnimation(skill.Animation.AnimationName);

        // 占用动画的技能结束旧普攻；无动画即时技能可与已发射效果并存。
        if (_animationStarted && _player != null && _player.StateMachine.CurrentState is AttackState)
            _player.StateMachine.ChangeState(new IdleState(_player));

        _waitingForAnimationEvent = _animationStarted
            && skill.TriggerTiming == SkillTriggerTiming.OnAnimationEvent;
        if (!_waitingForAnimationEvent && !(_animationStarted && skill.TriggerTiming == SkillTriggerTiming.AtAnimationTime))
        {
            ResolveSkillEffect();
            if (!_animationStarted) FinishCast();
        }
    }

    private void TickCasting()
    {
        if (!_isCasting || _castingSkill == null) return;

        if (_castDuration > 0f)
        {
            _castTimer += Time.deltaTime;
            if (_castTimer < _castDuration) return;

            _castDuration = 0f;
            StartSkillExecution();
            return;
        }

        if (_animationStarted && _animationController != null && _castingSkill.TriggerTiming == SkillTriggerTiming.AtAnimationTime
            && _animationController.NormalizedProgress >= _castingSkill.ReleaseNormalizedTime) ResolveSkillEffect();

        if (!_animationStarted || _animationController == null || !_animationController.IsPlayingAnimation())
        {
            // 动画结束仍未收到事件时兜底结算，避免空放
            if (!_effectResolved)
                ResolveSkillEffect();
            FinishCast();
        }
    }

    private void ResolveSkillEffect()
    {
        if (_effectResolved || _castingSkill == null) return;

        if (_attackExecutor == null || _castingSkill.AttackProfile == null)
        {
            Debug.LogWarning(
                $"[PlayerSkillController] 技能 {_castingSkill.SkillName} 缺少 AttackExecutor 或 AttackProfile。",
                this);
            _effectResolved = true;
            return;
        }

        if (_castingSkill.SummonPrefab != null) _player?.ClassCombat?.Summon(_castingSkill, _progress?.GetSkillRank(_castingSkill) ?? 1);
        _attackExecutor.ReleaseAttack(_attackContext);
        _effectResolved = true;
    }

    private void FinishCast()
    {
        _attackExecutor?.EndAttack(_attackContext);
        _attackContext            = null;
        _animationStarted         = false;
        _isCasting                = false;
        _castingSkill             = null;
        _castTimer                = 0f;
        _castDuration             = 0f;
        _waitingForAnimationEvent = false;
        _effectResolved           = false;
    }

    private void ClearCastingState()
    {
        if (_animationStarted && _animationController != null
            && _animationController.CurrentAnimationName() == _castingSkill?.Animation?.AnimationName)
            _animationController.StopAnimation();
        FinishCast();
    }

    private void StartCooldown(int slotIndex, float duration)
    {
        _cooldownDuration[slotIndex]  = Mathf.Max(0f, duration);
        _cooldownRemaining[slotIndex] = _cooldownDuration[slotIndex];
        OnSlotCooldownChanged?.Invoke(slotIndex);
    }

    private void TickCooldowns()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (_cooldownRemaining[i] <= 0f) continue;

            _cooldownRemaining[i] -= Time.deltaTime;
            if (_cooldownRemaining[i] < 0f)
                _cooldownRemaining[i] = 0f;

            OnSlotCooldownChanged?.Invoke(i);
        }
    }

    private void RecoverEnergy()
    {
        if (_stats == null || energyRecoveryRate <= 0f || _player != null && (_player.IsCharging || _player.Defense != null && _player.Defense.IsGuarding)) return;
        _stats.RecoverEnergy(energyRecoveryRate * Time.deltaTime);
    }

    private void SpawnCastEffect(SkillData skill)
    {
        if (skill.EffectPrefab == null) return;

        Vector3 pos = transform.position + transform.rotation * skill.EffectOffset;
        GameObject effect = Instantiate(skill.EffectPrefab, pos, transform.rotation);
        Destroy(effect, 5f);
    }

    private static bool IsValidSlot(int slotIndex) => slotIndex >= 0 && slotIndex < MaxSlots;
}
