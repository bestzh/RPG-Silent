using System;

namespace RPGSilent.Domain
{
    public interface IUIService
    {
        void OpenUI(string uiKey, params object[] args);
        void CloseUI(string uiKey);
        void CloseAllUI();
        void PreloadUI(string uiKey, Action onComplete = null);
    }
}
