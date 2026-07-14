# -*- coding: utf-8 -*-
"""Parse Unity interview guide and generate interview_enrichments.json."""
import json
import re
from pathlib import Path

MD_PATH = Path(__file__).parent / "Unity中高级开发面试宝典_2026版.md"
OUT_PATH = Path(__file__).parent / "interview_enrichments.json"

CHAPTER_RANGES = [
    (1, 35, "C#"),
    (36, 90, "Unity基础"),
    (91, 128, "UGUI"),
    (129, 150, "Animator"),
    (151, 178, "Addressables"),
    (179, 230, "性能优化"),
    (231, 268, "架构设计"),
    (269, 296, "网络"),
    (297, 316, "Android/JNI"),
    (317, 334, "微信小游戏"),
    (335, 349, "AI辅助开发"),
    (350, 394, "项目深挖"),
]

CHAPTER_APIS = {
    "C#": ["GC.Alloc", "Span<T>", "NativeArray<T>", "IEnumerator", "async/await", "UniTask"],
    "Unity基础": ["MonoBehaviour", "Transform", "Rigidbody", "SceneManager", "ScriptableObject"],
    "UGUI": ["Canvas", "RectTransform", "GraphicRaycaster", "ScrollRect", "TextMeshProUGUI"],
    "Animator": ["Animator", "AnimatorController", "AnimatorOverrideController", "PlayableGraph"],
    "Addressables": ["Addressables.LoadAssetAsync", "AsyncOperationHandle", "ResourceManager", "Catalog"],
    "性能优化": ["Profiler", "Frame Debugger", "ObjectPool", "SRP Batcher", "BurstCompiler"],
    "架构设计": ["EventBus", "ServiceLocator", "MVVM", "DI Container", "StateMachine"],
    "网络": ["Socket", "KCP", "Protobuf", "WebSocket", "HttpClient"],
    "Android/JNI": ["AndroidJavaObject", "AndroidJavaClass", "IL2CPP", "Gradle", "JNI"],
    "微信小游戏": ["WXSDK", "Wasm", "AssetBundle", "分包加载", "WebGL"],
    "AI辅助开发": ["Cursor", "Copilot", "Code Review", "Prompt", "Roslyn"],
    "项目深挖": ["STAR", "架构决策", "线上监控", "CI/CD", "团队协作"],
}


def get_chapter(qnum: int) -> str:
    for start, end, name in CHAPTER_RANGES:
        if start <= qnum <= end:
            return name
    return "Unity"


def parse_questions(md_text: str) -> dict:
    pattern = re.compile(
        r'<a id="q(\d+)"></a>\s*\n\s*### Q\d+ (.+?)\n\s*\n\s*\| 维度 \| 内容 \|\n'
        r'(?:\|[-| ]+\|\n)?'
        r'((?:\| \*\*.+?\*\* \| .+? \|\n)+)',
        re.MULTILINE,
    )
    questions = {}
    for m in pattern.finditer(md_text):
        num = int(m.group(1))
        title = m.group(2).strip()
        table = m.group(3)
        fields = {}
        for row in re.finditer(r'\| \*\*(.+?)\*\* \| (.+?) \|', table):
            fields[row.group(1).strip()] = row.group(2).strip()
        questions[num] = {"title": title, "fields": fields}
    return questions


def pick(*keys, fields, default=""):
    for k in keys:
        if k in fields and fields[k]:
            return fields[k]
    return default


def bullets(items):
    return "\n".join(f"- {x}" for x in items if x)


def star_answer(qnum, title, fields):
    framework = pick("答题框架", "标准答案", fields=fields)
    points = pick("表达要点", "原理解析", fields=fields)
    project = pick("可举项目", "项目实战", fields=fields)
    topic = re.sub(r"^(请介绍|如何介绍|你如何|如何回答)", "", title).strip("？? ")
    return bullets([
        f"**S（情境）**：交代项目背景——类型（RPG/MMO/小游戏）、平台、团队规模、当时约束（上线节点、包体/内存上限、弱网）。例：「在 {project or '某 Unity 项目'} 中，我负责 {topic}。」",
        f"**T（任务）**：明确个人职责与可量化目标，避免只说「我们做了」。例：「我需要在 X 周内把 {topic} 的 XX 指标从 A 改善到 B，并保证策划可配置。」",
        f"**A（行动）**：讲技术决策链——方案 A/B 对比、为何选当前方案、关键类/API、难点与取舍。{framework}",
        f"**R（结果）**：用数据收尾（帧率、内存、崩溃率、开发效率、线上指标），并复盘「重做会改什么」。{points}",
        "**Concrete talking points**：准备 2~3 个可被深挖的细节（类名如 PlayerSpawnPoint、一次线上事故、Profile 对比截图），证明你真正做过而非背题。",
    ])


