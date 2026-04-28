using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoaderManager : MonoBehaviour
{
    public static SceneLoaderManager Instance;

    private bool isSceneLoading;

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

    public void LoadScene(string sceneKey, bool additive = false, Action<float> onProgress = null, Action onComplete = null)
    {
        if (isSceneLoading)
        {
            Debug.LogWarning($"Scene is already loading: {sceneKey}");
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

            Debug.LogError($"Scene load failed: {sceneKey}");
        };

        StartCoroutine(TrackProgressCoroutine(handle, onProgress));
    }

    private IEnumerator TrackProgressCoroutine(AsyncOperationHandle<SceneInstance> handle, Action<float> onProgress)
    {
        while (!handle.IsDone)
        {
            onProgress?.Invoke(handle.PercentComplete);
            yield return null;
        }
    }
}
