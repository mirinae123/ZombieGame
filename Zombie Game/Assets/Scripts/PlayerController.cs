using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Transactions;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.UI.Image;


// 경사로 이동을 부드럽게 처리하기 위해 땅에 닿은 경우와 공중에 있는 경우의 gravityScale을 다르게 함
// 땅에서는 gravityScale을 더 크게 하여 경사로에서 쉽게 벗어나지 않도록 조정


// 플랫폼은 Platform, Contacted Platform 레이어로 구분
//  - Platform: 일반적인 플랫폼
//  - Contacted Platform: 현재 플레이어가 접촉 중인 플랫폼
//
// Platform 레이어에 포함된 플랫폼은 플레이어의 이동 상태에 따라 충돌 여부가 결정
// 플레이어가 떨어지고 있으면서, 경사로를 내려가는 중이 아닐 때만 충돌 활성화
//
// Contacted Platform 레이어에 포함된 플랫폼은 항상 충돌 활성화


public class PlayerController : MonoBehaviour
{
    // 이동 관련 수치
    public float walkSpeed = 7f;
    public float runSpeed = 14f;
    public float crouchSpeed = 3.5f;

    public float jumpForce = 7f;

<<<<<<< Updated upstream
    private BoxCollider2D bc;   //자신의 콜라이더
=======
    // 필요한 컴포넌트 저장
    private Rigidbody2D rb;
    private CapsuleCollider2D cc;
>>>>>>> Stashed changes

    // 접지 관련 변수
    private GameObject ground;
    private GameObject downJumpGround;
    private bool isGrounded = true;

    // 이동 속도 갱신용 변수
    private Vector2 velocity;

<<<<<<< Updated upstream
    public const int hRayNumber = 6;    //HorizontalCheck에서 Raycast 발사 횟수
    public const int vRayNumber = 6;
    private float hRaySpacing, vRaySpacing;
    private const float colPadding = .1f;

    private bool disableMovement = false;

    struct CollisionPoint   
=======
    // raycast 시작점 저장용 변수
    struct CollisionPoints
>>>>>>> Stashed changes
    {
        internal Vector2[] top, bottom; // 위아래에서 발사하는 raycast의 시작점
        internal Vector2[] left, right; // 좌우에서 발사하는 raycast의 시작점
    }
<<<<<<< Updated upstream
    private CollisionPoint colPoint;    //패딩 적용한 콜라이더의 꼭짓점

    void Start()
    {
        bc = GetComponent<BoxCollider2D>(); //콜라이더
=======
    private CollisionPoints colPoints;
    private const int hRayNumber = 8;   // 수평으로 발사할 raycast의 수
    private const int vRayNumber = 8;   // 수직으로 발사할 raycast의 수
    private const float colPadding = 0.01f; // 시작점의 오프셋

    private bool canMove = true;        // 이동 가능 여부

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();
>>>>>>> Stashed changes

        // raycast 시작점 배열 초기화
        colPoints.top = new Vector2[vRayNumber];
        colPoints.bottom = new Vector2[vRayNumber];
        colPoints.left = new Vector2[hRayNumber];
        colPoints.right = new Vector2[hRayNumber];
    }

<<<<<<< Updated upstream
    public void Move(float deltaX, float deltaTime)
    {
        if (disableMovement) return;    //이동가능 여부 검사

        velocity.x = deltaX * deltaTime * walkSpeed;    //x축 가속도
        velocity.y -= gravity * deltaTime;              //중력가속도

        if (velocity.y < -1f) velocity.y = -1f;     //종단속도

        UpdateCollisionPoint();     
        if (velocity.x != 0) HorizontalCheck(); //충돌검사
        if (velocity.y != 0) VerticalCheck();   //충돌검사
        transform.Translate(velocity);      //이동
    }