SPECIAL = {
    16: {
        "answer": bullets([
            "**结论**：Unity Inspector 序列化的是字段（field），不是普通 C# 属性（property）；想让私有数据可配，用 `[SerializeField] private` 或 Unity 2020+ 的 `[field: SerializeField]`。",
            "**机制**：Unity 内置序列化器只遍历标记了 `[SerializeField]` 的字段或 public 字段，不会走 property 的 get/set；`[NonSerialized]`、`[HideInInspector]` 可控制是否写入磁盘或是否显示。",
            "**auto property 示例**：`[field: SerializeField] public float Speed { get; private set; }` 可以序列化 backing field，但 Inspector 里字段名不如显式 `_speed` 直观，团队协作时难搜索。",
            "**推荐写法**：`[SerializeField] private float _moveSpeed;` + 只读属性 `public float MoveSpeed => _moveSpeed;`，既封装又可在 Inspector 调参。",
            "**ScriptableObject/Prefab**：修改序列化字段会触发 YAML 脏标记；属性若只在运行时计算则不落盘，适合 `[SerializeField]` 存原始值、property 做校验/换算。",
            "**Unity 应用**：配置组件、关卡参数、Addressables 引用都应走字段序列化；Editor 扩展用 `SerializedObject.FindProperty(\"_moveSpeed\")` 而非直接访问 property。",
        ]),
        "principle": "Unity 序列化基于反射扫描字段元数据并写入 YAML/二进制资产，与 C# 编译器生成的 property 访问器是两套体系。property 的 getter/setter 逻辑在反序列化阶段不会执行，因此运行时依赖 setter 校验的数据可能在 Inspector 改值时绕过检查。",
        "project": "在 RPG-Silent 中，`PlayerSpawnPoint` 用 `[SerializeField] private DungeonDatabase dungeonDatabase` 暴露副本表引用，而 `alignRotation` 等开关同样走字段序列化；若改成 `{ get; set; }` 自动属性且不加 `[field: SerializeField]`，Prefab 上会丢失引用导致出生点回退逻辑失效。",
        "mistakes": bullets([
            "把业务数据写成 public property 却在 Inspector 找不到——策划无法配表，运行时全是默认值，表现为「改了参数不生效」。",
            "在 property setter 里做范围钳制，但 Inspector 直接写字段绕过 setter——出现负速度、超大碰撞半径等脏数据。",
            "混用 `[SerializeField]` 与 public 字段不统一——Review 时无法一眼看出哪些是可配设计数据。",
        ]),
    },
    36: {
        "answer": bullets([
            "**结论**：初始化用 Awake/OnEnable/Start 分层，帧循环用 FixedUpdate/Update/LateUpdate，销毁前务必 OnDisable/OnDestroy 解绑。",
            "**完整顺序**：`Awake` → `OnEnable` → `Start` → 帧循环（`FixedUpdate` → `Update` → `LateUpdate`）→ `OnDisable` → `OnDestroy`。",
            "**Awake**：脚本实例化时调用，适合 `GetComponent` 缓存、读 `[SerializeField]` 配置；即使 GameObject 未激活也会执行（若脚本 enabled）。",
            "**OnEnable/Start**：`OnEnable` 在激活时触发且早于同帧 `Start`；应用 `SetActive(true)` 会再次 OnEnable。`Start` 适合依赖其他对象 Awake 完成的逻辑。",
            "**帧循环**：`FixedUpdate` 固定步长驱动 `Rigidbody`；`Update` 读输入；`LateUpdate` 跟相机/看背。",
            "**Unity 应用**：Script Execution Order 可微调同类函数先后；DontDestroyOnLoad 对象跨场景时 OnDisable/OnEnable 仍按激活状态触发。",
        ]),
        "principle": "Unity 将 MonoBehaviour 回调注册进 PlayerLoop 的不同阶段：Awake/Start 属于初始化阶段，Update 族属于 Simulation 阶段。OnEnable 与 Start 的分离使得对象池复用时可以在不重建组件的情况下重新订阅事件。",
        "project": "`PlayerSpawnPoint.Start()` 里 `yield return new WaitForFixedUpdate()`  deliberately 晚于玩家 `Rigidbody` 初始化，避免与物理写入冲突；若把定位逻辑放 Awake，可能被玩家 Controller 同帧覆盖位置。",
        "mistakes": bullets([
            "在 Awake 里访问其他对象的 Start 才赋值的字段——得到 null 或默认值，偶现初始化顺序 bug。",
            "OnEnable 订阅事件但 OnDisable 不解绑——对象池回收后仍收到回调，造成空引用或重复执行。",
            "在 OnDestroy 里调用 `Destroy(other)` 或访问已销毁单例——退出 Play 模式或切场景时抛 MissingReferenceException。",
        ]),
    },
    44: {
        "answer": bullets([
            "**结论**：`Time.timeScale = 0` 暂停「缩放时间域」，影响 `deltaTime`/`time`/`WaitForSeconds`；UI 动画和真实计时需用 unscaled 系列 API。",
            "**受影响**：`Time.deltaTime`、`Time.time`、`Time.fixedTime`（乘 scale）、协程 `WaitForSeconds`、`Animator.speed`（若 updateMode 为 Normal）、粒子 simulationSpeed 等。",
            "**不受影响**：`Time.unscaledDeltaTime`、`Time.realtimeSinceStartup`、`WaitForSecondsRealtime`、`DateTime.Now`、网络 IO 超时。",
            "**物理注意**：`Time.fixedDeltaTime` 本身不随 scale 变，但 FixedUpdate 调用次数会随 scale 减少；Rigidbody 在 scale=0 时仍可能因碰撞求解产生微位移，需 `Rigidbody.Sleep()` 或 `simulationMode`。",
            "**Unity 应用**：暂停菜单 DOTween 用 `SetUpdate(true)`；战斗倒计时 UI 用 `unscaledDeltaTime` 累加；子弹时间设 `timeScale=0.3` 并同步 `Time.fixedDeltaTime = 0.02f * timeScale`。",
        ]),
        "principle": "Unity 将游戏逻辑时间与墙钟时间分离：timeScale 是游戏时钟倍率，所有基于 `Time.time` 积分的系统共享同一缩放因子。Animator 的 UnscaledTime 模式则绑定 `Time.unscaledDeltaTime`，因此可在暂停时继续播放 UI 过渡。",
        "project": "RPG 暂停界面：`PauseMenu` 设 `Time.timeScale=0`，同时 `CanvasGroup` 淡入用 `UIAnimation` + unscaled 时间；恢复战斗时还原 scale 与 fixedDeltaTime，避免物理步长不一致导致角色穿透。",
        "mistakes": bullets([
            "暂停后 UI 动画/Loading 圈也停住——玩家以为卡死，因协程仍用 `WaitForSeconds`。",
            "只改 timeScale 不改 Animator updateMode——角色 idle 动画冻结但状态机 Transition 仍依赖 unscaled 参数，表现不一致。",
            "多人游戏客户端本地 timeScale=0 但服务器时间继续走——重连后状态不同步，需区分本地表现暂停与逻辑暂停。",
        ]),
    },
    48: {
        "answer": bullets([
            "**结论**：Trigger 只做 overlap 检测不产生物理阻挡；Collision 会解算接触力。拾取、区域检测用 Trigger，推拉、碰撞反馈用 Collision。",
            "**Trigger 条件**：至少一方 Collider 勾选 `Is Trigger`，且至少一方挂 `Rigidbody`（可为 Kinematic）；双方都是无 Rigidbody 的静态 Collider 时 **不会** 触发 `OnTriggerEnter`。",
            "**Collision 条件**：双方 `Is Trigger=false`，至少一方有 Rigidbody；回调提供 `Collision.contacts` 接触点与 `relativeVelocity`。",
            "**Kinematic 用法**：玩家用 Kinematic Rigidbody + CharacterController/脚本移动，Trigger 区域挂静态 Collider，可稳定收到 `OnTriggerEnter`。",
            "**Unity 应用**：掉落物：`SphereCollider(isTrigger=true)` + 玩家 Kinematic RB；墙壁：`BoxCollider` + Collision 播放音效；Layer Matrix 过滤 `Physics.IgnoreLayerCollision`。",
        ]),
        "principle": "PhysX 对 Trigger 走 Broadphase/Narrowphase 重叠检测但不生成 Contact Manifold，因此无摩擦、无反弹；Collision 会生成接触约束并进入求解器。Rigidbody 存在是为了让 PhysX 将对象纳入动态/kinematic 模拟树，静态-静态 overlap 默认不派发消息以省 CPU。",
        "project": "Dungeon 入口：`PortalTrigger : MonoBehaviour` 在 `OnTriggerEnter(Collider other)` 检查 `other.CompareTag(\"Player\")` 后加载场景；若忘记给玩家加 Rigidbody，编辑器里看似重叠却永远不进触发器，策划报「传送门无效」。",
        "mistakes": bullets([
            "拾取物与玩家都无 Rigidbody——OnTriggerEnter 永不触发，测试 Scene 里拖动物体靠近也无法复现。",
            "Trigger 与 Collision 混用同一 Collider 未勾选 isTrigger——角色被门挡住而不是穿过触发。",
            "在 OnTriggerStay 每帧 Instantiate 特效——CPU 和 GC 爆炸，应加 flag 或 OnTriggerEnter 单次处理。",
        ]),
    },
    86: {
        "answer": bullets([
            "**结论**：场景内关键对象（出生点、交互点、刷怪点）应在加载期注册到场景上下文或配置表，禁止运行时 `FindObjectOfType`/`GameObject.Find` 全局搜索。",
            "**机制**：`PlayerSpawnPoint` 优先从 `DungeonDatabase.TryGetById(DungeonLaunchContext.DungeonId, out Entry)` 读表配置坐标；无表数据时退回场景里 `PlayerSpawnPoint` 自身的 `Transform`。",
            "**时序**：`yield return new WaitForFixedUpdate()` 后再 `PlacePlayer`，并对 `Rigidbody` 清零 `linearVelocity`/`angularVelocity`，避免物理惯性把玩家甩飞。",
            "**注册表模式**：可用 `SceneRegistry` 单例在 Awake 收集 `[RegisterInScene]` 组件，或 ScriptableObject 关卡索引 + 场景内 Marker 双通道。",
            "**Unity 应用**：Additive 加载子场景时，每个子场景一个 `SpawnAuthoring` 写入 `DungeonDatabase`；跨场景 DontDestroyOnLoad 的玩家由 `PlayerRigSpawner` 统一放置。",
        ]),
        "principle": "全局查找是 O(n) 场景遍历且无法缓存，每次调用还分配枚举器；表驱动 + 场景 Marker 将「查谁」与「放在哪」解耦，Editor 里 `DungeonSpawnAuthoring` 可可视化写回表，保证策划改出生点不必改代码。",
        "project": "RPG-Silent：`PlayerSpawnPoint` 序列化引用 `DungeonDatabase`，运行时读 `DungeonLaunchContext.DungeonId`；`DungeonSpawnAuthoringEditor` 在 Scene 视图拖动手柄把 Transform 写回 `DungeonDatabase.Entry.SpawnPosition/SpawnRotation`，实现「场景编辑即配表」。",
        "mistakes": bullets([
            "多处 `FindWithTag(\"Player\")`/`FindObjectOfType<PlayerSpawnPoint>()`——Duplicate 玩家或切场景后找到错误实例，出生点随机。",
            "在 Awake 立即设置 Transform 而玩家 Rigidbody 同帧还在初始化——下一 FixedUpdate 被物理覆盖，表现为「闪一下回到原点」。",
            "表与场景 Transform 双源不一致且无日志——策划改场景但未写回 `DungeonDatabase`，打包后坐标仍与 Editor 预览不同。",
        ]),
    },
    198: {
        "answer": bullets([
            "**结论**：按音频长度与播放频率选 AudioClip Load Type——短 SFX 用 Decompress On Load，长 BGM/语音用 Streaming，中等长度权衡 Compressed In Memory。",
            "**Decompress On Load**：导入时解压到 PCM，播放零解压延迟，适合 <1s 的 UI 点击、脚步；内存占用 = 样本数 × 声道 × 位深。",
            "**Compressed In Memory**：保留压缩格式在内存，播放时 CPU 解压，适合中等音效；移动平台需关注 ADPCM/Vorbis 解码成本。",
            "**Streaming**：磁盘逐块读取，内存恒定，适合 3~5 分钟 BGM；注意 Android 上 Streaming 路径与 `Application.streamingAssetsPath`。",
            "**Unity 应用**：`AudioManager` 按组（BGM/SFX/Voice）设不同 Load Type；`AudioSource.PlayOneShot` 高频音效进池并共享 clip 引用，避免 duplicate clip。",
        ]),
        "principle": "Unity 音频管线在加载期决定样本驻留形态：Decompress 将解压成本前移到加载，Streaming 将成本分摊到播放 IO。Load Type 与 Compression Format（PCM/Vorbis/ADPCM）共同决定内存、CPU 与延迟三角关系。",
        "project": "战斗项目：100+ 短音效（砍杀、受击）统一 Decompress On Load + `AudioSource` 对象池；章节 BGM 仅保留当前/下一首 Streaming clip 引用，切歌 `Resources.UnloadUnusedAssets` 释放旧流。",
        "mistakes": bullets([
            "全部 Decompress On Load——移动端音频内存轻松超 100MB，触发 Low Memory 杀进程。",
            "Streaming BGM 却每帧 `Resources.Load` 新 clip——IO 卡顿且无法利用流式缓冲。",
            "3D 音效大量同时播放未设 `AudioSource.maxDistance`/优先级——混音器 Voice 数爆掉，全项目静音。",
        ]),
    },
    202: {
        "answer": bullets([
            "**结论**：Static Batching 合并静态网格降低 Draw Call，但会复制顶点缓冲增加内存；URP 项目应优先实测 SRP Batcher，再决定是否 Mark Static。",
            "**机制**：勾选 Static 且满足同材质/同 Lightmap 等条件时，引擎在构建期或运行时将多个 Mesh 合成大 VBO，CPU 侧一次提交；动态物体无法参与。",
            "**与 SRP Batcher**：SRP Batcher 通过 CBUFFER 复用材质数据减少 SetPassCall，两者解决不同瓶颈；部分 URP 配置下 Static Batching 收益被 SRP Batcher 部分替代，需 Frame Debugger 对比。",
            "**内存代价**：合批后每份静态网格顶点可能被复制进合批 VBO，同材质 500 个草实例可能 +50MB 顶点内存。",
            "**Unity 应用**：大地形建筑、关卡静态道具 Mark `Batching Static`；可交互门、可破坏箱不 Mark；GPU Instancing 适用的草/树优先 Instancing 而非 Static Batching。",
        ]),
        "principle": "Static Batching 在 CPU 侧合并 Draw Call，代价是将原本共享 Mesh 的实例复制为合批几何体常驻 GPU/CPU 内存。SRP Batcher 则不合并网格，而是让不同 Mesh 在 shader 兼容时共享 per-object 大缓冲，因此移动 URP 项目常先开 SRP Batcher 再评估 Static。",
        "project": "开放世界手游：城区建筑 Prefab 统一材质图集 + Batching Static，Draw Call 从 800 降到 200，但 Memory Profiler 显示 Mesh 内存 +30MB；改 GPU Instancing 渲染重复路灯后 DC 持平且内存回落。",
        "mistakes": bullets([
            "可移动/可缩放对象误勾 Static——每帧 Transform 变化导致合批失效并触发重建，CPU 尖刺。",
            "不同 Lightmap 或不同材质却 Mark Static——无法合批，白占 Static 标记的维护成本。",
            "假设 Static Batching 与 SRP Batcher 收益叠加——实测 Draw Call 不降反升，因材质变体不兼容 SRP Batcher。",
        ]),
    },
    203: {
        "answer": bullets([
            "**结论**：Dynamic Batching 仅适合顶点属性 ≤900 左右的小网格，CPU 合批开销大；URP 移动项目优先 SRP Batcher + GPU Instancing，勿指望 Dynamic Batching 救场。",
            "**限制**：网格顶点过多、多 pass shader、不同 scale（非 uniform 缩放）、启用 lightmap 等都会打断动态合批。",
            "**CPU 成本**：每帧在 CPU 侧拼顶点缓冲，物体多时合批本身成为瓶颈，Profiler 里 `Batching.Draw` 耗时上升。",
            "**URP 现实**：2021+ URP 默认更依赖 SRP Batcher/GPU Instancing；大量小道具共用 `MaterialPropertyBlock` 改色是更稳方案。",
            "**Unity 应用**：子弹、伤害数字等小 Quad 可合并 Atlas + 单材质 Instancing；Particle System 用 GPU 模式而非大量小 Mesh Renderer。",
        ]),
        "principle": "Dynamic Batching 在运行时把多个小网格的顶点拷贝到临时缓冲再绘制，属于 CPU 侧优化；当 Draw Call 已不高或网格不够「小且同构」时，拷贝成本超过省下的 SetPassCall。Instancing 则在 GPU 侧一次 draw 多 instance，扩展性更好。",
        "project": "塔防项目：50 种小敌人本期望 Dynamic Batching，Profiler 显示主线程 Batching 占 4ms；改为共享 SkinnedMesh 材质 + GPU Instancing 后 DC 从 120→15，CPU 回落 2ms。",
        "mistakes": bullets([
            "高面数角色期望动态合批——顶点超限，每个角色仍单独 Draw Call。",
            "对 UI Canvas  Renderer 开 Dynamic Batching 幻想——UGUI 走不同渲染路径，与 3D Dynamic Batching 无关。",
            "材质实例化（`renderer.material`）破坏合批——应使用 `sharedMaterial` + MaterialPropertyBlock。",
        ]),
    },
    276: {
        "answer": bullets([
            "**结论**：TCP 已有序可靠，应用层序号主要用于业务去重与请求-响应配对；UDP/KCP 自定义协议必须自建序号处理乱序、重复与丢包。",
            "**TCP 场景**：仍可用自增 seq 做「客户端请求 id」防止重复提交、幂等充值；不必重复造 TCP 传输层轮子。",
            "**UDP 场景**：包结构含 `uint32 seq` + `uint32 ack` + payload；接收端维护滑动窗口，`seq <= lastAck` 丢弃重复，`seq > expected` 缓存乱序。",
            "**战斗指令**：帧同步/状态同步里序号与服务器帧号绑定，迟到包直接丢弃而非重放，避免「回滚再执行」视觉跳变。",
            "**Unity 应用**：`NetworkChannel` 发送前 `Interlocked.Increment(ref _sendSeq)`；接收协程按序 dispatch 到 `MainThreadDispatcher`，超时触发重传或断线重连。",
        ]),
        "principle": "传输层序号解决「字节流/ datagram 如何可靠有序到达」；应用层序号解决「业务消息语义是否仍有效」。TCP 的 seq 是字节偏移，与应用消息边界无关，因此战斗逻辑仍需 messageId 或 frameId 做去重与过期判定。",
        "project": "KCP 战斗通道：每包 `header.seq` 单调递增，接收端 `Dictionary<uint, Action>` 存未确认 RPC；duplicate seq 直接 ignore，gap 超 3 帧请求服务器重发快照而非逐条补包。",
        "mistakes": bullets([
            "TCP 上仍按 UDP 逻辑每条等 ack 重传——双重重传导致延迟放大与服务器压力。",
            "序号用 int 溢出未处理——长时间运行后 seq 回绕等于旧包，误当新包执行两次技能。",
            "只递增发送序号不做接收去重——弱网重传导致同一攻击扣血两次，玩家投诉「幽灵伤害」。",
        ]),
    },
    307: {
        "answer": bullets([
            "**结论**：Android 发布包选 IL2CPP 获更好性能与安全性；Mono 适合 Editor/快速迭代。IL2CPP 需处理 AOT 裁剪、反射与泛型限制。",
            "**IL2CPP 流程**：C# → IL → C++ → NDK 编译 native .so；启动时无 JIT（除可选 HybridCLR 等扩展），方法调用为直接函数指针。",
            "**与 Mono 差异**：Mono 用 JIT 解释/编译 IL，包体较小、构建快，但运行时性能与代码保护弱于 native；Google Play 64 位要求使 IL2CPP 成为主流。",
            "**AOT 坑**：`link.xml` 保留反射用到的类型；泛型 `JsonUtility.FromJson<T>` 若 T 未在 AOT 实例化列表会 Runtime 崩溃。",
            "**Unity 应用**：Player Settings → Scripting Backend = IL2CPP，Target Architectures 勾 ARM64；`Managed Stripping Level=Low/Medium` 后跑一遍全量功能测试 + `dotnet` 符号表上传 Bugly。",
        ]),
        "principle": "IL2CPP 将托管代码静态编译为平台 native 指令，消除 JIT 开销并提高逆向门槛；代价是构建链更长、部分动态特性（Emit、某些反射）需在编译期可解析。Unity 的 Managed Stripping 在 IL2CPP 前移除未引用类型，进一步要求 link.xml 显式保留 JNI/序列化/热更入口。",
        "project": "RPG Android 包：IL2CPP + ARM64，JNI 回调 `AndroidJavaObject` 支付 SDK；曾因 `link.xml` 未保留 `GooglePlayGames.XXX` 导致 Release 闪退，Development 包 Mono 正常——典型 Stripping 问题。",
        "mistakes": bullets([
            "Release IL2CPP 未测反射注册路径——线上首启 Json 反序列化 MissingMethodException。",
            "只打 ARMv7 未开 ARM64——Play Store 拒审或高端机性能差。",
            "以为 IL2CPP 自动更快不优化 GC/逻辑——CPU Profiler 仍可能主线程脚本热点，native 只是执行更快。",
        ]),
    },
    334: {
        "answer": bullets([
            "**结论**：微信小游戏需建立 iOS/Android × 性能档位 × 基础库版本 的适配矩阵，覆盖安全区、输入、内存与 API 差异。",
            "**安全区**：`wx.getSystemInfoSync().safeArea` 驱动 UGUI 顶栏/底栏 padding；iPhone 刘海与 Android 打孔屏 inset 不同，需 Canvas Scaler + SafeAreaFitter 组件。",
            "**性能档位**：低端机关闭后处理、降粒子、限制同屏 Draw Call；用 `wx.getDeviceBenchmarkInfo` 或自研帧率/内存探针分 Low/Mid/High。",
            "**基础库版本**：支付、开放数据域、Worker 等 API 有最低版本；`wx.canIUse` 做降级，QA 矩阵含 2.30+ / 3.0+ 代表版本。",
            "**Unity 应用**：WebGL/Wasm 包体分包 + 首包资源压缩；Android 微信与 iOS 微信在音频自动播放、触摸延迟上行为不同，需真机双端各测一遍完整新手流程。",
        ]),
        "principle": "微信小游戏运行在宿主 WebView + 自定义 Wasm 运行时中，设备碎片化比原生 App 更严重：同一 Unity 构建在 iOS 微信的 JIT/Wasm 限制与 Android 微信的内存回收策略可能完全不同。基础库版本决定 JS API 能力边界，而非仅 OS 版本。",
        "project": "卡牌小游戏：适配矩阵 Excel 记录 20 款机型 × 微信 8.0.4x/8.0.5x；iOS 低端机内存 1.2GB 触发 Kill，通过纹理 Max Size 512 + ASTC 6×6 压到 400MB 以下才稳定。",
        "mistakes": bullets([
            "只测开发者自己的 flagship Android——iOS 微信首包 Wasm 编译慢 30s，被误判为「卡死」未做 loading 拆分。",
            "忽略 safeArea 导致购买按钮被 Home Indicator 遮挡——转化率下降难复现（QA 用无刘海模拟器）。",
            "调用新 API 未 `canIUse`——旧基础库用户黑屏，后台错误率 spike 但本地 Debug 正常。",
        ]),
    },
}


