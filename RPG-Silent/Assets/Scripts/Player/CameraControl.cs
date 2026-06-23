using RPGSilent.Domain;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

public class CameraControl : MonoBehaviour
{
    [Inject] private ICursorService _cursorService;

    public CinemachineCamera virtualCamera;
    public float rotationSpeed = 1f;

    private CinemachineHardLookAt freeLookCamera;

    private void Start()
    {
        freeLookCamera = virtualCamera.GetComponent<CinemachineHardLookAt>();
        _cursorService?.EnterGameplayCursor(resetToHidden: true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            _cursorService?.ToggleGameplayCursor();
    }
}
