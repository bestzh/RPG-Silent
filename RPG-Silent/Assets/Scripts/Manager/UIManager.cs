using System;
using System.Collections.Generic;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

/// <summary>
/// UI 生命周期管理器，实现 IUIService 接口。
/// 由 VContainer 的 GameLifetimeScope 注册并管理，不再使用静态单例。
/// 加载 UI Prefab 后自动调用 VContainer 注入，使 UIBase 子类可使用 [Inject]。
/// </summary>
public class UIManager : MonoBehaviour, IUIService
{
    // 全局容器（GameLifetimeScope），由 GameLifetimeScope.RegisterBuildCallback 显式传入
    private IObjectResolver _container;
    private GameAudioService audio;

    // 场景级容器（SceneLifetimeScope），游戏场景加载后由 SceneLifetimeScope 传入
    // 注入 MainUI 等场景级 UI 时优先使用，因为它拥有 IPlayerStatsReader 等场景注册
    private IObjectResolver _sceneResolver;

    private readonly Dictionary<string, GameObject>                        activeUIs      = new();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>>  loadedHandles  = new();
    private readonly Dictionary<string, GameObject>                        cachedUIs      = new();
    private readonly HashSet<string>                                       initializedUIs = new();
    private readonly Dictionary<string, PendingUI>                         pendingUIs     = new();

    private sealed class PendingUI
    {
        public bool Cache;
        public bool Open;
        public object[] Args;
        public Action OnPreloaded;
    }

    /// <summary>GameLifetimeScope 构建完成后调用，传入全局容器。</summary>
    public void SetGlobalResolver(IObjectResolver resolver)
    {
        _container = resolver;
        resolver.TryResolve<GameAudioService>(out audio);
    }

    /// <summary>游戏场景加载后由 SceneLifetimeScope 调用，切换到场景级容器。</summary>
    public void SetSceneResolver(IObjectResolver resolver)
    {
        _sceneResolver = resolver;
    }

    public bool IsSceneResolver(IObjectResolver resolver) => ReferenceEquals(_sceneResolver, resolver);

    /// <summary>释放属于已卸载场景的 UI 缓存，避免保留已经 Dispose 的场景服务。</summary>
    public void ReleaseUI(string uiKey)
    {
        // 请求完成后会检查身份；被取消的旧结果只释放，不再缓存或打开。
        pendingUIs.Remove(uiKey);
        bool wasCached = cachedUIs.TryGetValue(uiKey, out GameObject cachedObj);
        CloseUI(uiKey);
        if (!wasCached) return;

        cachedUIs.Remove(uiKey);
        initializedUIs.Remove(uiKey);
        if (loadedHandles.TryGetValue(uiKey, out var handle))
        {
            loadedHandles.Remove(uiKey);
            Addressables.ReleaseInstance(handle);
        }
        else if (cachedObj != null)
            Destroy(cachedObj);
    }

    public Transform UIRoot;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void OpenUI(string uiKey, params object[] args)
    {
        if (activeUIs.TryGetValue(uiKey, out GameObject activeObj))
        {
            BringToFront(activeObj);
            activeObj.GetComponent<UIBase>()?.OnOpen(args);
            return;
        }

        if (cachedUIs.TryGetValue(uiKey, out GameObject cachedObj))
        {
            OpenLoadedUI(uiKey, cachedObj, args);
            return;
        }

        if (pendingUIs.TryGetValue(uiKey, out PendingUI pending))
        {
            pending.Open = true;
            pending.Args = args;
            return;
        }

        LoadUI(uiKey, new PendingUI { Open = true, Args = args });
    }

    public void CloseUI(string uiKey)
    {
        if (pendingUIs.TryGetValue(uiKey, out PendingUI pending))
        {
            pending.Open = false;
            if (!pending.Cache) pendingUIs.Remove(uiKey);
        }

        if (!activeUIs.TryGetValue(uiKey, out GameObject uiObj)) return;
        activeUIs.Remove(uiKey);

        // Scene teardown can destroy the instance before its scoped service is
        // disposed. Remove ownership before callbacks, including reentrant Close.
        if (uiObj == null)
        {
            cachedUIs.Remove(uiKey);
            initializedUIs.Remove(uiKey);
            if (loadedHandles.TryGetValue(uiKey, out var staleHandle))
            {
                loadedHandles.Remove(uiKey);
                if (staleHandle.IsValid()) Addressables.Release(staleHandle);
            }
            return;
        }
        uiObj.GetComponent<UIBase>()?.OnClose();

        if (cachedUIs.ContainsKey(uiKey))
        {
            uiObj.SetActive(false);
            return;
        }

        if (loadedHandles.TryGetValue(uiKey, out AsyncOperationHandle<GameObject> handle))
        {
            Addressables.ReleaseInstance(handle);
            loadedHandles.Remove(uiKey);
            initializedUIs.Remove(uiKey);
            return;
        }

        Destroy(uiObj);
        initializedUIs.Remove(uiKey);
    }

