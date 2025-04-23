using UnityEngine;

public class GameStart : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UIManager.Instance.OpenUI("UI/StartUI");
        UIManager.Instance.PreloadUI("UI/LoadingUI");
    }


}
