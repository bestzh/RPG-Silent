using RPGSilent.Domain;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

public class CameraControl : MonoBehaviour
{
    [Inject] private ICursorService _cursorService;
    [Inject] private IPlayerInputActions _input;

    public CinemachineCamera virtualCamera;
    public float rotationSpeed = 1f;

    private CinemachineHardLookAt freeLookCamera;

    private void Start()
    {
        freeLookCamera = virtualCamera.GetComponent<CinemachineHardLookAt>();
        _cursorService?.EnterGameplayCursor(resetToHidden: true);
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
