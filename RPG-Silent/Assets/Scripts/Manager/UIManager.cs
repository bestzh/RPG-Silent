using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private Dictionary<string, GameObject> activeUIs = new();
    private Dictionary<string, AsyncOperationHandle<GameObject>> loadedHandles = new();
    
    private Dictionary<string, GameObject> cachedUIs = new();

    public Transform UIRoot;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void OpenUI(string uiKey, params object[] args)
    {
        // 优先使用缓存的 UI
        if (cachedUIs.TryGetValue(uiKey, out var cachedObj))
        {
            cachedObj.SetActive(true);
            var uiBase = cachedObj.GetComponent<UIBase>();
            uiBase?.OnOpen(args);

            activeUIs[uiKey] = cachedObj;
            return;
        }

        // 如果当前已打开则直接触发 OnOpen
        if (activeUIs.ContainsKey(uiKey))
        {
            activeUIs[uiKey].GetComponent<UIBase>().OnOpen(args);
            return;
        }

        // 没缓存也没打开，进行加载
        var handle = Addressables.InstantiateAsync(uiKey, UIRoot);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject uiObj = op.Result;
                loadedHandles[uiKey] = handle;
                activeUIs[uiKey] = uiObj;

                var uiBase = uiObj.GetComponent<UIBase>();
                uiBase.Init(uiKey);
                uiBase.OnOpen(args);
            }
            else
            {
                Debug.LogError($"加载 UI 失败：{uiKey}");
            }
        };
    }

    public void CloseUI(string uiKey)
    {
        if (!activeUIs.ContainsKey(uiKey)) return;

        GameObject uiObj = activeUIs[uiKey];
        var uiBase = uiObj.GetComponent<UIBase>();
        uiBase.OnClose();

        if (loadedHandles.TryGetValue(uiKey, out var handle))
        {
            Addressables.ReleaseInstance(handle);
        }
        else
        {
            GameObject.Destroy(uiObj); // fallback
        }

        activeUIs.Remove(uiKey);
        loadedHandles.Remove(uiKey);
    }

    public void CloseAllUI()
    {
        foreach (var ui in new List<string>(activeUIs.Keys))
        {
            CloseUI(ui);
        }
    }

    public void PreloadUI(string uiKey, Action onComplete = null)
    {
        if (cachedUIs.ContainsKey(uiKey))
        {
            onComplete?.Invoke();
            return;
        }

        var handle = Addressables.InstantiateAsync(uiKey, UIRoot);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject uiObj = op.Result;
                uiObj.name = uiKey;
                uiObj.SetActive(false);
                RegisterUI(uiKey, uiObj);
                onComplete?.Invoke();
            }
            else
            {
                Debug.LogError($"预加载 UI 失败：{uiKey}");
            }
        };
    }


    public void RegisterUI(string key, GameObject ui)
    {
        if (!cachedUIs.ContainsKey(key))
            cachedUIs[key] = ui;
    }

    public void OpenCachedUI(string key, string sceneName)
    {
        if (cachedUIs.TryGetValue(key, out var ui))
        {
            ui.SetActive(true);
            ui.GetComponent<UIBase>()?.OnOpen(sceneName);
        }
    }
}
