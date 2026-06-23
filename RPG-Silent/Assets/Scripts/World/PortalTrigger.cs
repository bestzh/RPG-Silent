using RPGSilent.Domain;
using UnityEngine;
using VContainer;

/// <summary>
/// 场景中的传送门触发器：玩家进入触发范围时打开对应的传送门 UI。
/// 依赖由 SceneLifetimeScope 在场景构建完成后注入。
/// </summary>
[RequireComponent(typeof(Collider))]
public class PortalTrigger : MonoBehaviour
{
    [Tooltip("对应 PortalDatabase 中的传送门 ID")]
    [SerializeField] private int portalId = 1;

    [Tooltip("可触发该传送门的对象 Tag")]
    [SerializeField] private string playerTag = "Player";

    [Inject] private IPortalService _portalService;

    private bool _playerInside;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_playerInside || !other.CompareTag(playerTag)) return;

        _playerInside = true;

        if (_portalService == null)
        {
            Debug.LogError(
                "[PortalTrigger] IPortalService 未注入。请确认场景中存在 SceneLifetimeScope " +
                "且已注册 PortalService。", this);
            return;
        }

        _portalService.OpenPortal(portalId);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInside = false;
    }
}
