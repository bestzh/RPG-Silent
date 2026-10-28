using RPGSilent.Domain;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

[DefaultExecutionOrder(-50)]
public class CameraControl : MonoBehaviour
{
    [Inject] private ICursorService _cursorService;
    [Inject] private IPlayerInputActions _input;
    [Inject] private IGameSettingsService gameSettings;

    public CinemachineCamera virtualCamera;
    public float rotationSpeed = 1f;
    [SerializeField, Min(0f)] private float viewHeight = 1.4f;

    private PlayerController player;
    private Transform viewTarget;
    private CinemachineThirdPersonFollow follow;
    private float freeDistance;
    private Vector3 freeShoulder;
    private Quaternion cameraRotation;
    private Vector3 lastPlayerPosition;
    private bool wasLocked;
    private float unlockBlendUntil;
    int hitSequence;
    float impactRemaining, impactAmplitude;

    private void Start()
    {
        player = transform.root.GetComponentInChildren<PlayerController>();
        if (player == null || virtualCamera == null) return;

        // A dedicated target carries pitch without tilting the physical capsule.
        viewTarget = new GameObject("Player View Target").transform;
        viewTarget.SetParent(transform.root, false);
        cameraRotation = player.ViewRotation;
        lastPlayerPosition = player.transform.position;
        UpdateViewTarget();
        virtualCamera.Follow = viewTarget;
        virtualCamera.LookAt = null;
        var hardLookAt = virtualCamera.GetComponent<CinemachineHardLookAt>();
        if (hardLookAt != null) hardLookAt.enabled = false;
        follow = virtualCamera.GetComponent<CinemachineThirdPersonFollow>();
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
            follow.AvoidObstacles.CameraRadius = .22f;
            follow.AvoidObstacles.DampingIntoCollision = 0f;
            follow.AvoidObstacles.DampingFromCollision = .35f;
            freeDistance = follow.CameraDistance;
            freeShoulder = follow.ShoulderOffset;
        }
        _cursorService?.EnterGameplayCursor(resetToHidden: true);
    }

    private void LateUpdate() => UpdateViewTarget();

    private void UpdateViewTarget()
    {
        if (viewTarget == null || player == null) return;
        Vector3 pivot = player.transform.position + Vector3.up * viewHeight;
        Vector3 displacement = player.transform.position - lastPlayerPosition;
        bool warped = displacement.sqrMagnitude > 64f;
        bool locked = player.TargetLock != null && player.TargetLock.IsLocked;
        if (wasLocked && !locked) unlockBlendUntil = Time.time + .35f;
        wasLocked = locked;
        Quaternion desired = player.ViewRotation;
        float distance = freeDistance;
        Vector3 shoulder = freeShoulder;
        if (locked)
        {
            Vector3 delta = player.TargetLock.AimPoint - pivot;
            float planar = new Vector2(delta.x, delta.z).magnitude;
            // Aim slightly above the midpoint, keeping feet and nearby opponents in view.
            float pitch = Mathf.Clamp(12f - Mathf.Atan2(delta.y, Mathf.Max(2f, planar)) * Mathf.Rad2Deg * .5f, -18f, 30f);
            if (planar > .15f) desired = Quaternion.Euler(pitch, Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 0);
            distance = Mathf.Clamp(freeDistance + Mathf.InverseLerp(2f, 14f, planar) * 1.4f, freeDistance, freeDistance + 1.4f);
            shoulder.x = .65f;
        }
        float blend = 1f - Mathf.Exp(-8f * Time.deltaTime);
        cameraRotation = warped || !locked && Time.time >= unlockBlendUntil ? desired : Quaternion.Slerp(cameraRotation, desired, blend);
        if (warped && virtualCamera != null) virtualCamera.OnTargetObjectWarped(viewTarget, displacement);
        var feedback = player.HitFeedback;
        if (feedback != null && feedback.Sequence != hitSequence)
        {
            hitSequence = feedback.Sequence;
            if (!feedback.Periodic && feedback.Visibility > 0)
            { impactRemaining = .18f; impactAmplitude = feedback.Impact == AttackImpact.Heavy ? .085f : .035f; }
        }
        if (!player.GameplayInputEnabled || !player.CanReceiveCombatEffects || warped) impactRemaining = 0;
        float scale = gameSettings?.CurrentSettings.ScreenShakeIntensity ?? 1f;
        if (impactRemaining > 0)
        {
            impactRemaining = Mathf.Max(0, impactRemaining - Time.deltaTime);
            float wave = Mathf.Sin((.18f - impactRemaining) * 95f) * impactAmplitude * impactRemaining / .18f * scale;
            pivot += cameraRotation * new Vector3(wave, wave * .4f, 0);
        }
        viewTarget.SetPositionAndRotation(pivot, cameraRotation);
        if (follow != null)
        {
            follow.CameraDistance = Mathf.Lerp(follow.CameraDistance, distance, blend);
            follow.ShoulderOffset = Vector3.Lerp(follow.ShoulderOffset, shoulder, blend);
        }
        lastPlayerPosition = player.transform.position;
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
