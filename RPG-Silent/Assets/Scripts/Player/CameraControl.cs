using Unity.Cinemachine;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
     public Transform player;  // 玩家对象
    public CinemachineCamera virtualCamera;
    public float rotationSpeed = 1f;
    public float distance = 5f;  // 摄像机与玩家的距离
    public float height = 2f;  // 摄像机相对玩家的高度

    private CinemachineHardLookAt freeLookCamera;
    private bool isAltPressed = false;

    void Start()
    {
        // 获取 CinemachineFreeLook 组件
        freeLookCamera = virtualCamera.GetComponent<CinemachineHardLookAt>();
    }

    void Update()
    {
        // 检查是否按下 Alt 键
        isAltPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (isAltPressed)
        {
            // 当按下 Alt 键时，启用自由视角模式
            // RotateCamera();
            RotateAroundPlayer();
        }
        else
        {
            // 当释放 Alt 键时，恢复默认的跟随视角
            freeLookCamera.LookAtOffset= Vector3.zero;
            // freeLookCamera.LookAtOffset.x = 0;

        }
    }

    void RotateAroundPlayer()
    {
        // 获取鼠标输入
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 控制摄像机旋转
        Vector3 direction = new Vector3(0, 0, -distance);
        Quaternion rotation = Quaternion.Euler(mouseY * rotationSpeed, mouseX * rotationSpeed, 0);
        
        // 计算摄像机的新位置
        Vector3 newPosition = player.position + rotation * direction + new Vector3(0, height, 0);
        
        // 设置摄像机的位置和朝向
        freeLookCamera.transform.position = newPosition;
        freeLookCamera.transform.LookAt(player);
    }
    void RotateCamera()
    {
        // 获取鼠标输入
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

    //     // 控制摄像机旋转
        freeLookCamera.LookAtOffset.x += mouseX * rotationSpeed;
        freeLookCamera.LookAtOffset.y += mouseY * rotationSpeed;

    //     // 限制Y轴的旋转范围，防止超出上下边界
        freeLookCamera.LookAtOffset.y = Mathf.Clamp(freeLookCamera.LookAtOffset.y, -0.8f, 0.8f);
    }
}