TOPIC_HINTS = [
    (r"struct|class|值类型|引用类型", [
        "`Vector3`/`Quaternion` 是 struct，作为字段赋值会拷贝 12~16 字节；`MonoBehaviour` 必须是 class 才能挂到 GameObject。",
        "Profiler 中对比 struct 传参与 class 引用：大 struct（>16 字节）按值传递可能比一次 GC 更贵。",
    ]),
    (r"委托|event|Action|Func", [
        "C# 事件 `event Action OnHit` 外部只能 `+=`/`-=`，触发必须在类内 `OnHit?.Invoke()`，防止外部恶意清空订阅。",
        "`UnityAction` 与 `Action` 类似但可序列化，常用于 Inspector 持久化回调（如 Button.onClick）。",
    ]),
    (r"async|await|UniTask|异步", [
        "Unity 2023+ 默认 SynchronizationContext 会把 continuation 派发到主线程，但 `Task.Run` 内仍不能直接调 `transform.position`。",
        "销毁对象时 `CancellationTokenSource.Cancel()` 取消 await，避免 `MissingReferenceException`。",
    ]),
    (r"Dictionary|哈希|hash", [
        "内部 `Entry[]` + 桶数组，负载因子超阈值时扩容为约 2 倍并重算 hash 索引。",
        "自定义 key 的 `GetHashCode()` 必须稳定：CombatEntity 作 key 时若 hash 随状态变会导致查不到。",
    ]),
    (r"协程|IEnumerator|yield", [
        "编译器为 iterator 生成状态机类，每次 `MoveNext()` 从上次 `yield` 处恢复。",
        "`StartCoroutine` 返回 `Coroutine` 句柄，`StopCoroutine` 或 `OnDisable` 必须停止，否则对象销毁后仍执行。",
    ]),
    (r"GC|装箱|Alloc|闭包", [
        "Profiler Deep Profile 看 `GC.Alloc` 列：常见元凶是 `string` 拼接、`LINQ`、`foreach` 装箱枚举、lambda 捕获。",
        "对象池 `Pool<T>.Get()` 复用 `List<T>`/`StringBuilder` 可让战斗帧保持 0B GC。",
    ]),
    (r"List|扩容|Capacity", [
        "`List<T>` 默认容量 4，翻倍扩容会 `Array.Copy` 并产生 GC；`new List<T>(64)` 预设容量。",
        "热路径用 `Clear()` 复用而非 `new List`，注意 `Count=0` 后 Capacity 仍保留。",
    ]),
    (r"string|StringBuilder", [
        "每次 `+` 拼接生成新 immutable string；循环内用 `StringBuilder` 或 `ZString`（UTF16 零 GC 方案）。",
        "`$\"HP:{hp}\"` 插值在 Unity 仍会分配，热路径改用 `StringBuilder.AppendFormat` 或缓存格式串。",
    ]),
    (r"序列化|SerializeField|Inspector", [
        "Unity 序列化走字段反射，不执行 property setter；`[FormerlySerializedAs]` 可兼容字段重命名。",
        "`[SerializeReference]` 支持多态引用序列化，适合技能/对话节点树。",
    ]),
    (r"生命周期|Awake|Start|Update|OnEnable", [
        "Script Execution Order 设为负数可让 InputManager 先于 PlayerController.Awake 执行。",
        "对象池 `SetActive(false)` 触发 OnDisable 但不 Destroy，再次启用时 OnEnable 会重跑。",
    ]),
    (r"FixedUpdate|Rigidbody|物理", [
        "`Rigidbody.MovePosition` 在 FixedUpdate 中与 PhysX 同步；Update 里改 transform 会导致视觉抖动。",
        "`Rigidbody.interpolation` 可在渲染帧插值物理位置，改善相机跟随观感。",
    ]),
    (r"timeScale|暂停|unscaled", [
        "暂停菜单 DOTween：`tween.SetUpdate(true)` 使用 unscaled time。",
        "`Animator.updateMode = AnimatorUpdateMode.UnscaledTime` 让 UI 角色在 timeScale=0 时仍播放。",
    ]),
    (r"Trigger|Collision|Collider", [
        "Kinematic Rigidbody + Trigger Collider 是拾取/传送门标准组合；静态 Collider 对不会触发回调。",
        "`Physics.IgnoreLayerCollision` 让玩家层与玩家层不碰撞但可与敌人 Collision。",
    ]),
    (r"Prefab|ScriptableObject|Scene", [
        "Prefab Variant 继承父 Prefab 差异，适合 10 种怪物共用 BaseEnemy 逻辑。",
        "ScriptableObject 存全局配置（技能表、DungeonDatabase），Scene 只放关卡实例与引用。",
    ]),
    (r"Canvas|UGUI|ScrollRect|Graphic", [
        "Canvas 重建：`Graphic.SetVerticesDirty()` → `Canvas.BuildBatch()`，频繁改 Text 会触发全 Canvas 重建。",
        "虚拟列表只实例化可见项 + `RectTransform.anchoredPosition` 复用，1000 条商品仍 20 个 Cell。",
    ]),
    (r"Animator|动画|Blend Tree", [
        "`Animator.SetFloat(\"Speed\", speed, dampTime, dt)` 平滑混合比每帧硬设参数更自然。",
        "Animation Event 在指定帧调 `OnAttackHit()`，但删除/重导入 clip 后 event 可能丢失需校验。",
    ]),
    (r"Addressables|AssetBundle|Resources", [
        "`AsyncOperationHandle` 必须配对 `Addressables.Release(handle)`，否则 AssetBundle 常驻内存。",
        "`await Addressables.LoadAssetAsync<T>(key).Task` 要处理同一 key 并发加载的竞态（缓存 handle）。",
    ]),
    (r"Draw Call|Batch|SRP|Shader|Instancing", [
        "Frame Debugger 看 `RenderLoop.Draw` 批次：SRP Batcher 合并条件是同一 shader variant + 兼容 CBUFFER。",
        "`MaterialPropertyBlock` 改色不 `new Material()`，避免破坏合批。",
    ]),
    (r"内存|Profiler|泄漏|对象池", [
        "Memory Profiler 拍两次快照 Diff：查 `Texture2D`、`Mesh`、`AssetBundle` 引用链谁持有 handle。",
        "静态事件 `OnPlayerDead += Handler` 未 `-=` 会导致 Scene 卸载后对象仍被引用无法 GC。",
    ]),
    (r"架构|事件|单例|状态机|MVVM", [
        "`IEventBus.Publish<DamageEvent>` 让 UI 血条与战斗解耦，新系统只订阅不修改 PlayerHealth 源码。",
        "有限状态机 `enum State { Idle, Attack }` + `Dictionary<State,IState>` 避免 bool 组合爆炸。",
    ]),
    (r"TCP|UDP|KCP|网络|同步|心跳", [
        "TCP Nagle 与粘包：应用层用长度头 `uint16 len + payload` 拆包。",
        "状态同步：客户端发输入 seq，服务器广播帧号；迟到包 seq < serverFrame 直接丢弃。",
    ]),
    (r"Android|JNI|IL2CPP|Gradle", [
        "`new AndroidJavaClass(\"com.unity3d.player.UnityPlayer\").GetStatic<AndroidJavaObject>(\"currentActivity\")` 取 Activity。",
        "IL2CPP Stripping 后 JNI 反射类名需在 `link.xml` `<preserve>` 保留。",
    ]),
    (r"微信|小游戏|Wasm|分包", [
        "首包 Wasm + 分包 AB：超过 4MB 首包需 CDN 远程包，`wx.downloadFile` 失败要有重试与降级 UI。",
        "`wx.onMemoryWarning` 回调里 `Resources.UnloadUnusedAssets` + 释放非必要 Addressables。",
    ]),
    (r"AI|Copilot|Cursor|Review", [
        "AI 生成 MonoBehaviour 常漏 `OnDestroy` Release 与 `OnDisable` 退订；合入前必跑 Play Mode 进出 3 次。",
        "Prompt 应包含：Unity 版本、URP/Built-in、生命周期约束、禁止 async void。",
    ]),
]

