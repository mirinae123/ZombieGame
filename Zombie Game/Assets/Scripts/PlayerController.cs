using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Transactions;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.UI.Image;

public class PlayerController : MonoBehaviour
{
    // 이동 관련 수치
    public float walkSpeed = 7f;
    public float runSpeed = 14f;
    public float crouchSpeed = 3.5f;

    public float jumpForce = 7f;

    // 필요한 컴포넌트 저장
    private Rigidbody2D rb;
    private CapsuleCollider2D cc;

    // 접지 관련 변수
    private Transform ignoredPlatform;
    private Transform detectedPlatform;
    private bool isGrounded = true;
    private bool canDownJump = true;
    private bool canAttachLadder = true;
    int layerMaskCombined = (1 << 7) | (1 << 8);

    // 이동 속도 갱신용 변수
    private Vector2 velocity;

    // raycast 시작점 저장용 변수
    struct CollisionPoints
    {
        internal Vector2[] top, bottom; // 위아래에서 발사하는 raycast의 시작점
        internal Vector2[] left, right; // 좌우에서 발사하는 raycast의 시작점
    }
    private CollisionPoints colPoints;
    private const int hRayNumber = 8;   // 수평으로 발사할 raycast의 수
    private const int vRayNumber = 8;   // 수직으로 발사할 raycast의 수
    private const float colPadding = 0.01f; // 시작점의 오프셋

    private bool canMove = true;        // 이동 가능 여부

    // 사다리, 벽타기 관련 변수
    private GameObject ladder;
    private GameObject climbable;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();