    public void CloseAllUI()
    {
        foreach (string uiKey in new List<string>(pendingUIs.Keys))
            CloseUI(uiKey);

        foreach (string uiKey in new List<string>(activeUIs.Keys))
        {
            CloseUI(uiKey);
        }
    }

    public bool IsUIOpen(string uiKey) => activeUIs.ContainsKey(uiKey);

    public void SetRaycastEnabled(string uiKey, bool enabled)
    {
        if (!TryGetUIGameObject(uiKey, out GameObject uiObj)) return;

        var group = uiObj.GetComponent<CanvasGroup>();
        if (group == null) group = uiObj.AddComponent<CanvasGroup>();

        group.blocksRaycasts = enabled;
        group.interactable   = enabled;
    }

    private bool TryGetUIGameObject(string uiKey, out GameObject uiObj)
    {
        // 注意：用 Unity 的 != null 判断，已销毁的 GameObject 会被视为 null，
        // 避免场景切换时字典里残留的悬空引用导致 MissingReferenceException。
        if (activeUIs.TryGetValue(uiKey, out uiObj) && uiObj != null) return true;
        if (cachedUIs.TryGetValue(uiKey, out uiObj) && uiObj != null) return true;

        uiObj = null;
        return false;
    }

    public void PreloadUI(string uiKey, Action onComplete = null)
    {
        if (cachedUIs.ContainsKey(uiKey))
        {
            onComplete?.Invoke();
            return;
        }

        if (activeUIs.TryGetValue(uiKey, out GameObject activeObj))
        {
            cachedUIs[uiKey] = activeObj;
            onComplete?.Invoke();
            return;
        }

        if (pendingUIs.TryGetValue(uiKey, out PendingUI pending))
        {
            pending.Cache = true;
            pending.OnPreloaded += onComplete;
            return;
        }

        LoadUI(uiKey, new PendingUI { Cache = true, OnPreloaded = onComplete });
    }

    private void LoadUI(string uiKey, PendingUI request)
    {
        pendingUIs[uiKey] = request;
        Transform parent = UIRoot != null ? UIRoot : transform;
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(uiKey, parent);
        handle.Completed += op =>
        {
            if (this == null || !pendingUIs.TryGetValue(uiKey, out PendingUI current) ||
                !ReferenceEquals(current, request))
            {
                if (op.Status == AsyncOperationStatus.Succeeded) Addressables.ReleaseInstance(op);
                else Addressables.Release(op);
                return;
            }

            pendingUIs.Remove(uiKey);
            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(op);
                Debug.LogError($"[UIManager] 加载 UI 失败: {uiKey}");
                return;
            }

            GameObject uiObj = op.Result;
            uiObj.SetActive(false);
            loadedHandles[uiKey] = op;
            if (request.Cache)
            {
                uiObj.name = uiKey;
                RegisterUI(uiKey, uiObj);
            }

            if (request.Open) OpenLoadedUI(uiKey, uiObj, request.Args);
            request.OnPreloaded?.Invoke();
        };
    }

    public void RegisterUI(string key, GameObject ui)
    {
        if (cachedUIs.ContainsKey(key)) return;

        cachedUIs[key] = ui;
        InitUI(key, ui);
    }

    public void OpenCachedUI(string key, string sceneName)
    {
        if (cachedUIs.TryGetValue(key, out GameObject ui))
        {
            OpenLoadedUI(key, ui, sceneName);
        }
    }

    private void OpenLoadedUI(string uiKey, GameObject uiObj, params object[] args)
    {
        // 优先使用场景级容器（含 IPlayerStatsReader 等），没有时退回全局容器
        IObjectResolver resolver = _sceneResolver ?? _container;
        if (resolver == null)
            Debug.LogError($"[UIManager] 容器为 null，{uiKey} 的依赖注入将跳过。" +
                           "请确认 GameLifetimeScope 已正确初始化。");
        else
            resolver.InjectGameObject(uiObj);

        InitUI(uiKey, uiObj);
        BringToFront(uiObj);
        uiObj.SetActive(true);
        activeUIs[uiKey] = uiObj;
        uiObj.GetComponent<UIBase>()?.OnOpen(args);
    }

    private static void BringToFront(GameObject uiObj)
    {
        if (uiObj != null)
            uiObj.transform.SetAsLastSibling();
    }

    private void InitUI(string uiKey, GameObject uiObj)
    {
        if (initializedUIs.Contains(uiKey)) return;

        var owner = uiObj.GetComponent<UIBase>();
        if (owner != null) { owner.Audio = audio; owner.Init(uiKey); }
        foreach (var button in uiObj.GetComponentsInChildren<Button>(true))
            if (button.GetComponent<UiAudioFeedback>() == null) button.gameObject.AddComponent<UiAudioFeedback>();
        initializedUIs.Add(uiKey);
    }
}