CHAPTER_UNITY_APPLY = {
    "C#": [
        "用 Deep Profile 对比优化前后 `GC.Alloc`，目标战斗帧 0B。",
        "热路径代码放独立 Assembly Definition，便于 Burst/测试与主工程隔离。",
    ],
    "Unity基础": [
        "在 Editor Play Mode 用 Frame Debugger + Physics Debug 验证行为与预期一致。",
        "用 Prefab 变体与 ScriptableObject 配置减少场景 diff 冲突。",
    ],
    "UGUI": [
        "Profiler 勾选 UI Details，定位 `Canvas.SendWillRenderCanvases` 耗时。",
        "Safe Area 用 `Screen.safeArea` 驱动 `RectTransform.offsetMin/Max`。",
    ],
    "Animator": [
        "Animator 窗口 Preview + `Animator.GetCurrentAnimatorStateInfo(0).shortNameHash` 调试状态。",
        "Humanoid 复用动画：`Avatar` 一致即可 Retarget 共享 clip。",
    ],
    "Addressables": [
        "Event Viewer 观察 load/release；Analyze 面板查 duplicate bundle。",
        "Content Update 后只打 changed bundle，客户端 `CheckForCatalogUpdates`。",
    ],
    "性能优化": [
        "建立帧预算：逻辑 4ms / 渲染 8ms / UI 2ms，每次只改一个变量对比 Profile。",
        "低端机降 `QualitySettings.resolutionScalingFixedDPIFactor` 与粒子上限。",
    ],
    "架构设计": [
        "Gameplay 只依赖 `IInventoryService` 接口，UI/网络/存档各实现 Adapter。",
        "配置表导出 JSON/Bytes，`ConfigManager.Get<T>(id)` 只读访问。",
    ],
    "网络": [
        "Charles/mitmproxy 抓包 + 客户端 seq 日志对齐排查乱序/重复。",
        "弱网模拟：延迟 200ms + 10% 丢包验证重连与 UI 提示。",
    ],
    "Android/JNI": [
        "logcat `-s Unity` 过滤；Release IL2CPP 必跑全量回归。",
        "Gradle `mainTemplate.gradle` 统一 SDK 与 NDK 版本。",
    ],
    "微信小游戏": [
        "真机双端测首包加载、内存 warning、支付/分享完整链路。",
        "基础库最低版本用 `canIUse` 做 API 降级分支。",
    ],
    "AI辅助开发": [
        "AI 产出走 PR + Roslyn 分析器 + 人工 Play Mode 验证清单。",
        "团队 Prompt 模板库沉淀 Unity 项目约束，减少幻觉 API。",
    ],
}

