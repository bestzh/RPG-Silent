# RPG-Silent 架构说明文档

> **架构**：Clean Architecture + VContainer 依赖注入  
> **项目**：RPG-Silent  
> **版本**：v2.0（重构后）

---

## 目录

1. [架构核心思想](#一架构核心思想)
2. [分层结构详解](#二分层结构详解)
3. [VContainer 框架说明](#三vcontainer-框架说明)
4. [优缺点分析](#四优缺点分析)
5. [为什么选择这个架构](#五为什么选择这个架构)
6. [日常开发使用指南](#六日常开发使用指南)
7. [常见场景示例](#七常见场景示例)
8. [注意事项与规范](#八注意事项与规范)

---

## 一、架构核心思想

### 用一句话概括

> **"外层依赖内层，内层不感知外层，通过接口通信，由容器装配。"**

### 核心原则

| 原则 | 含义 |
|------|------|
| **依赖倒置** | 高层模块（UI）不依赖低层模块（Manager），两者都依赖接口 |
| **单一职责** | 每个类只做一件事，数据归 Model，逻辑归 UseCase，显示归 View |
| **开闭原则** | 对扩展开放（加新接口实现），对修改关闭（不改现有代码） |
| **接口隔离** | 通过接口暴露功能，隐藏实现细节 |

### 重构前 vs 重构后的核心变化

```
重构前：
  PlayerController  ←──── MainUI (FindWithTag)
       ↑                      |
  EnemyController.TakeDamage  |
       ↑                      ↓
  UIManager.Instance      PlayerController

重构后：
  PlayerStats (Domain)  ←── PlayerTakeDamageUseCase
       ↑                           ↑
  IPlayerStatsReader    [Inject] PlayerController
       ↑                           ↑
  [Inject] MainUI         [Inject] EnemyController (→ IDamageable)
```

---

## 二、分层结构详解

```
Assets/Scripts/
│
├── Domain/          ← 第1层：领域核心（最内层，零依赖）
├── Application/     ← 第2层：用例层（依赖 Domain 接口）
├── Infrastructure/  ← 第3层：基础设施（实现接口，封装 Unity API）
├── Presentation/    ← 第4层：表现层（UI + 视图，最外层）
└── DI/              ← 装配层（把所有层组装起来）
```

### Domain 层（`Assets/Scripts/Domain/`）

**职责**：定义核心数据和规则，**零 Unity 依赖**，可直接写单元测试。

```
Domain/
├── Models/
│   └── PlayerStats.cs      ← 玩家数据（HP、金币、经验）
└── Interfaces/
    ├── IDamageable.cs      ← 可受伤接口
    ├── IRewardable.cs      ← 可发放奖励接口
    ├── IPlayerStatsReader.cs ← 只读玩家数据接口
    ├── IUIService.cs       ← UI 服务接口
    ├── ISceneLoader.cs     ← 场景加载接口
    └── IInputService.cs    ← 输入服务接口
```

**特征**：
- 纯 C# 类，没有 `using UnityEngine`
- 只包含数据结构和接口定义
- 任何人都可以理解，不需要懂 Unity

**示例**：

```csharp
// PlayerStats.cs —— 纯数据，不关心谁用它、在哪里显示
public class PlayerStats : IPlayerStatsReader
{
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public event Action<int, int> OnHealthChanged;

    public void TakeDamage(int damage)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
```

---

### Application 层（`Assets/Scripts/Application/`）

**职责**：编排具体的游戏逻辑（用例），每个 UseCase 只做一件事。

```
Application/
├── PlayerTakeDamageUseCase.cs  ← 玩家受伤逻辑
├── PlayerAddRewardUseCase.cs   ← 玩家获得奖励逻辑
└── PlayerHealUseCase.cs        ← 玩家治疗逻辑
```

**特征**：
- 纯 C# 类（不是 MonoBehaviour）
- 通过构造函数接收 Domain 层对象
- 逻辑可脱离 Unity Runtime 单独测试

**示例**：

```csharp
// PlayerTakeDamageUseCase.cs
public class PlayerTakeDamageUseCase
{
    private readonly PlayerStats _stats;   // 构造时注入

    public PlayerTakeDamageUseCase(PlayerStats stats) { _stats = stats; }

    public void Execute(int damage)
    {
        if (damage <= 0) return;
        _stats.TakeDamage(damage);
        Debug.Log($"玩家受伤 -{damage}，剩余 {_stats.CurrentHealth}");
    }
}
```

---

### Infrastructure 层（`Assets/Scripts/Manager/`）

**职责**：实现 Domain 接口，封装 Unity 具体 API（Addressables、NavMesh 等）。

```
Manager/
├── UIManager.cs          ← 实现 IUIService（Addressables 加载 UI）
├── InputManager.cs       ← 实现 IInputService（读取键鼠输入）
└── SceneLoaderManager.cs ← 实现 ISceneLoader（Addressables 加载场景）
```

**特征**：
- 是 MonoBehaviour，可以挂在 GameObject 上
- **只实现接口，不暴露自身类型**（外部代码只知道 `IUIService`，不知道 `UIManager`）
- 可以随时替换实现（例如换成网络加载 UI）

---

### Presentation 层（`Assets/Scripts/UI/` + `Player/`）

**职责**：显示数据、响应用户输入，不包含业务逻辑。

```
UI/
├── MainUI.cs      ← 订阅 IPlayerStatsReader 事件，更新血条/金币
├── StartUI.cs     ← 按钮点击 → 调用 IUIService 切换界面
├── LoadingUI.cs   ← 调用 ISceneLoader 加载场景
└── SettingsUI.cs  ← Tab 切换逻辑

Player/
└── PlayerController.cs ← 输入采集 + FSM 驱动 + 调用 UseCase
```

**特征**：
- 只依赖接口（`IUIService`、`IPlayerStatsReader` 等），不依赖具体实现
- 通过 `[Inject]` 接收依赖，不主动查找

---

### DI 层（`Assets/Scripts/DI/`）

**职责**：将所有层"组装"在一起，是唯一知道所有实现细节的地方。

```
DI/
├── GameLifetimeScope.cs   ← 全局容器（初始场景）
└── SceneLifetimeScope.cs  ← 游戏场景容器
```

---

## 三、VContainer 框架说明

### VContainer 是什么

VContainer 是 Unity 专用的**依赖注入（DI）框架**。

**依赖注入** = 你的类不自己创建所需的对象，而是由外部"注入"给你。

```csharp
// 传统写法（自己查找）：
private UIManager _ui;
void Awake() { _ui = UIManager.Instance; }  // 主动查找单例

// VContainer 写法（被动接收）：
[Inject] private IUIService _ui;            // VContainer 自动注入
```

### LifetimeScope（作用域容器）

VContainer 通过 `LifetimeScope` 管理对象的生命周期：

| 作用域 | 对应组件 | 生命周期 | 用途 |
|--------|----------|----------|------|
| `GameLifetimeScope` | 初始场景 | 全局（DontDestroyOnLoad）| 全局服务（UI/Input/Scene） |
| `SceneLifetimeScope` | 游戏场景 | 随场景 | 玩家数据、用例 |

**父子关系**：
```
GameLifetimeScope（父）
  └── SceneLifetimeScope（子）
        子容器可以访问父容器的所有注册
        父容器不能访问子容器的注册
```

### 注册方式一览

```csharp
// 注册普通类（VContainer 负责创建）
builder.Register<PlayerTakeDamageUseCase>(Lifetime.Scoped);

// 注册普通类，同时以接口暴露
builder.Register<PlayerStats>(...).AsSelf().As<IPlayerStatsReader>();

// 注册场景中已有的 MonoBehaviour 组件（以接口暴露）
builder.RegisterComponent(uiManager).As<IUIService>();

// 自动从场景层级中查找并注册 MonoBehaviour
builder.RegisterComponentInHierarchy<PlayerController>();

// 容器构建完成后回调
builder.RegisterBuildCallback(container => { ... });
```

### 注入方式一览

```csharp
// MonoBehaviour 中：字段注入（推荐，简洁）
[Inject] private IUIService _uiService;

// MonoBehaviour 中：方法注入（依赖关系更明确）
[Inject]
public void Construct(IUIService ui, ISceneLoader loader)
{
    _uiService = ui;
    _sceneLoader = loader;
}

// 纯 C# 类：构造函数注入（最标准，推荐）
public class PlayerTakeDamageUseCase
{
    public PlayerTakeDamageUseCase(PlayerStats stats) { ... }
}
```

### 注入时机

```
场景启动
  ↓
LifetimeScope.Awake()  ← 容器初始化，完成所有注册
  ↓
MonoBehaviour.Awake()  ← 此时 [Inject] 字段还是 null！
  ↓
VContainer 执行注入   ← [Inject] 字段在这里被填充
  ↓
MonoBehaviour.Start()  ← 此时 [Inject] 字段已就绪 ✅
```

> ⚠️ **重要**：不要在 `Awake()` 中使用 `[Inject]` 字段，要在 `Start()` 或之后使用。

---

## 四、优缺点分析

### 优点

| 优点 | 具体体现 |
|------|---------|
| **消灭单例** | 不再有 `UIManager.Instance`，对象生命周期由容器统一管理 |
| **可测试** | `PlayerStats`、`UseCase` 是纯 C# 类，可以直接写 NUnit 单元测试 |
| **可替换** | 只需换一个实现了 `IUIService` 的类，调用方不需要任何修改 |
| **依赖明确** | 一个类需要什么，构造函数或 `[Inject]` 一目了然 |
| **解耦** | `EnemyController` 不再依赖 `PlayerController`，只依赖 `IDamageable` 接口 |
| **数据独立** | `PlayerStats` 独立于 Unity，存档/网络同步只需序列化它 |

### 缺点

| 缺点 | 应对方式 |
|------|---------|
| **学习成本** | 需要理解 DI、接口、LifetimeScope 概念，前期需要投入时间 |
| **场景配置** | 每个场景需要手动挂 LifetimeScope 组件并配置，容易遗漏 |
| **调试复杂** | 注入失败时报错不直观（如 `No such registration`），需要看注册列表 |
| **运行时报错** | 编译正常但运行时才发现注入缺失，比编译时错误更难发现 |
| **不适合超小项目** | 简单 Demo 用单例更快，DI 带来的是工程复杂度换来的长期收益 |

---

## 五、为什么选择这个架构

### 解决了什么问题

**问题1：单例泛滥，全局耦合**
```csharp
// 重构前 —— 任何地方都能调用，依赖关系不可控
UIManager.Instance.OpenUI("UI/MainUI");
```

```csharp
// 重构后 —— 必须通过注入声明依赖，依赖关系一目了然
[Inject] private IUIService _uiService;
_uiService.OpenUI("UI/MainUI");
```

**问题2：PlayerController 既存数据又处理逻辑**
```csharp
// 重构前 —— 一个类 300 行，职责混乱
public class PlayerController : MonoBehaviour
{
    public int CurrentHealth;     // 数据
    public int Gold;              // 数据
    private void Update() { ... } // 输入
    public void TakeDamage() { }  // 逻辑
    public event Action HealthChanged; // 事件
}
```

```csharp
// 重构后 —— 各司其职
PlayerStats           → 存储 HP、Gold、Exp，触发事件
PlayerController      → 处理输入、驱动 FSM
PlayerTakeDamageUseCase → 执行受伤逻辑
MainUI                → 订阅事件，更新显示
```

**问题3：UI 直接依赖场景对象**
```csharp
// 重构前 —— 运行时查找，脆弱
player = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
```

```csharp
// 重构后 —— 编译时声明依赖，健壮
[Inject] private IPlayerStatsReader _stats; // 由容器保证不为 null
```

### 带来的长期收益

```
需求变化                  重构前工作量        重构后工作量
─────────────────────────────────────────────────────
添加新敌人类型             修改 AttackExecutor  只需实现 IDamageable ✅
替换 UI 框架（换 UGUI→UIToolkit）  修改所有 UI 类     只改 IUIService 实现 ✅
实现存档系统               重构 PlayerController  只序列化 PlayerStats ✅
写单元测试玩家受伤          需要启动 Unity       直接 new + NUnit ✅
多人联机同步玩家数据         大量重构             同步 PlayerStats ✅
```

---

## 六、日常开发使用指南

### 新增一个 UI 界面

**Step 1**：创建脚本，继承 `UIBase`，声明需要的依赖

```csharp
using RPGSilent.Domain;
using VContainer;

public class InventoryUI : UIBase
{
    [Inject] private IUIService         _uiService;   // 需要打开/关闭其他 UI
    [Inject] private IPlayerStatsReader _stats;       // 需要读取玩家金币

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        // 打开时刷新显示
        _stats.OnGoldChanged += UpdateGoldDisplay;
        _stats.Refresh();
    }

    public override void OnClose()
    {
        base.OnClose();
        _stats.OnGoldChanged -= UpdateGoldDisplay;
    }

    private void UpdateGoldDisplay(int gold) { /* 更新 UI */ }
}
```

**Step 2**：制作 Prefab，添加 Addressable 标签（Address 设为 `UI/InventoryUI`）

**Step 3**：在需要打开它的地方调用（注入 `IUIService` 后使用）

```csharp
_uiService.OpenUI("UI/InventoryUI");
```

**无需修改任何其他文件。** ✅

---

### 新增一个游戏逻辑（UseCase）

**Step 1**：在 `Application/` 下创建 UseCase 类

```csharp
// Application/PlayerLevelUpUseCase.cs
using RPGSilent.Domain;

public class PlayerLevelUpUseCase
{
    private readonly PlayerStats _stats;

    public PlayerLevelUpUseCase(PlayerStats stats) { _stats = stats; }

    public void Execute()
    {
        // 升级逻辑
        Debug.Log("玩家升级！");
    }
}
```

**Step 2**：在 `SceneLifetimeScope.Configure` 中注册

```csharp
builder.Register<PlayerLevelUpUseCase>(Lifetime.Scoped);
```

**Step 3**：在需要的地方注入使用

```csharp
[Inject] private PlayerLevelUpUseCase _levelUpUseCase;
// 调用：
_levelUpUseCase.Execute();
```

---

### 新增一个可受伤的对象（实现 IDamageable）

例如：添加可破坏的箱子

```csharp
using RPGSilent.Domain;

public class DestructibleBox : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHp = 30;
    private int _hp;

    public bool IsDead => _hp <= 0;

    private void Start() { _hp = maxHp; }

    public void TakeDamage(int damage)
    {
        _hp -= damage;
        if (IsDead) Destroy(gameObject);
    }
}
```

**AttackExecutor 会自动识别并攻击它**，无需修改任何战斗代码。 ✅

---

### 新增一个全局服务

例如：添加音效服务

**Step 1**：在 `Domain/Interfaces/` 创建接口

```csharp
// Domain/Interfaces/IAudioService.cs
namespace RPGSilent.Domain
{
    public interface IAudioService
    {
        void PlaySFX(string clipName);
        void PlayBGM(string clipName);
        void StopBGM();
    }
}
```

**Step 2**：在 `Manager/` 创建实现

```csharp
using RPGSilent.Domain;
using UnityEngine;

public class AudioManager : MonoBehaviour, IAudioService
{
    public void PlaySFX(string clipName) { /* 播放音效 */ }
    public void PlayBGM(string clipName) { /* 播放背景音乐 */ }
    public void StopBGM() { /* 停止 */ }
}
```

**Step 3**：在 `GameLifetimeScope` 注册

```csharp
[SerializeField] private AudioManager audioManager;

protected override void Configure(IContainerBuilder builder)
{
    // ... 其他注册
    builder.RegisterComponent(audioManager).As<IAudioService>();
}
```

**Step 4**：在 Unity Inspector 中将 `AudioManager` 拖入 `GameLifetimeScope` 的字段

**Step 5**：任何需要音效的地方注入使用

```csharp
[Inject] private IAudioService _audio;
_audio.PlaySFX("Attack");
```

---

## 七、常见场景示例

### 场景1：敌人死亡，玩家获得奖励

```
EnemyController.Die()
  └─ _targetRewardable.AddReward(10, 25)      // 通过 IRewardable 接口
        └─ PlayerController.AddReward(10, 25)
              └─ _addRewardUseCase.Execute(10, 25)
                    └─ _stats.AddGold(10)
                    └─ _stats.AddExp(25)
                          └─ 触发 OnGoldChanged、OnExpChanged 事件
                                └─ MainUI 收到事件，更新金币/经验显示 ✅
```

### 场景2：玩家攻击命中敌人

```
AttackState（动画帧）
  └─ AttackExecutor.AttackRelease()
        └─ ExecuteMeleeArc()
              └─ 检测碰撞体，GetComponentInParent<IDamageable>()
                    └─ ApplyDamage(target, profile, hitPoint)
                          └─ target.TakeDamage(20)    // 通过接口，不关心是谁
                                └─ EnemyController.TakeDamage(20) ✅
                                └─ DestructibleBox.TakeDamage(20) ✅（新加的也能被攻击）
```

### 场景3：加载游戏场景，MainUI 初始化

```
点击"开始游戏"
  └─ StartUI → _uiService.OpenUI("UI/LoadingUI")
        └─ LoadingUI → _sceneLoader.LoadScene("Scenes/Main")
              └─ 场景加载完成
                    └─ SceneLifetimeScope 初始化
                          └─ 创建 PlayerStats(100)
                          └─ 注册 UseCase
                          └─ 找到 PlayerController，注入
                          └─ RegisterBuildCallback → UIManager.SetSceneResolver(sceneContainer)
              └─ _uiService.OpenUI("UI/MainUI")
                    └─ Addressables 加载 MainUI Prefab
                          └─ sceneContainer.InjectGameObject(mainUI)  ← 能找到 IPlayerStatsReader ✅
                                └─ MainUI._stats 被填充
                                └─ MainUI.OnOpen() → 订阅事件，刷新显示 ✅
```

---

## 八、注意事项与规范

### ⚠️ 不要在 Awake() 中使用 [Inject] 字段

```csharp
// ❌ 错误：Awake 时注入还没完成
private void Awake()
{
    _uiService.OpenUI("...");  // NullReferenceException！
}

// ✅ 正确：Start 时注入已完成
private void Start()
{
    _uiService.OpenUI("...");  // OK
}
```

### ⚠️ 不要在 Domain 层引用 UnityEngine

```csharp
// ❌ 错误：Domain 层引入了 Unity 依赖
using UnityEngine;
namespace RPGSilent.Domain
{
    public class PlayerStats
    {
        public Vector3 Position; // Domain 不应该知道位置
    }
}

// ✅ 正确：Domain 只有纯 C# 类型
public class PlayerStats
{
    public int CurrentHealth { get; private set; }
}
```

### ⚠️ 新功能先问：该放哪一层？

```
有数据需要持久化/同步？          → Domain/Models/
是对数据的操作逻辑？             → Application/UseCase
是封装 Unity API 的服务？        → Infrastructure/Manager
是显示或响应用户输入？           → Presentation/UI 或 Player
是把上面几者组装在一起？         → DI/LifetimeScope
```

### ⚠️ 新增的 MonoBehaviour 需要注册到容器

如果一个 MonoBehaviour 用了 `[Inject]`，**必须**让 VContainer 知道它：

```csharp
// 方式1：在 LifetimeScope 中注册（推荐）
builder.RegisterComponentInHierarchy<MyNewBehaviour>();

// 方式2：UIManager 加载时自动注入（适合动态加载的 UI Prefab）
// UIManager.OpenLoadedUI 中已经调用了 container.InjectGameObject(uiObj)
// 所以 UI Prefab 上的 MonoBehaviour 会自动被注入
```

### ⚠️ 调试注入失败时的排查步骤

```
报错：VContainerException: No such registration of type: XXX

1. 检查 XXX 是否在对应的 LifetimeScope.Configure 里注册了
2. 检查注入发生时，所需的 LifetimeScope 是否已初始化
   （例如：游戏场景的 SceneLifetimeScope 是否已启动）
3. 检查 ParentReference 是否正确设置
   （SceneLifetimeScope.Parent = GameLifetimeScope）
4. 在 LifetimeScope 的 Configure 里打 Debug.Log 确认注册流程
```

---

## 附录：项目注册关系总表

### GameLifetimeScope 注册列表

| 接口 | 实现 | 生命周期 |
|------|------|----------|
| `IUIService` | `UIManager` | Singleton |
| `IInputService` | `InputManager` | Singleton |
| `ISceneLoader` | `SceneLoaderManager` | Singleton |
| — | `GameStart`（ComponentInHierarchy）| Singleton |

### SceneLifetimeScope 注册列表

| 接口/类型 | 实现 | 生命周期 |
|-----------|------|----------|
| `PlayerStats` + `IPlayerStatsReader` | `PlayerStats` | Scoped |
| `PlayerTakeDamageUseCase` | `PlayerTakeDamageUseCase` | Scoped |
| `PlayerAddRewardUseCase` | `PlayerAddRewardUseCase` | Scoped |
| `PlayerHealUseCase` | `PlayerHealUseCase` | Scoped |
| — | `PlayerController`（ComponentInHierarchy）| Scoped |

---

*文档版本：v1.0 | 最后更新：2026-06-10*
