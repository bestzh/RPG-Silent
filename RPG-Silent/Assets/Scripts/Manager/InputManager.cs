using RPGSilent.Domain;
using UnityEngine;

/// <summary>
/// 输入服务，实现 IInputService 接口。
/// 由 VContainer 的 GameLifetimeScope 注册并管理，不再使用静态单例。
/// </summary>
public class InputManager : MonoBehaviour, IInputService
{
    public Vector2 MoveInput { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(h, v).normalized;
    }
}