    void HorizontalCheck()
    {
        float hDir = Mathf.Sign(velocity.x);                //Raycast방향, x축 이동 방향
        float hLen = Mathf.Abs(velocity.x) + colPadding;    //Raycast길이, x축 이동 속력

        Vector2 origin = hDir > 0 ? colPoint.bottomRight : colPoint.bottomLeft; //Raycast 발사 위치

        for (int i = 0; i < hRayNumber; i++)    //높이를 높여가며 Raycast발사
        {
            //다음 프레임에 충돌할지 예측
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * hDir, hLen, LayerMask.GetMask("Solid"));

            if (hit)
            {
                hLen = hit.distance;    //이 코드는 무슨 용도?
                velocity.x = (hit.distance - colPadding) * hDir;    //충돌하지 않게 속도 조정
            }

            origin += Vector2.up * hRaySpacing; //발사 높이 상승
=======
    private void Update()
    {
        // 플레이어의 위치에 따라 주변 플랫폼의 충돌 상태 업데이트
        UpdatePlatforms();

        if (rb.velocity.y >= 0) // 플레이어가 점프한 후 올라가고 있는 경우
        {
            // 플레이어와 일반 플랫폼의 충돌 비활성화
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"));
        }
        else if (!isGrounded)   // 플레이어가 떨어지고 있으면서, 땅에 닿아있지 않은 경우(경사로를 내려가고 있지 않은 경우)
        {
            // 플레이어와 일반 플랫폼의 충돌 활성화
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), false);
>>>>>>> Stashed changes
        }
    }

    // 플레이어의 좌우 이동 적용
    // float deltaX: 플레이어의 axis 입력 값
    public void Move(float deltaX)
    {
<<<<<<< Updated upstream
        float vDir = Mathf.Sign(velocity.y);                //Raycast방향, y축 이동 방향
        float vLen = Mathf.Abs(velocity.y) + colPadding;    //Raycast길이, y축 이동 속력

        Vector2 origin = vDir > 0 ? colPoint.topLeft : colPoint.bottomLeft; //Raycast 발사 위치

        for (int i = 0; i < vRayNumber; i++)    //위치를 바꿔가며 Raycast발사
        {
            //다음 프레임에 충돌할지 예측
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up * vDir, vLen, LayerMask.GetMask("Solid"));

            if (hit)
            {
                vLen = hit.distance;    //이 코드는 무슨 용도?
                velocity.y = (hit.distance - colPadding) * vDir;    //충돌하지 않게 속도 조정
            }

            origin += Vector2.right * vRaySpacing;  //발사 위치 변경
=======
        if (canMove)                                            // 움직일 수 있는 경우
        {
            velocity.Set(deltaX * walkSpeed, rb.velocity.y);    // 속도 저장용 변수를 갱신한 후
            rb.velocity = velocity;                             // rigidBody에 적용
>>>>>>> Stashed changes
        }
    }

    // 점프 적용
    public void Jump()
    {
<<<<<<< Updated upstream
        Vector2 origin = colPoint.bottomLeft;

        //바닥에 닿았는지 검사
        for (int i = 0; i < vRayNumber; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, colPadding * 2f, LayerMask.GetMask("Solid"));

            if (hit)    //바닥에 닿았다면
            {
                velocity.y = jumpForce; //점프
                return;
            }

            origin += Vector2.right * vRaySpacing;
=======
        if (isGrounded && canMove)                                      // 땅에 닿아 있고, 움직일 수 있는 경우
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);   // jumpForce 적용
            rb.gravityScale = 3f;                                       // 공중에 있을 때는 중력 감소
>>>>>>> Stashed changes
        }
    }

    // 아래점프 적용
    public void DownJump()
    {
<<<<<<< Updated upstream
        Bounds bound = bc.bounds;       //콜라이더의 위치, 크기 정보
        bound.Expand(colPadding * -2);  //패딩만큼 범위 축소

        //colPoint에 콜라이더의 위치정보 저장
        colPoint.topLeft = new Vector2(bound.min.x, bound.max.y);
        colPoint.topRight = new Vector2(bound.max.x, bound.max.y);
        colPoint.bottomLeft = new Vector2(bound.min.x, bound.min.y);
        colPoint.bottomRight = new Vector2(bound.max.x, bound.min.y);
    }
