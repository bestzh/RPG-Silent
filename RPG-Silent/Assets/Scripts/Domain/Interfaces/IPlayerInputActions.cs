using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPGSilent.Domain
{
    /// <summary>
    /// 玩家输入动作服务接口：集中管理所有 InputAction，支持运行时改键与持久化。
    /// </summary>
    public interface IPlayerInputActions
    {
        // ── 原始 Action（供 ControllerPage 改键使用）────────────────────────────
        InputAction Move         { get; }
        InputAction Sprint       { get; }
        InputAction Roll         { get; }
        InputAction Jump         { get; }
        InputAction Walk         { get; }
        InputAction Attack       { get; }
        InputAction StanceToggle { get; }
        InputAction Pause        { get; }

        // ── 离散动作事件（PlayerController 订阅）────────────────────────────────
        event Action SprintStarted;
        event Action SprintEnded;
        event Action RollTriggered;
        event Action JumpTriggered;
        event Action WalkStarted;
        event Action WalkEnded;
        event Action AttackTriggered;
        event Action StanceToggleTriggered;
        event Action PauseTriggered;

        // ── 移动轴（InputManager 每帧读取）──────────────────────────────────────
        Vector2 MoveInput { get; }

        // ── 改键 API（ControllerPage 使用）──────────────────────────────────────

        /// <summary>
        /// 启动交互式改键操作。
        /// Sprint 动作改键时会自动将同一路径镜像到 Roll。
        /// </summary>
        void StartRebind(InputAction action, int bindingIndex,
            Action<string> onComplete, Action onCancel = null);

        /// <summary>取消当前正在进行的改键操作。</summary>
        void CancelCurrentRebind();

        /// <summary>获取指定 Action、指定绑定索引的可读按键名称。</summary>
        string GetDisplayString(InputAction action, int bindingIndex);

        // ── 冲刺持定时间（ControllerSettingsService 变更时通知）──────────────────
        void ApplySprintHoldTime(float duration);

        // ── 持久化 ───────────────────────────────────────────────────────────────
        void Save();
        void Load();
        void ResetBindings();

        // ── 暂停时禁用游戏输入 ─────────────────────────────────────────────────────
        void SetGameplayInputEnabled(bool enabled);
    }
}