PRINCIPLE_DEEP = {
    "C#": [
        "CLR 分代 GC 在 Unity 主线程触发时会形成帧尖刺，因此热路径零分配是移动端硬指标。",
        "C# 语义与 Unity 引擎对象（UnityEngine.Object）的销毁语义叠加，判空需用 `== null` 重载。",
    ],
    "Unity基础": [
        "PlayerLoop 各阶段顺序固定，脚本回调插入 native 模拟管线，错阶段操作会导致一帧延迟或穿透。",
        "UnityEngine.Object 的 identity 与 C# 引用分离，Destroyed 对象 C# 引用非 null 但 `== null` 为 true。",
    ],
    "UGUI": [
        "UGUI 是 retained-mode：改数据 → MarkDirty → Rebuild Mesh → Batch，CPU 成本在 Rebuild 而非 Draw。",
        "多 Canvas 分层（Static HUD / Dynamic Popup）可隔离重建范围。",
    ],
    "Animator": [
        "PlayableGraph 在 native 层评估，C# 每帧 Set 参数会触发状态机重评估，应事件驱动或 damp 过渡。",
        "Root Motion 位移写入 `Animator.deltaPosition`，与 CharacterController 叠加需明确优先级。",
    ],
    "Addressables": [
        "ResourceManager 引用计数 + Catalog 解析是加载内核；Release 未配对等价于 AB 泄漏。",
        "Remote Catalog 更新可不改包体换资源地址，但需处理旧客户端与新 catalog 版本兼容。",
    ],
    "性能优化": [
        "移动 GPU 常 fill-rate/bandwidth bound，降分辨率与 Overdraw 往往比减 Draw Call 更有效。",
        "优化有效性用同场景同机型的 Profile 对比中位数帧时间，而非单次 FPS 截图。",
    ],
    "架构设计": [
        "边界清晰比模式名称重要：变化方向决定依赖方向，稳定层不依赖不稳定层。",
        "数据驱动把策划迭代从编译期挪到配置期，但需 Schema 校验与版本迁移。",
    ],
    "网络": [
        "可靠性与实时性不可兼得：TCP 保序增延迟，UDP 低延迟需应用层补可靠性。",
        "客户端预测提升手感，但必须以服务器快照校正防止永久分歧。",
    ],
    "Android/JNI": [
        "JNI 每次调用有固定开销，批量传递 primitive array 比逐字段 Get 更省。",
        "IL2CPP AOT 在构建期确定泛型实例，运行时无法 JIT 新组合。",
    ],
    "微信小游戏": [
        "Wasm 线性内存上限远低于原生 App，纹理/AudioClip 必须分档与流式。",
        "宿主 WebView 生命周期与 Unity Pause 不同步，需监听 `wx.onShow/onHide`。",
    ],
    "AI辅助开发": [
        "LLM 训练数据滞后于 Unity LTS 版本，生成代码可能调用已废弃 API。",
        "AI 适合样板与工具，架构决策与性能关键路径仍需工程师负责。",
    ],
    "项目深挖": [
        "STAR 的价值在于暴露决策链：为什么选 A 不选 B，而不是罗列功能清单。",
        "面试官会用细节追问（类名、帧数、线上数据）验证故事真实性，泛泛而谈会被扣分。",
    ],
}


