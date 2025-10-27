// using UnityEngine;
// using System.Collections;


// public class PlayerMovement : MonoBehaviour
// {
//     public static PlayerMovement instance;
//     public float speed = 0;

//     [Header("移动参数")]
//     public float moveSpeed = 6f;         // 地面移动速度
//     public float airControl = 0.5f;      // 空中控制比率（空中移动较弱）
//     public float rotationSpeed = 10f;    // 转身平滑度

//     [Header("跳跃参数")]
//     public float jumpForce = 7f;         // 跳跃力度
//     public float groundCheckDistance = 1.2f; // 地面检测距离

//     Vector3 movement;
//     Animator anim;
//     Rigidbody playerRigidbody;
//     int floorMask;
//     float camRayLength = 100f;

//     public Vector3 GetMousePoint { get; set; }
//     void Awake()
//     {
//         floorMask = LayerMask.GetMask("Floor");
//         anim = GetComponent<Animator>();
//         playerRigidbody = GetComponent<Rigidbody>();

//         instance = this;
//     }

//     void Update()
//     {
//         // 检测空格键——跳跃
//         if (Input.GetKeyDown(KeyCode.Space))
//         {
//             Jump();
//         }

//         // 检测左Shift键——滑铲
//         if (Input.GetKeyDown(KeyCode.LeftShift))
//         {
//             Slide();
//         }
//     }

//     void FixedUpdate()
//     {
//         float h = Input.GetAxis("Horizontal");
//         float v = Input.GetAxis("Vertical");

//         //displacement
//         Move(h, v);
//         //make player to the mouse
//         Turning();
//         //Play the animation based on whether you are walking or not
//         Animating(h, v);
//     }

//     void Move(float h,float v)
//     {
//         // movement.Set(h, 0, v);
//         // movement = movement.normalized * speed * Time.deltaTime;
//         // playerRigidbody.MovePosition(transform.position + movement);
        
//         // 取得摄像机的正前和正右方向（但去掉上下倾斜部分）
//         Vector3 camForward = Camera.main.transform.forward;
//         Vector3 camRight = Camera.main.transform.right;
//         camForward.y = 0;
//         camRight.y = 0;
//         camForward.Normalize();
//         camRight.Normalize();

//         // 计算相对相机方向的移动
//         Vector3 moveDir = (camForward * v + camRight * h).normalized;

//         // 移动
//         playerRigidbody.MovePosition(transform.position + moveDir * speed * Time.deltaTime);

//         // 朝移动方向转身（如果有输入）
//         if (moveDir.magnitude > 0.1f)
//         {
//             Quaternion newRotation = Quaternion.LookRotation(moveDir);
//             playerRigidbody.MoveRotation(Quaternion.Slerp(playerRigidbody.rotation, newRotation, 10f * Time.deltaTime));
//         }
//     }

//     // Keep the player's orientation pointing to the point where the mouse hits on the ground
//     // y is height
//     void Turning()
//     {
//         Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
//         RaycastHit floorHit;
//         if(Physics.Raycast(camRay,out floorHit, camRayLength, floorMask))
//         {
//             Vector3 playerToMouse = floorHit.point - transform.position;
//             GetMousePoint = playerToMouse;
//             playerToMouse.y = 0f;
//             Quaternion newRotation = Quaternion.LookRotation(playerToMouse);
//             playerRigidbody.MoveRotation(newRotation);
//         }
//         Debug.DrawRay(Camera.main.transform.position, floorHit.point, Color.red);
//         Debug.DrawLine(Camera.main.transform.position, floorHit.point, Color.red);
//     }

//     void Animating(float h,float v)
//     {
//         bool walking = h != 0f || v != 0f;

//         // IsWalking(parameter) is connected to the animated state machine
//         anim.SetBool("IsWalking", walking);
//     }

//     void Jump()
//     {
//         // 给刚体一个向上的力
//         if (playerRigidbody.velocity.y == 0)  // 防止多段跳
//         {
//             playerRigidbody.AddForce(Vector3.up * 900f);
//             anim.SetTrigger("Jump");
//         }
//     }

//     void Slide()
//     {
//         // 触发滑铲动画
//         anim.SetTrigger("Slide");

//         // 可选：短时间降低碰撞体高度或加速移动
//         StartCoroutine(SlideCoroutine());
//     }

//     IEnumerator SlideCoroutine()
//     {
//         // 临时提升速度
//         float oldSpeed = speed;
//         speed *= 2f;
//         yield return new WaitForSeconds(0.5f);
//         speed = oldSpeed;
//     }


// }


