using System;

namespace RPGSilent.Domain
{
    public interface ISceneLoader
    {
        bool IsLoading { get; }

        void LoadScene(string key, bool additive = false,
                       Action<float> onProgress = null,
                       Action onComplete = null,
                       Action<Exception> onFailed = null);
    }
}