=======
        if (isGrounded && canMove && ground.CompareTag("Platform"))         // 땋에 닿아 있고, 움직일 수 있고, 닿아 있는 땅이 플랫폼인 경우
        {
            downJumpGround = ground;                                        // 일시적으로 충돌을 비활성화할 플랫폼을 downJumpGround 변수에 저장
            downJumpGround.GetComponent<BoxCollider2D>().isTrigger = true;  // 충돌 비활성화
            downJumpGround.layer = LayerMask.NameToLayer("Platform");       // 레이어를 일반 플랫폼으로 변경
>>>>>>> Stashed changes

            isGrounded = false;                                             // 접지 여부 비활성화
            rb.gravityScale = 3f;                                           // 공중에 있을 때는 중력 감소
        }
    }
    
    // 물체 뛰어넘기 적용
    // float deltaX: 플레이어의 axis 입력 값
    public void Vault(float deltaX)
    {
<<<<<<< Updated upstream
        if (disableMovement) return;    //이동 가능 여부 검사

        float hDir = Mathf.Sign(deltaX);    //Raycast방향
        float hLen = 1.5f + colPadding;     //Raycast길이

        Vector2 origin = hDir > 0 ? colPoint.bottomRight : colPoint.bottomLeft; //Raycast발사 위치

        //충돌 검사
        for (int i = 0; i < hRayNumber; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * hDir, hLen, LayerMask.GetMask("Solid"));   //Raycast발사
            Debug.DrawRay(origin, Vector2.right * hDir, Color.red, .5f);

            if (hit && hit.collider.CompareTag("Vaultable"))    //Vaultable에 닿았다면
=======
        if (canMove)    // 움직일 수 있는 경우
        {
            float direction = Mathf.Sign(deltaX);   // 방향 저장

            Vector2[] origin = direction > 0 ? colPoints.right : colPoints.left;    // 방향에 따라 raycast 시작점 지정

            for (int i = 0; i < hRayNumber; i++)
>>>>>>> Stashed changes
            {
                // 시작점으로부터, 플레이어가 바라보는 방향으로, 1.5f 만큼 raycast 발사
                RaycastHit2D hit = Physics2D.Raycast(origin[i], Vector2.right * direction, 1.5f + colPadding, LayerMask.GetMask("Solid"));

<<<<<<< Updated upstream
                float endOffset = (bc.size.x + vaultCol.size.x) / 2f;   //자신의 넓이와 물체의 넓이의 평균

                Vector3 mid = vaultObj.transform.position + Vector3.up * vaultCol.size.y / 2f;  //x=물체의 중앙, y=물체의 위끝
                Vector3 end = vaultObj.transform.position + Vector3.right * endOffset * hDir + Vector3.up * .1f;
                Debug.Log("-1");
                StartCoroutine(ExecuteVault(mid, end));
                return;
=======
                if (hit && hit.collider.CompareTag("Vaultable"))                        // hit한 물체가 Vaultable이라면
                {
                    GameObject vaultObj = hit.collider.gameObject;                      // 물체 임시 저장
                    BoxCollider2D vaultCol = vaultObj.GetComponent<BoxCollider2D>();    // boxCollider 임시 저장

                    float endOffset = (cc.bounds.size.x + vaultCol.bounds.size.x);      // 플레이어가 최종적으로 이동해야 할 거리 계산

                    Vector3 mid = vaultObj.transform.position + Vector3.up * vaultCol.size.y;                       // 중간 위치 계산
                    Vector3 end = transform.position + Vector3.right * endOffset * direction + Vector3.up * .1f;    // 최종 위치 계산
                    StartCoroutine(ExecuteVault(mid, end));                             // 물체 뛰어넘기 코루틴 호출
                    return;
                }
>>>>>>> Stashed changes
            }
        }
    }

    // 물체 뛰어넘기 관련 코루틴
    // Vector2 mid: 중간에 거치는 위치
    // Vector2 end: 최종 도착 위치
    IEnumerator ExecuteVault(Vector2 mid, Vector2 end)
    {
<<<<<<< Updated upstream
        disableMovement = true; //움직일 수 없음

        velocity = Vector2.zero;
        Debug.Log("0");
=======
        canMove = false;
        rb.simulated = false;

        rb.velocity = Vector3.zero;

>>>>>>> Stashed changes
        yield return new WaitForSeconds(.5f);
        Debug.Log("1");
        transform.position = mid;
        yield return new WaitForSeconds(.5f);
        Debug.Log("2");
        transform.position = end;

        canMove = true;
        rb.simulated = true;
    }

    // 플레이어를 기점으로 위에 있는 플랫폼은 충돌 비활성화
    // 아래에 있는 플랫폼은 충돌 활성화
    private void UpdatePlatforms()
    {
        UpdateCollisionPoints();    // raycast 시작점 갱신

        for (int i = 0; i < vRayNumber; i++)
        {
            // 플레이어의 아래쪽으로부터, 위 방향으로, 캡슐 콜라이더의 2배 거리만큼 raycast 발사
            RaycastHit2D topHit = Physics2D.Raycast(colPoints.bottom[i], Vector2.up, cc.bounds.size.y * 2, LayerMask.GetMask("Platform"));
            // 플레이어의 아래쪽으로부터, 아래 방향으로, 1f 거리만큼 raycast 발사
            RaycastHit2D bottomHit = Physics2D.Raycast(colPoints.bottom[i], Vector2.down, 1f, LayerMask.GetMask("Platform"));

            // 위에 있는 플랫폼은 충돌 비활성화
            if (topHit) topHit.collider.gameObject.GetComponent<BoxCollider2D>().isTrigger = true;
            // 아래에 있으면서, 일시적으로 충돌이 비활성화된 플랫폼(downJumpGround)가 아니라면 충돌 활성화
            if (bottomHit && bottomHit.collider.gameObject != downJumpGround) bottomHit.collider.gameObject.GetComponent<BoxCollider2D>().isTrigger = false;
        }
    }

    // raycast 시작점 갱신
    private void UpdateCollisionPoints()
    {
        Bounds bound = cc.bounds;                               // 캡슐 콜라이더의 범위
        float hRaySpacing = bound.size.y / (hRayNumber - 1);    // 수평으로 발사할 raycast의 간격
        float vRaySpacing = bound.size.x / (vRayNumber - 1);    // 수직으로 발사할 raycast의 간격

        // 수평 방향 raycast의 시작점 갱신
        for (int i = 0; i < hRayNumber; i++)
        {
            float x, y;

            y = bound.min.y + hRaySpacing * i;  // y 좌표 계산

            // 캡슐 콜라이더의 형태에 맞게 x 좌표 계산
            if (y < (bound.min.y + bound.size.x / 2f))
            {
                x = Mathf.Sqrt(Mathf.Pow(bound.size.x / 2f, 2) - Mathf.Pow(bound.min.y + bound.size.x / 2f - y, 2)) - colPadding;
            }
            else if (y > (bound.max.y - +bound.size.x / 2f))
            {
                x = Mathf.Sqrt(Mathf.Pow(bound.size.x / 2f, 2) - Mathf.Pow(y - bound.max.y + bound.size.x / 2f, 2)) - colPadding;
            }
            else
            {
                x = bound.size.x / 2f - colPadding;
            }

            // 시작점 저장
            colPoints.left[i].Set(bound.center.x - x, y);
            colPoints.right[i].Set(bound.center.x + x, y);
        }

        // 수직 방향 raycast의 시작점 갱신
        for (int i = 0; i < vRayNumber; i++)
        {
            float x, y;

            // x, y 좌표 계산
            x = bound.min.x + vRaySpacing * i;
            y = Mathf.Sqrt(Mathf.Pow(bound.size.x / 2f, 2) - Mathf.Pow(Mathf.Abs(x - bound.center.x), 2)) - colPadding;

            // 시작점 저장
            colPoints.top[i].Set(x, bound.max.y - (bound.size.x / 2f) + y);
            colPoints.bottom[i].Set(x, bound.min.y + (bound.size.x / 2f) - y);
        }

    }

    // 충돌 시작 시 호출
    // 착지 여부 판단, 아래 점프로 인해 비활성된 플랫폼 갱신
    private void OnCollisionEnter2D(Collision2D collision)
    {
        List<ContactPoint2D> contactPoints = new List<ContactPoint2D>();    // 접촉 지점 저장용 변수
        collision.GetContacts(contactPoints);                               // 접촉 지점 저장
        Vector2 point = contactPoints[0].point;                             // 접촉 지점 위치

        // 플레이어가 착지 여부 판정
        // 접촉 지점의 x 좌표가 플레이어 안쪽에 있고, 접촉 지점의 y 좌표가 플레이어 아래에 있는 경우 성공
        if (point.x >= cc.bounds.min.x && point.x <= cc.bounds.max.x && point.y <= transform.position.y)
        {
            isGrounded = true;                      // 접지 여부 갱신
            ground = collision.collider.gameObject; // 접지면 저장

            // 접지 플랫폼은 별도의 레이어로 관리
            if (ground.CompareTag("Platform")) ground.layer = LayerMask.NameToLayer("Contacted Platform");

            // 아래점프로 인해 충돌이 비활성화된 플랫폼이 있다면
            if (downJumpGround)
            {
                downJumpGround.GetComponent<BoxCollider2D>().isTrigger = false; // 충돌 활성화 후
                downJumpGround = null;                                          // 변수 초기화
            }

            rb.gravityScale = 5f;   // 공중에 있을 때는 중력 감소
        }
    }

    // 충돌 중지 시 호출
    // 접지 여부 갱신
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.gameObject == ground)                    // 벗어난 물체가 기존 접지면이었던 경우
        {
            isGrounded = false;                                         // 접지 여부 갱신

            // 접지 플랫폼을 일반 플랫폼으로 변경
            if (ground.layer == LayerMask.NameToLayer("Contacted Platform")) ground.layer = LayerMask.NameToLayer("Platform");
            ground = null;                                  

            rb.gravityScale = 3f;   // 공중에 있을 때는 중력 감소
        }
    }
}