using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement instance;

    [Header("Move")]
    public float moveSpeed = 6f;          // 地面最大速度
    public float acceleration = 20f;      // 地面加速度
    public float deceleration = 25f;      // 地面减速度
    public float airAcceleration = 6f;    // 空中“微控制”

    [Header("Jump")]
    public float jumpHeight = 1.6f;       // 跳跃高度(米)
    public float coyoteTime = 0.12f;      // 土狼时间（离地后可跳的宽容窗口）
    public float jumpBuffer = 0.12f;      // 跳跃缓存（提早按跳的宽容窗口）

    [Header("Ground Check")]
    public float groundCheckRadius = 0.2f;
    public float groundCheckOffset = 0.05f;
    public LayerMask groundMask;

    [Header("Camera-relative")]
    public Transform cameraTransform;     // 主摄像机 Transform

    [Header("Slide")]
    public float slideBoost = 1.8f;       // 滑铲速度倍率
    public float slideTime = 0.5f;

    Animator anim;
    Rigidbody rb;
    CapsuleCollider col;

    // 状态
    bool grounded;
    float lastGroundedTime;
    float lastJumpPressedTime;

    // 动画/输入
    Vector2 input;            // (h, v)
    bool wantJump;
    bool wantSlide;

    void Awake()
    {
        instance = this;
        anim = GetComponent<Animator>();
        rb   = GetComponent<Rigidbody>();
        col  = GetComponent<CapsuleCollider>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // 刚体推荐设置
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // 平滑
    }

    void Update()
    {
        // 采集输入（不要在 FixedUpdate 读输入）
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input = input.normalized;

        if (Input.GetKeyDown(KeyCode.Space))
            lastJumpPressedTime = Time.time;  // 记录按下跳跃的时刻

        wantSlide = Input.GetKeyDown(KeyCode.LeftShift);

        // 简单行走动画开关
        bool walking = input.sqrMagnitude > 0.01f && grounded;
        anim.SetBool("IsWalking", walking);
    }

    void FixedUpdate()
    {
        GroundCheck();                 // 稳定的落地检测
        HandleMovement();              // 相机相对移动 + 加速/减速
        HandleFacing();                // 朝移动方向转身
        HandleJumpAndBuffer();         // coyote + buffer 的跳跃
        HandleSlide();                 // 滑铲（可选）
    }

    void GroundCheck()
    {
        // 胶囊底部位置
        Vector3 center = transform.position + col.center;
        float bottom = center.y - (col.height * 0.5f) + col.radius + groundCheckOffset;
        Vector3 spherePos = new Vector3(center.x, bottom, center.z);

        grounded = Physics.CheckSphere(spherePos, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (grounded) lastGroundedTime = Time.time;
    }

    void HandleMovement()
    {
        // 以“摄像机朝向”为基准的前/右
        Vector3 camF = cameraTransform.forward; camF.y = 0; camF.Normalize();
        Vector3 camR = cameraTransform.right;   camR.y = 0; camR.Normalize();

        Vector3 desiredDir = (camF * input.y + camR * input.x).normalized;
        Vector3 vel = rb.velocity;

        // 只处理水平速度分量（x,z），竖直交给物理
        Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);
        Vector3 desiredVel = desiredDir * moveSpeed;

        // 选择合适的加速度（地面/空中）
        float accel = grounded ? (desiredVel.sqrMagnitude > 0.01f ? acceleration : deceleration)
                               : airAcceleration;

        // 速度朝目标逼近
        Vector3 velChange = Vector3.ClampMagnitude(desiredVel - horizVel, accel * Time.fixedDeltaTime);
        rb.velocity = new Vector3(horizVel.x + velChange.x, vel.y, horizVel.z + velChange.z);
    }

    void HandleFacing()
    {
        Vector3 vel = rb.velocity; vel.y = 0;
        if (vel.sqrMagnitude > 0.02f)
        {
            Quaternion targetRot = Quaternion.LookRotation(vel.normalized);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 12f * Time.fixedDeltaTime));
        }
    }

    void HandleJumpAndBuffer()
    {
        // 是否满足“可跳跃”：在 coyoteTime 内仍算落地
        bool canCoyote = (Time.time - lastGroundedTime) <= coyoteTime;
        bool buffered  = (Time.time - lastJumpPressedTime) <= jumpBuffer;

        if (buffered && canCoyote)
        {
            lastJumpPressedTime = -999f;  // 消耗缓存
            JumpNow();
        }
    }

    void JumpNow()
    {
        // 通过期望高度计算需要的初速度：v = sqrt(2*g*h)
        float jumpVel = Mathf.Sqrt(2f * Physics.gravity.magnitude * jumpHeight);

        Vector3 v = rb.velocity;
        if (v.y < 0f) v.y = 0f;       // 下落中跳、先清掉向下速度（更跟手）
        v.y = jumpVel;
        rb.velocity = v;              // 直接设竖直速度，保留水平速度

        anim.SetTrigger("Jump");
    }

    void HandleSlide()
    {
        if (!wantSlide || !grounded) return;
        wantSlide = false;
        StartCoroutine(SlideCoroutine());
        anim.SetTrigger("Slide");
    }

    IEnumerator SlideCoroutine()
    {
        float baseSpeed = moveSpeed;
        moveSpeed *= slideBoost;
        yield return new WaitForSeconds(0.5f);
        moveSpeed = baseSpeed;
    }
}
