using System;
using System.Collections;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景加载服务，实现 ISceneLoader 接口。
/// 由 VContainer 的 GameLifetimeScope 注册并管理，不再使用静态单例。
/// </summary>
public class SceneLoaderManager : MonoBehaviour, ISceneLoader
{
    private bool isSceneLoading;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneKey, bool additive = false,
                          Action<float> onProgress = null, Action onComplete = null)
    {
        if (isSceneLoading)
        {
            Debug.LogWarning($"[SceneLoader] 场景正在加载中，忽略重复请求: {sceneKey}");
            return;
        }

        LoadSceneMode mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
        AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(sceneKey, mode);
        isSceneLoading = true;

        handle.Completed += completedHandle =>
        {
            isSceneLoading = false;

            if (completedHandle.Status == AsyncOperationStatus.Succeeded)
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke();
                return;
            }

            Debug.LogError($"[SceneLoader] 场景加载失败: {sceneKey}");
        };

        StartCoroutine(TrackProgressCoroutine(handle, onProgress));
    }

    private IEnumerator TrackProgressCoroutine(AsyncOperationHandle<SceneInstance> handle,
                                               Action<float> onProgress)
    {
        while (!handle.IsDone)
        {
            onProgress?.Invoke(handle.PercentComplete);
            yield return null;
        }
    }
}
