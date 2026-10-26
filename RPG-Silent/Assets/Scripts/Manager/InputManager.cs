using RPGSilent.Domain;
using UnityEngine;
using VContainer;

/// <summary>
/// 输入服务，实现 IInputService 接口。
/// 移动方向通过 IPlayerInputActions.Move（New Input System 2DVector 复合体）读取，
/// 支持运行时改键。
/// 由 VContainer 的 GameLifetimeScope 注册并管理。
/// </summary>
public class InputManager : MonoBehaviour, IInputService
{
    [Inject] private IPlayerInputActions _playerInputActions;

    public Vector2 MoveInput { get; private set; }

    private void Awake()
    {
        // Main 中的旧场景组件不是全局容器注册的服务，不应跨场景累积。
        var scope = GameLifetimeScope.Instance;
        if (scope != null && scope.Container != null &&
            !ReferenceEquals(scope.Container.Resolve<IInputService>(), this))
        {
            enabled = false;
            Destroy(this);
        }
    }

    private void Update()
    {
        MoveInput = (_playerInputActions?.MoveInput ?? Vector2.zero).normalized;
    }
}
