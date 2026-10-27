using RPGSilent.Domain;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

[DefaultExecutionOrder(-50)]
public class CameraControl : MonoBehaviour
{
    [Inject] private ICursorService _cursorService;
    [Inject] private IPlayerInputActions _input;

    public CinemachineCamera virtualCamera;
    public float rotationSpeed = 1f;
    [SerializeField, Min(0f)] private float viewHeight = 1.4f;

    private PlayerController player;
    private Transform viewTarget;

    private void Start()
    {
        player = transform.root.GetComponentInChildren<PlayerController>();
        if (player == null || virtualCamera == null) return;

        // A dedicated target carries pitch without tilting the physical capsule.
        viewTarget = new GameObject("Player View Target").transform;
        viewTarget.SetParent(transform.root, false);
        UpdateViewTarget();
        virtualCamera.Follow = viewTarget;
        virtualCamera.LookAt = null;
        var hardLookAt = virtualCamera.GetComponent<CinemachineHardLookAt>();
        if (hardLookAt != null) hardLookAt.enabled = false;
        var follow = virtualCamera.GetComponent<CinemachineThirdPersonFollow>();
        if (follow != null)
        {
            // Aim around the upper torso instead of above the head. Keep the
            // authored camera distance and horizontal shoulder offset.
            Vector3 shoulder = follow.ShoulderOffset;
            shoulder.y = 0f;
            follow.ShoulderOffset = shoulder;
            follow.VerticalArmLength = 0f;
            follow.Damping = new Vector3(0.05f, 0.1f, 0.05f);
            follow.AvoidObstacles.Enabled = true;
            follow.AvoidObstacles.CollisionFilter = Physics.DefaultRaycastLayers;
            follow.AvoidObstacles.IgnoreTag = "Player";
        }
        _cursorService?.EnterGameplayCursor(resetToHidden: true);
    }

    private void LateUpdate() => UpdateViewTarget();

    private void UpdateViewTarget()
    {
        if (viewTarget != null && player != null)
            viewTarget.SetPositionAndRotation(player.transform.position + Vector3.up * viewHeight, player.ViewRotation);
    }

    private void OnDestroy()
    {
        if (viewTarget != null) Destroy(viewTarget.gameObject);
    }

    private void OnEnable()
    {
        if (_input != null) _input.CursorToggleTriggered += OnCursorToggleTriggered;
    }

    private void OnDisable()
    {
        if (_input != null) _input.CursorToggleTriggered -= OnCursorToggleTriggered;
    }

    private void OnCursorToggleTriggered() => _cursorService?.ToggleGameplayCursor();
}
