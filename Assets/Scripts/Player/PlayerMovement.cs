using UnityEngine;
using System.Collections;


public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement instance;
    public float speed = 0;

    Vector3 movement;
    Animator anim;
    Rigidbody playerRigidbody;
    int floorMask;
    float camRayLength = 100f;

    public Vector3 GetMousePoint { get; set; }
    void Awake()
    {
        floorMask = LayerMask.GetMask("Floor");
        anim = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();

        instance = this;
    }

    void Update()
    {
        // 检测空格键——跳跃
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }

        // 检测左Shift键——滑铲
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Slide();
        }
    }

    void FixedUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        //displacement
        Move(h, v);
        //make player to the mouse
        Turning();
        //Play the animation based on whether you are walking or not
        Animating(h, v);
    }

    void Move(float h,float v)
    {
        movement.Set(h, 0, v);
        movement = movement.normalized * speed * Time.deltaTime;
        playerRigidbody.MovePosition(transform.position + movement);
    }

    // Keep the player's orientation pointing to the point where the mouse hits on the ground
    // y is height
    void Turning()
    {
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit floorHit;
        if(Physics.Raycast(camRay,out floorHit, camRayLength, floorMask))
        {
            Vector3 playerToMouse = floorHit.point - transform.position;
            GetMousePoint = playerToMouse;
            playerToMouse.y = 0f;
            Quaternion newRotation = Quaternion.LookRotation(playerToMouse);
            playerRigidbody.MoveRotation(newRotation);
        }
        Debug.DrawRay(Camera.main.transform.position, floorHit.point, Color.red);
        Debug.DrawLine(Camera.main.transform.position, floorHit.point, Color.red);
    }

    void Animating(float h,float v)
    {
        bool walking = h != 0f || v != 0f;

        // IsWalking(parameter) is connected to the animated state machine
        anim.SetBool("IsWalking", walking);
    }

    void Jump()
    {
        // 给刚体一个向上的力
        if (playerRigidbody.velocity.y == 0)  // 防止多段跳
        {
            playerRigidbody.AddForce(Vector3.up * 300f);
            anim.SetTrigger("Jump");
        }
    }

    void Slide()
    {
        // 触发滑铲动画
        anim.SetTrigger("Slide");

        // 可选：短时间降低碰撞体高度或加速移动
        StartCoroutine(SlideCoroutine());
    }

    IEnumerator SlideCoroutine()
    {
        // 临时提升速度
        float oldSpeed = speed;
        speed *= 2f;
        yield return new WaitForSeconds(0.5f);
        speed = oldSpeed;
    }


}
