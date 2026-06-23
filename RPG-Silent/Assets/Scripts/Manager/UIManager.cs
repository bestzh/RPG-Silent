using System;
using System.Collections.Generic;
using RPGSilent.Domain;
using UnityEngine;
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

    // 场景级容器（SceneLifetimeScope），游戏场景加载后由 SceneLifetimeScope 传入
    // 注入 MainUI 等场景级 UI 时优先使用，因为它拥有 IPlayerStatsReader 等场景注册
    private IObjectResolver _sceneResolver;

    private readonly Dictionary<string, GameObject>                        activeUIs      = new();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>>  loadedHandles  = new();
    private readonly Dictionary<string, GameObject>                        cachedUIs      = new();
    private readonly HashSet<string>                                       initializedUIs = new();

    /// <summary>GameLifetimeScope 构建完成后调用，传入全局容器。</summary>
    public void SetGlobalResolver(IObjectResolver resolver)
    {
        _container = resolver;
    }

    /// <summary>游戏场景加载后由 SceneLifetimeScope 调用，切换到场景级容器。</summary>
    public void SetSceneResolver(IObjectResolver resolver)
    {
        _sceneResolver = resolver;
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

        Transform parent = UIRoot != null ? UIRoot : transform;
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(uiKey, parent);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                // 先关闭，确保 InjectGameObject 在 OnEnable 之前完成
                // 避免子组件（如 ScreenPage）在注入前就触发 OnEnable
                op.Result.SetActive(false);
                loadedHandles[uiKey] = handle;
                OpenLoadedUI(uiKey, op.Result, args);
                return;
            }

            Debug.LogError($"[UIManager] 加载 UI 失败: {uiKey}");
        };
    }

    public void CloseUI(string uiKey)
    {
        if (!activeUIs.TryGetValue(uiKey, out GameObject uiObj)) return;

        uiObj.GetComponent<UIBase>()?.OnClose();
        activeUIs.Remove(uiKey);

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
        if (activeUIs.TryGetValue(uiKey, out uiObj)) return true;
        if (cachedUIs.TryGetValue(uiKey, out uiObj)) return true;

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

        Transform parent = UIRoot != null ? UIRoot : transform;
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(uiKey, parent);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject uiObj = op.Result;
                uiObj.name = uiKey;
                uiObj.SetActive(false);

                loadedHandles[uiKey] = handle;
                RegisterUI(uiKey, uiObj);
                onComplete?.Invoke();
                return;
            }

            Debug.LogError($"[UIManager] 预加载 UI 失败: {uiKey}");
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

        uiObj.GetComponent<UIBase>()?.Init(uiKey);
        initializedUIs.Add(uiKey);
    }
}
