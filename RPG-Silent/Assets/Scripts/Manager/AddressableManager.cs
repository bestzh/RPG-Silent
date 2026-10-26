using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class AddressableManager : MonoBehaviour
{
    public static AddressableManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 全局根的唯一性由 GameLifetimeScope 决定，不销毁共享根上的其他服务。
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void LoadAndInstantiate(string address, Transform parent = null, Action<GameObject> onLoaded = null)
    {
        Addressables.InstantiateAsync(address, parent).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onLoaded?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogError($"加载资源失败: {address}");
            }
        };
    }
    public void LoadScene(string sceneAddress, bool additive = false, Action onLoaded = null)
    {
        Addressables.LoadSceneAsync(
            sceneAddress,
            additive ? LoadSceneMode.Additive : LoadSceneMode.Single
        ).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onLoaded?.Invoke();
            }
            else
            {
                Debug.LogError($"加载场景失败: {sceneAddress}");
            }
        };
    }
}
