using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoaderManager : MonoBehaviour
{
    public static SceneLoaderManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void LoadScene(string sceneKey, bool additive = false, Action<float> onProgress = null, Action onComplete = null)
    {
        var mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;

        Addressables.LoadSceneAsync(sceneKey, mode).Completed += (AsyncOperationHandle<SceneInstance> handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke();
            }
            else
            {
                Debug.LogError($"场景加载失败：{sceneKey}");
            }
        };

        // Progress 模拟处理（可选）
        StartCoroutine(TrackProgressCoroutine(sceneKey, onProgress));
    }

    private System.Collections.IEnumerator TrackProgressCoroutine(string sceneKey, Action<float> onProgress)
    {
        AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(sceneKey);
        while (!handle.IsDone)
        {
            onProgress?.Invoke(handle.PercentComplete);
            yield return null;
        }
    }
}
