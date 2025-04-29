using Unity.Cinemachine;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public CinemachineCamera virtualCamera;
    public float rotationSpeed = 1f;
    private bool isCursorVisible = true; 

    private CinemachineHardLookAt freeLookCamera;

    void Start()
    {
        // 获取 CinemachineFreeLook 组件
        freeLookCamera = virtualCamera.GetComponent<CinemachineHardLookAt>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            ToggleCursorVisibility();
        }
        // RotateCamera();
    }
    void RotateCamera()
    {
        // 获取鼠标输入
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");


    //     freeLookCamera.LookAtOffset.y += mouseY * rotationSpeed;

    // //     // 限制Y轴的旋转范围，防止超出上下边界
    //     freeLookCamera.LookAtOffset.y = Mathf.Clamp(freeLookCamera.LookAtOffset.y, -0.8f, 0.8f);
    }

    private void ToggleCursorVisibility()
    {
        if (isCursorVisible)
        {
            Cursor.visible = false; // 隐藏光标
            Cursor.lockState = CursorLockMode.Locked; // 锁定光标
        }
        else
        {
            Cursor.visible = true; // 显示光标
            Cursor.lockState = CursorLockMode.None; // 解锁光标
        }

        // 切换状态
        isCursorVisible = !isCursorVisible;
    }
}
