using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private readonly Dictionary<string, GameObject> activeUIs = new();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> loadedHandles = new();
    private readonly Dictionary<string, GameObject> cachedUIs = new();
    private readonly HashSet<string> initializedUIs = new();

    public Transform UIRoot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    public void OpenUI(string uiKey, params object[] args)
    {
        if (activeUIs.TryGetValue(uiKey, out GameObject activeObj))
        {
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
                loadedHandles[uiKey] = handle;
                OpenLoadedUI(uiKey, op.Result, args);
                return;
            }

            Debug.LogError($"Load UI failed: {uiKey}");
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

            Debug.LogError($"Preload UI failed: {uiKey}");
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
        InitUI(uiKey, uiObj);
        uiObj.SetActive(true);
        activeUIs[uiKey] = uiObj;
        uiObj.GetComponent<UIBase>()?.OnOpen(args);
    }

    private void InitUI(string uiKey, GameObject uiObj)
    {
        if (initializedUIs.Contains(uiKey)) return;

        uiObj.GetComponent<UIBase>()?.Init(uiKey);
        initializedUIs.Add(uiKey);
    }
}
