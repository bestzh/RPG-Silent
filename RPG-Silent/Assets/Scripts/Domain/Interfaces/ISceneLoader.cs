using System;

namespace RPGSilent.Domain
{
    public interface ISceneLoader
    {
        void LoadScene(string key, bool additive = false,
                       Action<float> onProgress = null,
                       Action onComplete = null);
    }
}
