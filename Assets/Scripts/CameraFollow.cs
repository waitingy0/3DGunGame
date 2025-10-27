// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class CameraFollow : MonoBehaviour {

//     public Transform target;
//     public float smoothing = 5f;
//     Vector3 offset;
// 	// Use this for initialization
// 	void Start () {
//         offset = transform.position - target.position;
// 	}
	
// 	// Update is called once per frame
// 	void FixedUpdate () {
//         Vector3 targetCamPos = target.position + offset;
//         transform.position = Vector3.Lerp(transform.position, targetCamPos, smoothing * Time.deltaTime);
// 	}
// }

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;          // 玩家对象
    public float distance = 5f;       // 相机距离
    public float height = 2f;         // 相机高度
    public float mouseSensitivity = 3f; // 鼠标灵敏度
    public float rotationSmooth = 5f; // 平滑旋转速度
    public float minPitch = 5f;     // 最低俯仰角
    public float maxPitch = 45f;      // 最高仰角

    private float yaw = 0f;           // 水平角度
    private float pitch = 20f;        // 垂直角度

    void Start()
    {
        // 锁定鼠标光标，防止跑出屏幕
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 初始化角度
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 鼠标输入
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * mouseSensitivity;
        pitch -= mouseY * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 计算旋转
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        // 计算相机位置（围绕目标旋转）
        Vector3 offset = rotation * new Vector3(0, height, -distance);
        Vector3 targetPos = target.position + offset;

        // 平滑移动和旋转
        transform.position = Vector3.Lerp(transform.position, targetPos, rotationSmooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
