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
    public bool IsLoading => isSceneLoading;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneKey, bool additive = false,
                          Action<float> onProgress = null, Action onComplete = null,
                          Action<Exception> onFailed = null)
    {
        if (isSceneLoading)
        {
            onFailed?.Invoke(new InvalidOperationException("另一个场景正在加载，请稍后重试。"));
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            ReportFailure(sceneKey, new ArgumentException("场景地址不能为空。"), onFailed);
            return;
        }

        // 先占用请求，连同步异常和已完成的失败句柄也必须走同一恢复路径。
        isSceneLoading = true;
        AsyncOperationHandle<SceneInstance> handle;
        try
        {
            LoadSceneMode mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            handle = Addressables.LoadSceneAsync(sceneKey, mode);
        }
        catch (Exception exception)
        {
            isSceneLoading = false;
            ReportFailure(sceneKey, exception, onFailed);
            return;
        }

        handle.Completed += completedHandle =>
        {
            isSceneLoading = false;

            if (completedHandle.Status == AsyncOperationStatus.Succeeded)
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke();
                return;
            }

            Exception failure = completedHandle.OperationException ??
                                new InvalidOperationException("场景加载未成功完成。");
            // 成功场景由 Addressables 在卸载时释放；失败句柄由本服务释放。
            Addressables.Release(completedHandle);
            ReportFailure(sceneKey, failure, onFailed);
        };

        StartCoroutine(TrackProgressCoroutine(handle, onProgress));
    }

    private IEnumerator TrackProgressCoroutine(AsyncOperationHandle<SceneInstance> handle,
                                               Action<float> onProgress)
    {
        while (handle.IsValid() && !handle.IsDone)
        {
            onProgress?.Invoke(handle.PercentComplete);
            yield return null;
        }
    }

    private static void ReportFailure(string sceneKey, Exception failure, Action<Exception> onFailed)
    {
        Debug.LogError($"[SceneLoader] 场景加载失败: {sceneKey}\n{failure.Message}");
        onFailed?.Invoke(failure);
    }
}