        // raycast 시작점 배열 초기화
        colPoints.top = new Vector2[vRayNumber];
        colPoints.bottom = new Vector2[vRayNumber];
        colPoints.left = new Vector2[hRayNumber];
        colPoints.right = new Vector2[hRayNumber];
    }

    private void Update()
    {
        // 아래 있는 플랫폼 감지
        detectedPlatform = Physics2D.Raycast(transform.position - Vector3.up * cc.bounds.size.y / 2f, Vector2.down, 0.2f, layerMaskCombined).transform;

        if (detectedPlatform != null)   // 아래에 플랫폼이 있는 경우
        {
            isGrounded = true;

            // 땅에 닿은 경우 강한 중력 적용
            if (rb.velocity.y < 0 && !(ladder || climbable)) rb.gravityScale = 5f;

            if (detectedPlatform != ignoredPlatform && !(ladder || climbable))
            {
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), false);
                ignoredPlatform = null;
            }
        }
        else
        {
            isGrounded = false;

            // 땅에 닿지 안흔 경우 약한 중력 적용
            if (!(ladder || climbable)) rb.gravityScale = 3f;
        }
    }

    // 플레이어의 좌우 이동 적용
    // float deltaX: 플레이어의 axis 입력 값
    public void Move(float deltaX, float deltaY)
    {
        // 경사로에서 플레이어가 미끄러지지 않도록 x 좌표 고정
        if (deltaX == 0 && isGrounded)  // 좌우 입력 값이 없고, 지면에 접하고 있는 경우
        {
            // x 좌표와 z 회전 고정
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }
        else                            // 그 외의 경우
        {   
            // z 회전만 고정
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (canMove)                                                // 움직일 수 있는 경우
        {
            if (!(ladder || climbable)) // 사다리 또는 벽을 타는 중이 아닌 경우
            {
                velocity.Set(deltaX * walkSpeed, rb.velocity.y);    // 속도 저장용 변수를 갱신한 후
                rb.velocity = velocity;                             // rigidBody에 적용

                if (deltaY < 0)                 // 아래 키를 누르고 있는 경우
                {
                    AttachToLadderBelow();      // 아래에 있는 사다리에 올라탐
                    if (!ladder) DownJump();    // 올라탈 사다리가 없는 경우 아래 점프 시도
                }
                if (deltaY > 0) AttachToLadder(deltaX);
            }
            else                        // 사다리 또는 벽을 타고 있는 경우
            {
                velocity.Set(0, deltaY * walkSpeed);                // 속도 저장용 변수를 갱신한 후
                rb.velocity = velocity;                             // rigidBody에 적용

                if (ladder) MoveInsideLadder();                     // 사다리 내에서 이동
                if (climbable) { }
            }
        }
    }

    // 점프 적용
    public void Jump()
    {
        if (canMove && isGrounded && !(ladder || climbable))
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    // 아래점프 적용
    public void DownJump()
    {
        if (canMove && isGrounded && canDownJump && !(ladder || climbable))
        {
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), true);
            ignoredPlatform = detectedPlatform;
            StartCoroutine(DownJumpCooldown());
        }
    }

    // 아래점프 쿨타임 적용
    IEnumerator DownJumpCooldown()
    {
        canDownJump = false;
        yield return new WaitForSeconds(.5f);
        canDownJump = true;
    }

    // 물체 뛰어넘기 적용
    // float deltaX: 플레이어의 axis 입력 값
    public void Vault(float deltaX)
    {
        if (canMove)    // 움직일 수 있는 경우
        {
            float direction = Mathf.Sign(deltaX);   // 방향 저장

            Vector2[] origin = direction > 0 ? colPoints.right : colPoints.left;    // 방향에 따라 raycast 시작점 지정

            for (int i = 0; i < hRayNumber; i++)
            {
                UpdateCollisionPoints();

                // 시작점으로부터, 플레이어가 바라보는 방향으로, 1.5f 만큼 raycast 발사
                RaycastHit2D hit = Physics2D.Raycast(origin[i], Vector2.right * direction, 1.5f + colPadding, LayerMask.GetMask("Solid"));

                if (hit && hit.collider.CompareTag("Vaultable"))                        // hit한 물체가 Vaultable이라면
                {
                    GameObject vaultObj = hit.collider.gameObject;                      // 물체 임시 저장
                    BoxCollider2D vaultCol = vaultObj.GetComponent<BoxCollider2D>();    // boxCollider 임시 저장

                    // 위치 계산
                    Vector3 mid = vaultObj.transform.position + Vector3.up * (vaultCol.size.y + cc.bounds.size.y) / 2f;
                    Vector3 end = new Vector3(vaultObj.transform.position.x + direction * (vaultCol.bounds.size.x + cc.bounds.size.x) / 2f, vaultObj.transform.position.y + (cc.bounds.size.y - vaultCol.bounds.size.y) / 2f, 0);

                    StartCoroutine(ExecuteVault(mid, end));                             // 물체 뛰어넘기 코루틴 호출
                    return;
                }
            }
        }
    }

    // 사다리에 올라타기
    public void AttachToLadder(float deltaX = 0, bool fromAbove = false)
    {
        if (canMove && canAttachLadder && !(ladder || climbable))  // 움직일 수 있고, 기존에 사다리 또는 벽을 타고 있는 경우가 아닌 경우
        {
            float direction = Mathf.Sign(deltaX);                                   // 플레이어가 바라보고 있는 방향

            UpdateCollisionPoints();                                                // raycast 시작점 갱신
            Vector2[] origin = direction > 0 ? colPoints.left : colPoints.right;    // 플레이어 방향에 따라 시작점 설정

            for (int i = 0; i < hRayNumber; i++)
            {
                // raycast 시작점으로부터, 플레이어가 바라보는 방향으로 raycast 발사
                RaycastHit2D hit = Physics2D.Raycast(origin[i], Vector2.right * direction * (cc.bounds.size.x + .5f), cc.bounds.size.x / 2f);

                if (hit && hit.collider.CompareTag("Ladder") && hit.distance < 1f && ((!fromAbove && transform.position.y < hit.collider.bounds.max.y) || fromAbove))   // 사다리를 hit한 경우
                {
                    // 접지면, 접지 여부 등 변수를 초기화
                    detectedPlatform = null;
                    ignoredPlatform = null;
                    isGrounded = false;

                    Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), true);

                    rb.gravityScale = 0f;               // 중력 비활성화
                    rb.velocity = Vector2.zero;         // 플레이어 속도 초기화
                    ladder = hit.collider.gameObject;   // 현재 타고 있는 사다리를 ladder 변수에 저장

                    // 플레이어의 좌표를 사다리 위치로 이동
                    Bounds ladderBounds = ladder.GetComponent<BoxCollider2D>().bounds;
                    float y = Mathf.Clamp(transform.position.y, ladderBounds.min.y + cc.bounds.size.y / 2f + .1f, float.MaxValue);
                    transform.position = new Vector2(ladderBounds.center.x, y);

                    return;
                }
            }
        }
    }

    // 아래 키를 누르면 아래 사다리에 올라타기
    private void AttachToLadderBelow()
    {
        if (canMove && !(ladder || climbable))      // 움직일 수 있고, 기존에 사다리 또는 벽을 타고 있는 경우가 아닌 경우
        {
            UpdateCollisionPoints();                // raycast 시작점 갱신
            Vector2[] origin = colPoints.bottom;    // 시작점은 플레이어의 아래쪽

            for (int i = 0; i < vRayNumber; i++)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(origin[i], Vector2.down, 1f);

                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider.CompareTag("Ladder"))
                    {
                        AttachToLadder(0, true);
                        return;
                    }
                }
            }
        }
    }

    // 사다리 안에서 이동
    private void MoveInsideLadder()
    {
        Bounds bound = ladder.GetComponent<BoxCollider2D>().bounds;

        // 플레이어가 사다리 맨위, 만아래에 도달한 경우 벗어나기
        if ((cc.bounds.min.y >= bound.max.y) || (cc.bounds.min.y <= bound.min.y))
        {
            DetachFromLadder();
            StartCoroutine(LadderCooldown());
        }
    }

    // 사다리 타기 쿨타임 적용
    IEnumerator LadderCooldown()
    {
        canAttachLadder = false;
        yield return new WaitForSeconds(.2f);
        canAttachLadder = true;
    }

    // 사다리에서 벗어나기 
    // bool isJumping: 점프로 사다리에서 벗어나는지 여부
    public void DetachFromLadder(bool isJumping = false)
    {
        if (canMove && ladder)  // 움직일 수 있고, 현재 사다리를 타고 있다면
        {
            rb.gravityScale = 90f;               // 중력 다시 적용 후
            ladder = null;                      // ladder 변수 초기화

            // 점프로 사다리에서 벗어나는 경우
            if (isJumping) rb.AddForce(Vector2.up * jumpForce / 2f, ForceMode2D.Impulse);
        }
    }

    // 물체 뛰어넘기 관련 코루틴
    // Vector2 mid: 중간에 거치는 위치
    // Vector2 end: 최종 도착 위치
    IEnumerator ExecuteVault(Vector2 mid, Vector2 end)
    {
        canMove = false;
        rb.simulated = false;

        rb.velocity = Vector3.zero;

        yield return new WaitForSeconds(.5f);
        transform.position = mid;
        yield return new WaitForSeconds(.5f);
        transform.position = end;

        canMove = true;
        rb.simulated = true;
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
                x = Mathf.Sqrt(Mathf.Abs(Mathf.Pow(bound.size.x / 2f, 2) - Mathf.Pow(bound.min.y + bound.size.x / 2f - y, 2))) - colPadding;
            }
            else if (y > (bound.max.y - +bound.size.x / 2f))
            {
                x = Mathf.Sqrt(Mathf.Abs(Mathf.Pow(bound.size.x / 2f, 2) - Mathf.Pow(y - bound.max.y + bound.size.x / 2f, 2))) - colPadding;
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
            y = Mathf.Sqrt(Mathf.Abs(Mathf.Pow(bound.size.x / 2f, 2) - Mathf.Pow(Mathf.Abs(x - bound.center.x), 2))) - colPadding;

            // 시작점 저장
            colPoints.top[i].Set(x, bound.max.y - (bound.size.x / 2f) + y);
            colPoints.bottom[i].Set(x, bound.min.y + (bound.size.x / 2f) - y);
        }
    }
}