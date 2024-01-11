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

    private bool canMove = true;        // 이동 가능 여부

    // 사다리, 벽타기 관련 변수
    private GameObject ladder;
    private GameObject climbable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();
    }

    private void Update()
    {
        // 아래 있는 플랫폼 감지
        detectedPlatform = CustomBoxCast(CustomBoxCastDirection.Down, 0.2f, layerMaskCombined).transform;

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
            float direction = Mathf.Sign(deltaX);
            // 시작점으로부터, 플레이어가 바라보는 방향으로, 1.5f 만큼 raycast 발사
            RaycastHit2D hit = CustomBoxCast(direction, 1.5f, LayerMask.GetMask("Solid"));

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

    // 사다리에 올라타기
    public void AttachToLadder(float deltaX = 0, bool fromAbove = false)
    {
        if (canMove && canAttachLadder && !(ladder || climbable))  // 움직일 수 있고, 기존에 사다리 또는 벽을 타고 있는 경우가 아닌 경우
        {
            // raycast 시작점으로부터, 플레이어가 바라보는 방향으로 raycast 발사
            RaycastHit2D hit = CustomBoxCast(deltaX, cc.bounds.extents.x, ~LayerMask.GetMask("Player"));

            if (hit && hit.collider.CompareTag("Ladder") && hit.distance < 1f && (transform.position.y < hit.collider.bounds.max.y || fromAbove))   // 사다리를 hit한 경우
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

    // 아래 키를 누르면 아래 사다리에 올라타기
    private void AttachToLadderBelow()
    {
        if (canMove && !(ladder || climbable))      // 움직일 수 있고, 기존에 사다리 또는 벽을 타고 있는 경우가 아닌 경우
        {
            Vector2 boxSize = new Vector2(cc.size.x, 0.01f);
            RaycastHit2D[] hits = Physics2D.BoxCastAll(transform.position + Vector3.down * cc.bounds.extents.y, boxSize, 0, Vector2.down, 1);

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


    #region CustomBoxCast
    enum CustomBoxCastDirection { Left, Right, Up, Down };  // CustomBoxCast 방향을 정하기 위한 매개변수
    RaycastHit2D CustomBoxCast(CustomBoxCastDirection direction, float distance, int layerMask = -1)
    {
        // -1을 이진수로 나타내면 모든 비트가 1

        // Physics2D.Baxcast를 호출하는데 필요한 요소들
        Vector2 boxSize = new Vector2(1, 1);    // 박스 크기
        Vector2 origin = transform.position;    // 시작점
        Vector2 _direction = new Vector2();     // 방향

        switch (direction)  // 요소 할당
        {
            case CustomBoxCastDirection.Left:
                origin += Vector2.left * cc.bounds.extents.x;   // x=좌측끝, y=중앙
                boxSize.Set(0.01f, cc.bounds.size.y);
                _direction = Vector2.left;
                break;
            case CustomBoxCastDirection.Right:
                origin += Vector2.right * cc.bounds.extents.x;  // x=우측끝, y=중앙
                boxSize.Set(0.01f, cc.bounds.size.y);
                _direction = Vector2.right;
                break;
            case CustomBoxCastDirection.Up:
                origin += Vector2.up * cc.bounds.extents.y;     // x=중앙, y=위끝
                boxSize.Set(cc.bounds.size.x, 0.01f);
                _direction = Vector2.up;
                break;
            case CustomBoxCastDirection.Down:
                origin += Vector2.down * cc.bounds.extents.y;   // x=중앙, y=아래끝
                boxSize.Set(cc.bounds.size.x, 0.01f);
                _direction = Vector2.down;
                break;
        }

        //  Physics2D.Baxcast 호출
        return Physics2D.BoxCast(origin, boxSize, 0, _direction, distance, layerMask);
    }

    RaycastHit2D CustomBoxCast(float deltaX, float distance, int layerMask = -1)
    {
        // -1을 이진수로 나타내면 모든 비트가 1

        if      (deltaX > 0)    return CustomBoxCast(CustomBoxCastDirection.Right, distance, layerMask);
        else if (deltaX < 0)    return CustomBoxCast(CustomBoxCastDirection.Left , distance, layerMask);
        else                    return CustomBoxCast(CustomBoxCastDirection.Down , distance, layerMask);
    }

    #endregion //CustomBoxCast
}