def match_hints(title: str, std: str, chapter: str, qnum: int, max_hints: int = 4) -> list:
    text = f"{title} {std}"
    hits = []
    for pattern, hints in TOPIC_HINTS:
        if re.search(pattern, text, re.I):
            hits.extend(hints)
    if not hits:
        apis = CHAPTER_APIS.get(chapter, ["MonoBehaviour"])
        hits.append(f"结合 `{apis[qnum % len(apis)]}` 在 Editor 中复现并 Profile 验证。")
    seen = set()
    out = []
    for h in hits:
        if h not in seen:
            seen.add(h)
            out.append(h)
    return out[:max_hints]


def split_clauses(text: str) -> list:
    if not text:
        return []
    parts = re.split(r'[。；;]\s*', text)
    return [p.strip() for p in parts if p.strip()]


def expand_mistake(m: str, title: str) -> str:
    m = m.strip().rstrip("。")
    if not m:
        return ""
    consequences = {
        "不解绑": "对象销毁后仍收到回调 → MissingReferenceException 或逻辑重复执行",
        "Find": "O(n) 遍历 + 字符串比较，切场景后可能找到错误实例",
        "泄漏": "Memory Profiler 快照 Diff 可见引用链不断增，切场景内存不降",
        "async void": "异常无法被调用方捕获，Unity 控制台只报 UnobservedTaskException",
        "Static": "DontDestroyOnLoad 与域重载下静态字段残留旧 Scene 引用",
        "Material": "每次访问 `renderer.material` 实例化新材质，Draw Call 与内存双爆",
        "LINQ": "迭代器 + 委托分配，战斗 Update 每帧触发 GC Spike",
        "滥用": "过度优化或错误优化导致可读性下降且 Profile 无收益",
    }
    extra = "导致运行时异常或性能回退"
    for k, v in consequences.items():
        if k in m:
            extra = v
            break
    return f"{m}——{extra}"


