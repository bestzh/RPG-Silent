# RPG-Silent 重构方案文档

> **目标架构**：Clean Architecture + VContainer 依赖注入 + ScriptableObject 事件总线  
> **适用项目**：`e:/project/RPG-Silent`  
> **撰写日期**：2026-06-09  

---

## 目录

1. [现状问题分析](#一现状问题分析)
2. [目标架构概述](#二目标架构概述)
3. [目标目录结构](#三目标目录结构)
4. [各层职责定义](#四各层职责定义)
5. [分阶段重构计划](#五分阶段重构计划)
   - [Phase 0：安装 VContainer](#phase-0安装-vcontainer)
   - [Phase 1：抽取 Domain 层](#phase-1抽取-domain-层)
   - [Phase 2：建立事件总线](#phase-2建立-so-事件总线)
   - [Phase 3：配置 VContainer DI](#phase-3配置-vcontainer-di)
   - [Phase 4：改造基础服务](#phase-4改造基础服务infrastructure)
   - [Phase 5：改造 UI 层](#phase-5改造-ui-层)
   - [Phase 6：改造 PlayerController](#phase-6改造-playercontroller)
   - [Phase 7：改造 EnemyController](#phase-7改造-enemycontroller)
   - [Phase 8：提取 Application 用例层](#phase-8提取-application-用例层)
6. [关键文件改造对照表](#六关键文件改造对照表)
7. [重构优先级总览](#七重构优先级总览)
8. [架构收益对比](#八架构收益对比)
9. [注意事项与风险](#九注意事项与风险)

---

## 一、现状问题分析

### 1.1 `PlayerController` 职责过重（God Object）

```csharp
// 当前：一个类同时承担 数据存储 + 输入处理 + 状态机驱动 + 对外事件
public class PlayerController : MonoBehaviour
{
    public int MaxHealth = 100;          // ← 数据（应属于 Model 层）
    public int CurrentHealth { get; }    // ← 数据
    public int Gold { get; }             // ← 数据
    public event Action<int,int> HealthChanged; // ← 数据事件

    // 同时还处理输入、状态机、移动速度...
}
```

**问题**：数据、逻辑、输入三者混杂，存档/网络同步/单元测试均困难。

### 1.2 单例泛滥

```csharp
UIManager.Instance.OpenUI("UI/StartUI");       // StartUI.cs
SceneLoaderManager.Instance.LoadScene(...);    // LoadingUI.cs
InputManager.Instance.MoveInput               // PlayerController.cs
UIManager.Instance.CloseUI("UI/SettingsUI");  // SettingsUI.cs
```

6 处单例直接访问，高度耦合，无法替换实现，无法测试。

### 1.3 UI 直接查找场景对象

```csharp
// MainUI.cs
GameObject playerObject = GameObject.FindWithTag("Player"); // ← 运行时搜索
player = playerObject.GetComponent<PlayerController>();     // ← 直接依赖具体类
```

**问题**：Tag 拼错或对象不存在时静默失败，UI 与玩家逻辑强耦合。

### 1.4 敌人直接依赖玩家具体类

```csharp
// EnemyController.cs
target.GetComponent<PlayerController>()?.TakeDamage(damage); // ← 依赖具体实现
player?.AddReward(rewardGold, rewardExp);                    // ← 依赖具体实现
```

### 1.5 技能系统遗留硬编码

```csharp
// SkillCastManager.cs
if (Input.GetKeyDown(KeyCode.Q)) TryCastSkill("Fireball");  // ← 硬编码按键
if (Input.GetKeyDown(KeyCode.E)) TryCastSkill("SwordSlash");// ← 硬编码技能名
```

### 1.6 无法进行单元测试

所有逻辑都在 `MonoBehaviour` 内，必须启动 Unity 运行时才能测试任何功能。

---

## 二、目标架构概述

```
┌────────────────────────────────────────────────────────────┐
│                    Presentation 层                          │
│  UI (MainUI / StartUI / ...)   Player View / Enemy View    │
│          ↓ 依赖接口                    ↓ 依赖接口            │
├────────────────────────────────────────────────────────────┤
│                    Application 层（用例）                    │
│  PlayerTakeDamageUseCase  PlayerAddRewardUseCase  ...       │
│          ↓ 依赖接口                                          │
├────────────────────────────────────────────────────────────┤
│                     Domain 层（领域核心）                    │
│  PlayerStats   EnemyStats   IDamageable   IRewardable       │
│           纯 C# 类，零 Unity 依赖，可直接单元测试             │
├────────────────────────────────────────────────────────────┤
│                   Infrastructure 层                          │
│  UIService   SceneLoader   InputService   AudioService      │
│        实现接口，封装 Unity API（Addressables 等）            │
├────────────────────────────────────────────────────────────┤
│                       DI 容器（VContainer）                  │
│       GameLifetimeScope   SceneLifetimeScope                │
│            管理所有对象的生命周期与依赖注入                    │
└────────────────────────────────────────────────────────────┘

横切关注点（所有层均可使用）：
  ScriptableObject 事件总线  ←  解耦跨层通信
```

**核心原则**：
- **依赖方向**：外层依赖内层，内层不感知外层
- **Domain 层零 Unity 依赖**：可以在纯 C# 项目中运行和测试
- **接口隔离**：通过接口通信，不依赖具体实现
- **DI 替代单例**：对象生命周期由 VContainer 统一管理

---

## 三、目标目录结构

```
Assets/Scripts/
│
├── Domain/                             ← 领域核心层（纯 C#）
│   ├── Models/
│   │   ├── PlayerStats.cs             ← 玩家数据模型（HP/Gold/Exp）
│   │   └── EnemyStats.cs              ← 敌人数据模型
│   └── Interfaces/
│       ├── IDamageable.cs             ← 可受伤接口
│       ├── IRewardable.cs             ← 可发放奖励接口
│       ├── IPlayerStatsReader.cs      ← 只读玩家数据接口（供 UI 使用）
│       ├── ISceneLoader.cs            ← 场景加载接口
│       ├── IUIService.cs              ← UI 服务接口
│       └── IInputService.cs           ← 输入服务接口
│
├── Application/                        ← 用例层（游戏逻辑编排）
│   ├── PlayerTakeDamageUseCase.cs
│   ├── PlayerAddRewardUseCase.cs
│   └── PlayerHealUseCase.cs
│
├── Infrastructure/                     ← 基础设施层（实现接口，封装 Unity API）
│   ├── UI/
│   │   ├── UIBase.cs                  ← 保留，轻微修改
│   │   └── UIService.cs               ← 原 UIManager，实现 IUIService
│   ├── Scene/
│   │   └── SceneLoader.cs             ← 原 SceneLoaderManager，实现 ISceneLoader
│   ├── Input/
│   │   └── InputService.cs            ← 原 InputManager，实现 IInputService
│   └── Audio/
│       └── AudioService.cs            ← 预留
│
├── Presentation/                       ← 表现层
│   └── UI/
│       ├── StartUI.cs                 ← 改用注入，去掉 Instance
│       ├── LoadingUI.cs               ← 改用注入
│       ├── MainUI.cs                  ← 订阅 PlayerStats 事件，不再 FindWithTag
│       ├── SettingsUI.cs              ← 改用注入
│       └── SettingPage/
│           ├── ScreenPage.cs
│           ├── SoundPage.cs
│           ├── ControllerPage.cs
│           └── GamePage.cs
│
├── Player/                             ← 玩家逻辑（精简职责）
│   ├── PlayerController.cs            ← 仅负责：输入 + FSM 驱动 + 物理
│   ├── PlayerStateMachine.cs          ← 不变
│   ├── PlayerStance.cs                ← 不变
│   ├── PlayerStanceController.cs      ← 不变
│   ├── StanceDatabase.cs              ← 不变
│   ├── PlayerAnimationEventReceiver.cs← 不变
│   ├── CameraControl.cs               ← 不变
│   └── States/                        ← 不变
│       ├── PlayerState.cs
│       ├── IdleState.cs ... DeadState.cs
│
├── Enemy/                              ← 敌人（从 Player/ 独立出来）
│   └── EnemyController.cs             ← 改用 IDamageable 接口
│
├── Combat/                             ← 不变（AttackExecutor 接口化）
├── Animation/                          ← 不变
│
├── Events/                             ← SO 事件总线（新增）
│   ├── Core/
│   │   ├── GameEvent.cs               ← 无参数 SO 事件
│   │   ├── GameEventListener.cs       ← 无参数监听器组件
│   │   ├── IntGameEvent.cs            ← int 参数 SO 事件
│   │   ├── IntGameEventListener.cs
│   │   ├── FloatGameEvent.cs
│   │   └── Vector2GameEvent.cs
│   └── Channels/                      ← 各功能事件 .asset（在这里创建资产）
│       （示例：OnPlayerDead.asset, OnSceneLoad.asset...）
│
├── DI/                                 ← VContainer 依赖注入配置
│   ├── GameLifetimeScope.cs           ← 全局作用域（随 DontDestroyOnLoad）
│   └── SceneLifetimeScope.cs          ← 场景级作用域（随场景销毁）
│
├── Editor/                             ← 不变
│   └── StanceOverrideAutoFiller.cs
│
└── Common/
    └── Singleton.cs                   ← 逐步废弃（保留兼容期）
```

---

## 四、各层职责定义

| 层 | 职责 | 允许依赖 | 禁止依赖 |
|---|---|---|---|
| **Domain** | 纯业务数据和规则，零框架依赖 | 无（自洽） | UnityEngine、任何其他层 |
| **Application** | 编排用例，调用 Domain | Domain 接口 | Infrastructure 具体实现、MonoBehaviour |
| **Infrastructure** | 实现接口，封装 Unity API | Domain 接口、UnityEngine | Application、Presentation |
| **Presentation** | UI 显示、用户输入响应 | Application UseCase、Domain 接口 | Infrastructure 具体类 |
| **DI** | 组装所有对象 | 所有层 | 业务逻辑 |

---

## 五、分阶段重构计划

---

### Phase 0：安装 VContainer

**方式一（推荐）**：`Package Manager → Add package from git URL`

```
https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.16.6
```

**方式二**：下载 `.unitypackage` 从 [VContainer Releases](https://github.com/hadashiA/VContainer/releases)

**验证安装**：新建脚本，输入 `using VContainer;` 无报错即安装成功。

---

### Phase 1：抽取 Domain 层

**目标**：把 `PlayerController` 中的数据和事件移入独立的纯 C# 类。

#### 新建 `Assets/Scripts/Domain/Models/PlayerStats.cs`

```csharp
using System;

namespace RPGSilent.Domain
{
    /// <summary>
    /// 玩家核心数据模型。纯 C# 类，无 Unity 依赖，可直接单元测试。
    /// </summary>
    public class PlayerStats
    {
        public int MaxHealth  { get; private set; }
        public int CurrentHealth { get; private set; }
        public int Gold       { get; private set; }
        public int Exp        { get; private set; }

        public bool IsDead => CurrentHealth <= 0;

        public event Action<int, int> OnHealthChanged;  // (current, max)
        public event Action<int>      OnGoldChanged;
        public event Action<int>      OnExpChanged;

        public PlayerStats(int maxHealth)
        {
            MaxHealth     = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            if (IsDead || damage <= 0) return;
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;
            Exp += amount;
            OnExpChanged?.Invoke(Exp);
        }

        public void NotifyAll()
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnGoldChanged?.Invoke(Gold);
            OnExpChanged?.Invoke(Exp);
        }
    }
}
```

#### 新建 `Assets/Scripts/Domain/Interfaces/IDamageable.cs`

```csharp
namespace RPGSilent.Domain
{
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(int damage);
    }
}
```

#### 新建 `Assets/Scripts/Domain/Interfaces/IRewardable.cs`

```csharp
namespace RPGSilent.Domain
{
    public interface IRewardable
    {
        void AddReward(int gold, int exp);
    }
}
```

#### 新建 `Assets/Scripts/Domain/Interfaces/IPlayerStatsReader.cs`

```csharp
using System;

namespace RPGSilent.Domain
{
    /// <summary>
    /// UI 层只读玩家数据的接口，避免 UI 直接操作 PlayerStats。
    /// </summary>
    public interface IPlayerStatsReader
    {
        int MaxHealth     { get; }
        int CurrentHealth { get; }
        int Gold          { get; }
        int Exp           { get; }
        bool IsDead       { get; }

        event Action<int, int> OnHealthChanged;
        event Action<int>      OnGoldChanged;
        event Action<int>      OnExpChanged;
    }
}
```

让 `PlayerStats` 实现该接口：

```csharp
// PlayerStats.cs 修改
public class PlayerStats : IPlayerStatsReader
{
    // ... 已有内容不变，类声明改为：
}
```

---

### Phase 2：建立 SO 事件总线

**目标**：用 ScriptableObject 作为事件频道，完全解耦跨模块通信。

#### 新建 `Assets/Scripts/Events/Core/GameEvent.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace RPGSilent.Events
{
    [CreateAssetMenu(fileName = "GameEvent", menuName = "Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<GameEventListener> _listeners = new();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised();
        }

        public void Register(GameEventListener listener)   => _listeners.Add(listener);
        public void Unregister(GameEventListener listener) => _listeners.Remove(listener);
    }
}
```

#### 新建 `Assets/Scripts/Events/Core/GameEventListener.cs`

```csharp
using UnityEngine;
using UnityEngine.Events;

namespace RPGSilent.Events
{
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent gameEvent;
        [SerializeField] private UnityEvent response;

        private void OnEnable()  => gameEvent?.Register(this);
        private void OnDisable() => gameEvent?.Unregister(this);

        public void OnEventRaised() => response?.Invoke();
    }
}
```

#### 新建带参数版本 `Assets/Scripts/Events/Core/IntGameEvent.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace RPGSilent.Events
{
    [CreateAssetMenu(fileName = "IntGameEvent", menuName = "Events/Int Game Event")]
    public class IntGameEvent : ScriptableObject
    {
        private readonly List<System.Action<int>> _listeners = new();

        public void Raise(int value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i]?.Invoke(value);
        }

        public void Register(System.Action<int> listener)   => _listeners.Add(listener);
        public void Unregister(System.Action<int> listener) => _listeners.Remove(listener);
    }
}
```

**在 Inspector 中创建事件资产**（`右键 → Create → Events → ...`）：

```
Assets/Scripts/Events/Channels/
    OnPlayerDead.asset          (GameEvent)
    OnPlayerHealthChanged.asset (IntGameEvent)
    OnPlayerGoldChanged.asset   (IntGameEvent)
    OnSceneLoadComplete.asset   (GameEvent)
```

---

### Phase 3：配置 VContainer DI

**目标**：用 VContainer 管理所有服务的生命周期，彻底替代单例。

#### 新建 `Assets/Scripts/DI/GameLifetimeScope.cs`

> 挂在 `DontDestroyOnLoad` 的 GameObject 上，随游戏全局存在。

```csharp
using RPGSilent.Infrastructure;
using RPGSilent.Domain;
using VContainer;
using VContainer.Unity;

namespace RPGSilent.DI
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 基础设施服务（全局单例）
            builder.RegisterComponentInNewPrefab<UIService>(...)
                   .As<IUIService>()
                   .DontDestroyOnLoad();
            
            builder.Register<SceneLoader>(Lifetime.Singleton).As<ISceneLoader>();
            builder.Register<InputService>(Lifetime.Singleton).As<IInputService>();
        }
    }
}
```

#### 新建 `Assets/Scripts/DI/SceneLifetimeScope.cs`

> 挂在游戏场景中的 GameObject 上，随场景销毁。

```csharp
using RPGSilent.Application;
using RPGSilent.Domain;
using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace RPGSilent.DI
{
    public class SceneLifetimeScope : LifetimeScope
    {
        [SerializeField] private int playerMaxHealth = 100;

        protected override void Configure(IContainerBuilder builder)
        {
            // 玩家数据 Model（场景级，随场景销毁）
            builder.Register<PlayerStats>(
                _ => new PlayerStats(playerMaxHealth),
                Lifetime.Scoped
            ).As<IPlayerStatsReader>();

            // 用例（依赖自动注入）
            builder.Register<PlayerTakeDamageUseCase>(Lifetime.Scoped);
            builder.Register<PlayerAddRewardUseCase>(Lifetime.Scoped);
            builder.Register<PlayerHealUseCase>(Lifetime.Scoped);
        }
    }
}
```

**Unity 场景配置**：
1. 创建空 GameObject `[GameScope]`，挂 `GameLifetimeScope`
2. 创建空 GameObject `[SceneScope]`，挂 `SceneLifetimeScope`，Parent 设为 `[GameScope]`

---

### Phase 4：改造基础服务（Infrastructure）

#### 新建 `Assets/Scripts/Domain/Interfaces/IUIService.cs`

```csharp
using System;

namespace RPGSilent.Domain
{
    public interface IUIService
    {
        void OpenUI(string uiKey, params object[] args);
        void CloseUI(string uiKey);
        void CloseAllUI();
        void PreloadUI(string uiKey, Action onComplete = null);
    }
}
```

#### 改造 `UIManager.cs` → `Infrastructure/UI/UIService.cs`

```csharp
// 主要改动：
// 1. 类名改为 UIService
// 2. 去掉 public static UIManager Instance
// 3. 实现 IUIService 接口
// 4. 由 VContainer 注册和管理生命周期

public class UIService : MonoBehaviour, IUIService
{
    // ... 原有逻辑完全保留，仅去掉 Instance 相关代码
}
```

#### 新建 `Assets/Scripts/Domain/Interfaces/ISceneLoader.cs`

```csharp
using System;

namespace RPGSilent.Domain
{
    public interface ISceneLoader
    {
        void LoadScene(string key, bool additive = false,
                       Action<float> onProgress = null,
                       Action onComplete = null);
    }
}
```

#### 改造 `SceneLoaderManager.cs` → `Infrastructure/Scene/SceneLoader.cs`

```csharp
// 主要改动：去掉 static Instance，实现 ISceneLoader
public class SceneLoader : MonoBehaviour, ISceneLoader
{
    // 原有逻辑完全保留
}
```

#### 新建 `Assets/Scripts/Domain/Interfaces/IInputService.cs`

```csharp
using UnityEngine;

namespace RPGSilent.Domain
{
    public interface IInputService
    {
        Vector2 MoveInput { get; }
    }
}
```

#### 改造 `InputManager.cs` → `Infrastructure/Input/InputService.cs`

```csharp
public class InputService : MonoBehaviour, IInputService
{
    public Vector2 MoveInput { get; private set; }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(h, v).normalized;
    }
    // 去掉 static Instance
}
```

---

### Phase 5：改造 UI 层

#### 改造 `StartUI.cs`

```csharp
// 改造前
UIManager.Instance.OpenUI("UI/LoadingUI", "Scenes/Main");

// 改造后
using RPGSilent.Domain;
using VContainer;

public class StartUI : UIBase
{
    [Inject] private IUIService _uiService;

    private void Awake()
    {
        StartButton.onClick.AddListener(() =>
        {
            _uiService.OpenUI("UI/LoadingUI", "Scenes/Main");
            _uiService.CloseUI("UI/StartUI");
        });
        SettingsButton.onClick.AddListener(() => _uiService.OpenUI("UI/SettingsUI"));
    }
}
```

#### 改造 `LoadingUI.cs`

```csharp
// 改造前
SceneLoaderManager.Instance.LoadScene(...)
UIManager.Instance.OpenUI(...)

// 改造后
using RPGSilent.Domain;
using VContainer;

public class LoadingUI : UIBase
{
    [Inject] private ISceneLoader _sceneLoader;
    [Inject] private IUIService   _uiService;

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        string nextScene = args.Length > 0 ? args[0] as string : "Scenes/Main";

        _sceneLoader.LoadScene(nextScene, false,
            progress =>
            {
                progressBar.value  = progress;
                progressText.text  = $"{(int)(progress * 100)}%";
            },
            () =>
            {
                _uiService.CloseUI("UI/LoadingUI");
                _uiService.OpenUI("UI/MainUI");
            });
    }
}
```

#### 改造 `MainUI.cs`（最重要）

```csharp
// 改造前：FindWithTag + GetComponent，强耦合
// 改造后：注入接口，零耦合

using RPGSilent.Domain;
using VContainer;

public class MainUI : UIBase
{
    // 不再 FindWithTag！注入只读接口
    [Inject] private IPlayerStatsReader _stats;

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        _stats.OnHealthChanged += OnHealthChanged;
        _stats.OnGoldChanged   += UpdateGold;
        _stats.OnExpChanged    += UpdateExp;
        _stats.NotifyAll(); // 初始刷新（需在接口中添加此方法）
    }

    public override void OnClose()
    {
        base.OnClose();
        if (_stats == null) return;
        _stats.OnHealthChanged -= OnHealthChanged;
        _stats.OnGoldChanged   -= UpdateGold;
        _stats.OnExpChanged    -= UpdateExp;
    }

    private void OnHealthChanged(int current, int max)
    {
        if (hpBar != null) hpBar.value = max > 0 ? (float)current / max : 0f;
    }

    private void UpdateGold(int gold)
    {
        if (goldText != null) goldText.text = $"{gold}";
    }

    private void UpdateExp(int exp)
    {
        if (moneyText != null) moneyText.text = $"{exp}";
    }
}
```

#### 改造 `SettingsUI.cs`

```csharp
// 改造前
UIManager.Instance.OpenUI("UI/StartUI");

// 改造后
[Inject] private IUIService _uiService;

private void OnBackButtonClicked()
{
    _uiService.OpenUI("UI/StartUI");
    _uiService.CloseUI("UI/SettingsUI");
}
```

---

### Phase 6：改造 PlayerController

**改造目标**：`PlayerController` 只保留 **输入处理 + FSM 驱动 + 物理移动**，数据操作全部委托给 UseCase。

```csharp
using RPGSilent.Application;
using RPGSilent.Domain;
using VContainer;
using UnityEngine;

public class PlayerController : MonoBehaviour, IDamageable, IRewardable
{
    // ── 注入依赖（由 VContainer 自动注入）──────────────────────
    [Inject] private IInputService             _inputService;
    [Inject] private PlayerTakeDamageUseCase   _takeDamageUseCase;
    [Inject] private PlayerAddRewardUseCase    _addRewardUseCase;

    // ── 数据只读（从 Domain Model 读取）────────────────────────
    [Inject] private IPlayerStatsReader        _stats;

    // ── 玩法组件（保留在这里）──────────────────────────────────
    public PlayerStateMachine StateMachine { get; private set; }
    public Animator animator;
    public Rigidbody rb;

    public bool IsDead => _stats.IsDead;

    // 移动相关（本地数据，非持久化，保留在 Controller）
    public bool IsJumping  { get; private set; }
    public bool IsRolling  { get; private set; }
    public Vector2 InputDir => _inputService.MoveInput;

    public float WalkSpeed   = 2.5f;
    public float MoveSpeed   = 5f;
    public float SprintSpeed = 8f;

    // ... 其余 InputAction 注册逻辑保持不变

    private void Awake()
    {
        StateMachine        = new PlayerStateMachine();
        // GetComponent 调用保留（这是 Unity 组件获取，不是业务耦合）
        rb       = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        SetupInputActions();
    }

    private void Start()
    {
        StateMachine.ChangeState(new IdleState(this));
    }

    // ── 接口实现 ────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        _takeDamageUseCase.Execute(damage);     // 数据逻辑交给 UseCase
        GetComponent<SkillCastManager>()?.InterruptSkill();

        if (_stats.IsDead)
            StateMachine.ChangeState(new DeadState(this));
        else
            StateMachine.ChangeState(new HurtState(this));
    }

    public void AddReward(int gold, int exp)
    {
        _addRewardUseCase.Execute(gold, exp);   // 数据逻辑交给 UseCase
    }

    // ... Update / Input 处理逻辑基本不变
}
```

---

### Phase 7：改造 EnemyController

**目标**：用接口取代对 `PlayerController` 的直接依赖。

```csharp
// 改造前
private Transform target;
target = GameObject.FindWithTag("Player")?.transform;
target.GetComponent<PlayerController>()?.TakeDamage(damage);
player?.AddReward(rewardGold, rewardExp);

// 改造后
private IDamageable  _targetDamageable;
private IRewardable  _targetRewardable;

private void Start()
{
    // 仍可用 FindWithTag 找 GameObject（场景约定），但通过接口交互
    GameObject playerObj = GameObject.FindWithTag("Player");
    if (playerObj != null)
    {
        _targetDamageable = playerObj.GetComponent<IDamageable>();
        _targetRewardable = playerObj.GetComponent<IRewardable>();
    }
}

private void Attack()
{
    _targetDamageable?.TakeDamage(damage);
}

private void GrantReward()
{
    _targetRewardable?.AddReward(rewardGold, rewardExp);
}
```

> **敌人自身也实现 IDamageable 接口**，供 `AttackExecutor` 统一调用：

```csharp
public class EnemyController : MonoBehaviour, IDamageable
{
    public bool IsDead { get; private set; }
    
    public void TakeDamage(int damage) { ... }
}
```

同时 `AttackExecutor.ApplyDamage` 改为接收 `IDamageable`：

```csharp
// 改造前
public void ApplyDamage(EnemyController enemy, AttackProfile profile, Vector3 hitPoint)

// 改造后
public void ApplyDamage(IDamageable target, AttackProfile profile, Vector3 hitPoint)
```

---

### Phase 8：提取 Application 用例层

#### `Assets/Scripts/Application/PlayerTakeDamageUseCase.cs`

```csharp
using RPGSilent.Domain;
using UnityEngine;

namespace RPGSilent.Application
{
    public class PlayerTakeDamageUseCase
    {
        private readonly PlayerStats _stats;

        public PlayerTakeDamageUseCase(PlayerStats stats)
        {
            _stats = stats;
        }

        public void Execute(int damage)
        {
            if (damage <= 0) return;
            _stats.TakeDamage(damage);
            Debug.Log($"Player took {damage} dmg. HP: {_stats.CurrentHealth}/{_stats.MaxHealth}");
        }
    }
}
```

#### `Assets/Scripts/Application/PlayerAddRewardUseCase.cs`

```csharp
using RPGSilent.Domain;
using UnityEngine;

namespace RPGSilent.Application
{
    public class PlayerAddRewardUseCase
    {
        private readonly PlayerStats _stats;

        public PlayerAddRewardUseCase(PlayerStats stats)
        {
            _stats = stats;
        }

        public void Execute(int gold, int exp)
        {
            if (gold > 0) _stats.AddGold(gold);
            if (exp  > 0) _stats.AddExp(exp);
            Debug.Log($"Reward: +{gold} gold, +{exp} exp. Total: {_stats.Gold}/{_stats.Exp}");
        }
    }
}
```

#### `Assets/Scripts/Application/PlayerHealUseCase.cs`

```csharp
using RPGSilent.Domain;

namespace RPGSilent.Application
{
    public class PlayerHealUseCase
    {
        private readonly PlayerStats _stats;

        public PlayerHealUseCase(PlayerStats stats)
        {
            _stats = stats;
        }

        public void Execute(int amount)
        {
            if (amount <= 0) return;
            _stats.Heal(amount);
        }
    }
}
```

---

## 六、关键文件改造对照表

| 原文件 | 改造方式 | 新位置 |
|--------|----------|--------|
| `Manager/UIManager.cs` | 去掉 `Instance`，实现 `IUIService` | `Infrastructure/UI/UIService.cs` |
| `Manager/InputManager.cs` | 去掉 `Instance`，实现 `IInputService` | `Infrastructure/Input/InputService.cs` |
| `Manager/SceneLoaderManager.cs` | 去掉 `Instance`，实现 `ISceneLoader` | `Infrastructure/Scene/SceneLoader.cs` |
| `Manager/UIBase.cs` | 保留，加 VContainer 支持 | `Infrastructure/UI/UIBase.cs` |
| `Manager/ScreenShakeManager.cs` | 去掉 `Instance`，注入使用 | `Infrastructure/Camera/ScreenShakeService.cs` |
| `Player/PlayerController.cs` | 去掉数据字段，改用注入 | `Player/PlayerController.cs`（原地改） |
| `Player/EnemyController.cs` | 依赖 `IDamageable`/`IRewardable` | `Enemy/EnemyController.cs` |
| `UI/MainUI.cs` | 去掉 `FindWithTag`，注入 `IPlayerStatsReader` | `Presentation/UI/MainUI.cs` |
| `UI/StartUI.cs` | `Instance` → `[Inject] IUIService` | `Presentation/UI/StartUI.cs` |
| `UI/LoadingUI.cs` | `Instance` → 注入 | `Presentation/UI/LoadingUI.cs` |
| `UI/SettingsUI.cs` | `Instance` → 注入 | `Presentation/UI/SettingsUI.cs` |
| `GameStart.cs` | 改用注入 | `GameStart.cs`（原地改） |
| `Combat/AttackExecutor.cs` | `ApplyDamage` 参数改为 `IDamageable` | 原地改 |
| `Manager/SkillCastManager.cs` | 硬编码按键抽成配置 | 原地改 |
| ——新增—— | `Domain/Models/PlayerStats.cs` | `Domain/Models/` |
| ——新增—— | `Domain/Interfaces/*.cs`（多个） | `Domain/Interfaces/` |
| ——新增—— | `Application/*UseCase.cs`（多个） | `Application/` |
| ——新增—— | `Events/Core/*.cs`（事件总线） | `Events/Core/` |
| ——新增—— | `DI/GameLifetimeScope.cs` | `DI/` |
| ——新增—— | `DI/SceneLifetimeScope.cs` | `DI/` |

---

## 七、重构优先级总览

| 优先级 | 阶段 | 任务 | 预估耗时 | 收益 | 风险 |
|--------|------|------|----------|------|------|
| ⭐⭐⭐ | Phase 0 | 安装 VContainer | 15 min | 基础准备 | 极低 |
| ⭐⭐⭐ | Phase 1 | 抽取 `PlayerStats` Domain Model | 1 h | 数据解耦，可测试 | 低 |
| ⭐⭐⭐ | Phase 3 | 配置 `GameLifetimeScope` / `SceneLifetimeScope` | 1 h | DI 基础建立 | 低 |
| ⭐⭐⭐ | Phase 4 | 基础服务接口化 + 去单例 | 2 h | 消灭全部 `.Instance` | 中 |
| ⭐⭐ | Phase 5 | 改造 UI 层（注入替代 Instance） | 2 h | UI 层干净 | 低 |
| ⭐⭐ | Phase 6 | 精简 `PlayerController` | 2 h | 最大职责拆分 | 中 |
| ⭐⭐ | Phase 7 | `EnemyController` 接口化 | 1 h | 敌人/玩家解耦 | 低 |
| ⭐ | Phase 8 | 抽取 UseCase 层 | 2 h | 逻辑可单独测试 | 低 |
| ⭐ | Phase 2 | SO 事件总线 | 3 h | 跨模块解耦 | 中 |

**建议顺序**：Phase 0 → 1 → 3 → 4 → 5 → 6 → 7 → 8 → 2

---

## 八、架构收益对比

| 问题 | 重构前 | 重构后 |
|------|--------|--------|
| 玩家数据存储位置 | `PlayerController`（与逻辑混合） | `PlayerStats`（独立纯 C# 类） |
| 访问全局服务方式 | `UIManager.Instance.xxx` | `[Inject] IUIService _uiService` |
| UI 获取玩家数据 | `FindWithTag("Player")` + `GetComponent` | `[Inject] IPlayerStatsReader _stats` |
| 敌人攻击玩家 | 直接调 `PlayerController.TakeDamage` | 调 `IDamageable.TakeDamage` |
| 攻击执行器目标 | `EnemyController` 具体类 | `IDamageable` 接口 |
| 奖励发放 | 直接调 `PlayerController.AddReward` | 调 `IRewardable.AddReward` |
| 单元测试 | 无法测试（依赖 Unity Runtime） | `PlayerStats`、UseCase 可直接 NUnit 测试 |
| 新增敌人类型 | 需继承 `EnemyController` | 实现 `IDamageable` 接口即可 |
| 存档/网络同步 | 需大规模重构 `PlayerController` | 只需序列化/同步 `PlayerStats` |

---

## 九、注意事项与风险

### 9.1 VContainer 在 MonoBehaviour 中的注入方式

VContainer 不支持构造函数注入 `MonoBehaviour`，需使用以下两种方式之一：

```csharp
// 方式一：字段注入（推荐，简洁）
[Inject] private IUIService _uiService;

// 方式二：方法注入（明确，便于测试）
[Inject]
public void Construct(IUIService uiService, ISceneLoader sceneLoader)
{
    _uiService    = uiService;
    _sceneLoader  = sceneLoader;
}
```

### 9.2 LifetimeScope 父子关系

- `GameLifetimeScope`（父）：注册全局服务，`DontDestroyOnLoad`
- `SceneLifetimeScope`（子）：注册场景级对象，场景卸载时自动销毁

子 Scope 可访问父 Scope 的注册，父不能访问子。

### 9.3 `PlayerStats` 在 Scope 中的注册

`PlayerStats` 是 POCO（纯 C# 对象），注册方式：

```csharp
builder.Register<PlayerStats>(
    container => new PlayerStats(100),
    Lifetime.Scoped
);
```

### 9.4 Addressables 与 VContainer 的兼容性

通过 Addressables 动态加载的 UI Prefab 实例化后，VContainer 不会自动注入。  
需要在 `UIService.OpenUI` 完成加载后手动调用：

```csharp
// UIService 中，加载完成回调里：
_container.InjectGameObject(op.Result); // 手动触发注入
```

`_container` 需在 `UIService` 中通过 `[Inject] IObjectResolver _container` 获取。

### 9.5 逐步迁移，保留兼容期

不建议一次性全部重写。建议：
1. 先建立新的 Domain/DI 层，不删除原有代码
2. 新功能全部使用新架构
3. 旧代码在有空时逐步迁移
4. 所有旧的单例 `.Instance` 加 `[Obsolete]` 标记，逐步消灭

### 9.6 FSM States 对 PlayerController 的依赖

所有 `PlayerState` 子类目前通过构造函数持有 `PlayerController` 引用，这部分**暂不需要改动**。FSM 是 `PlayerController` 的内部实现细节，持有同层的引用是合理的。

---

## 附录：推荐参考资料

| 资料 | 链接 |
|------|------|
| VContainer 官方文档 | https://vcontainer.hadashikick.jp |
| VContainer GitHub | https://github.com/hadashiA/VContainer |
| Ryan Hippie - SO Architecture (GDC) | Unity 官方 YouTube 搜索 "Scriptable Object Architecture" |
| Clean Architecture（书籍） | Robert C. Martin《Clean Architecture》 |
| Unity Clean Architecture 示例 | https://github.com/Unity-Technologies/GameDevPatterns |

---

*文档版本：v1.0 | 最后更新：2026-06-09*