def enrich_question(qnum: int, data: dict) -> dict:
    title = data["title"]
    fields = data["fields"]
    chapter = get_chapter(qnum)

    std = pick("标准答案", "答题框架", fields=fields)
    principle_src = pick("原理解析", "表达要点", fields=fields)
    project_src = pick("项目实战", "可举项目", fields=fields)
    mistakes_src = pick("常见错误", "常见扣分", fields=fields)
    exam = pick("考察点", fields=fields)

    if qnum in SPECIAL:
        return SPECIAL[qnum]

    if qnum >= 350:
        topic = re.sub(r"^(请介绍|如何介绍|你如何|如何回答)", "", title).strip("？? ")
        return {
            "answer": star_answer(qnum, title, fields),
            "principle": (
                f"项目深挖题考察表达结构与决策深度，而非再背知识点。"
                f"{principle_src or exam} "
                f"面试官通过追问类名、时序、数据验证 STAR 真实性。"
                f"{PRINCIPLE_DEEP.get(chapter, [''])[qnum % len(PRINCIPLE_DEEP.get(chapter, ['']))]}"
            ),
            "project": (
                f"准备与「{topic}」匹配的真实案例：{(project_src or '选你负责最深、能讲清取舍的一个系统').rstrip('。')}。"
                f"提前写清你的 Task 边界，Action 列 3 个技术关键词（如 Addressables/对象池/状态机），Result 准备 2 个数字。"
            ),
            "mistakes": bullets([
                expand_mistake(mistakes_src or "只罗列功能无决策", title),
                "只说「我们团队」不说「我负责」——贡献模糊，易被判定为旁听。",
                "Result 无数据、无复盘——显得夸大或缺乏工程严谨性。",
            ]),
        }

    answer_bullets = []
    if std:
        answer_bullets.append(f"**结论**：{std}")

    clauses = split_clauses(principle_src) or split_clauses(std)
    for c in clauses[:3]:
        if c and (not std or c not in std):
            answer_bullets.append(f"**机制**：{c}——理解这一点才能解释「为什么」而非只背结论。")

    if exam:
        answer_bullets.append(f"**考察点**：{exam}，面试时应先给出判断标准再展开。")

    for h in match_hints(title, std or "", chapter, qnum, max_hints=3):
        answer_bullets.append(h)

    unity_applies = CHAPTER_UNITY_APPLY.get(chapter, [])
    if project_src:
        answer_bullets.append(f"**项目落地**：{project_src}")
    if unity_applies:
        answer_bullets.append(unity_applies[qnum % len(unity_applies)])

    answer = bullets(answer_bullets[:8])

    deep_pool = PRINCIPLE_DEEP.get(chapter, ["底层实现决定上层用法边界。"])
    deep = deep_pool[qnum % len(deep_pool)]
    if principle_src and principle_src != std:
        principle = f"{principle_src} {deep}"
    elif exam:
        principle = f"从「{exam}」出发：{deep}"
    else:
        principle = deep

    apis = CHAPTER_APIS.get(chapter, ["MonoBehaviour"])
    api = apis[qnum % len(apis)]
    project_parts = []
    if project_src:
        project_parts.append(project_src.rstrip("。") + "。")
    project_parts.append(
        f"实现时可基于 `{api}` 做最小验证场景，在 Play Mode 用 Profiler 记录优化/改造前后数据，便于面试追问举证。"
    )
    project = " ".join(project_parts)

    mistake_items = []
    if mistakes_src:
        for m in re.split(r'[、,，]', mistakes_src):
            m = m.strip()
            if m:
                mistake_items.append(expand_mistake(m, title))
    if len(mistake_items) < 2:
        mistake_items.append(
            f"只背「{title.rstrip('？?')}」概念不在 Unity Editor 实测——追问细节（类名、API、Profile 数据）时露馅。"
        )
    if len(mistake_items) < 3:
        mistake_items.append(
            f"忽视 {exam or chapter} 相关边界（对象销毁、切场景、域重载、弱网）——集成阶段才暴露。"
        )

    return {
        "answer": answer,
        "principle": principle,
        "project": project,
        "mistakes": bullets(mistake_items[:3]),
    }


def main():
    md_text = MD_PATH.read_text(encoding="utf-8")
    questions = parse_questions(md_text)
    if len(questions) != 394:
        missing = [i for i in range(1, 395) if i not in questions]
        raise SystemExit(f"Expected 394 questions, got {len(questions)}. Missing: {missing[:20]}...")

    output = {}
    for qnum in range(1, 395):
        output[str(qnum)] = enrich_question(qnum, questions[qnum])

    with OUT_PATH.open("w", encoding="utf-8") as f:
        json.dump(output, f, ensure_ascii=False, indent=2)

    print(f"Wrote {len(output)} entries to {OUT_PATH}")


if __name__ == "__main__":
    main()
