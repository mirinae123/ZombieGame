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
    // �̵� ���� ��ġ
    public float walkSpeed = 7f;
    public float runSpeed = 14f;
    public float crouchSpeed = 3.5f;

    public float jumpForce = 7f;

    // �ʿ��� ������Ʈ ����
    private Rigidbody2D rb;
    private CapsuleCollider2D cc;

    // ���� ���� ����
    private Transform ignoredPlatform;
    private Transform detectedPlatform;
    private bool isGrounded = true;
    private bool canDownJump = true;
    private bool canAttachLadder = true;
    int layerMaskCombined = (1 << 7) | (1 << 8);

    // �̵� �ӵ� ���ſ� ����
    private Vector2 velocity;

    // raycast ������ ����� ����
    struct CollisionPoints
    {
        internal Vector2[] top, bottom; // ���Ʒ����� �߻��ϴ� raycast�� ������
        internal Vector2[] left, right; // �¿쿡�� �߻��ϴ� raycast�� ������
    }
    private CollisionPoints colPoints;
    private const int hRayNumber = 8;   // �������� �߻��� raycast�� ��
    private const int vRayNumber = 8;   // �������� �߻��� raycast�� ��
    private const float colPadding = 0.01f; // �������� ������

    private bool canMove = true;        // �̵� ���� ����

    // ��ٸ�, ��Ÿ�� ���� ����
    private GameObject ladder;
    private GameObject climbable;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();

        // raycast ������ �迭 �ʱ�ȭ
        colPoints.top = new Vector2[vRayNumber];
        colPoints.bottom = new Vector2[vRayNumber];
        colPoints.left = new Vector2[hRayNumber];
        colPoints.right = new Vector2[hRayNumber];
    }

    private void Update()
    {
        // �Ʒ� �ִ� �÷��� ����
        detectedPlatform = Physics2D.Raycast(transform.position - Vector3.up * cc.bounds.size.y / 2f, Vector2.down, 0.2f, layerMaskCombined).transform;

        if (detectedPlatform != null)   // �Ʒ��� �÷����� �ִ� ���
        {
            isGrounded = true;

            // ���� ���� ��� ���� �߷� ����
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

            // ���� ���� ���� ��� ���� �߷� ����
            if (!(ladder || climbable)) rb.gravityScale = 3f;
        }
    }

    // �÷��̾��� �¿� �̵� ����
    // float deltaX: �÷��̾��� axis �Է� ��
    public void Move(float deltaX, float deltaY)
    {
        // ���ο��� �÷��̾ �̲������� �ʵ��� x ��ǥ ����
        if (deltaX == 0 && isGrounded)  // �¿� �Է� ���� ����, ���鿡 ���ϰ� �ִ� ���
        {
            // x ��ǥ�� z ȸ�� ����
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }
        else                            // �� ���� ���
        {   
            // z ȸ���� ����
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (canMove)                                                // ������ �� �ִ� ���
        {
            if (!(ladder || climbable)) // ��ٸ� �Ǵ� ���� Ÿ�� ���� �ƴ� ���
            {
                velocity.Set(deltaX * walkSpeed, rb.velocity.y);    // �ӵ� ����� ������ ������ ��
                rb.velocity = velocity;                             // rigidBody�� ����

                if (deltaY < 0)                 // �Ʒ� Ű�� ������ �ִ� ���
                {
                    AttachToLadderBelow();      // �Ʒ��� �ִ� ��ٸ��� �ö�Ž
                    if (!ladder) DownJump();    // �ö�Ż ��ٸ��� ���� ��� �Ʒ� ���� �õ�
                }
                if (deltaY > 0) AttachToLadder(deltaX);
            }
            else                        // ��ٸ� �Ǵ� ���� Ÿ�� �ִ� ���
            {
                velocity.Set(0, deltaY * walkSpeed);                // �ӵ� ����� ������ ������ ��
                rb.velocity = velocity;                             // rigidBody�� ����

                if (ladder) MoveInsideLadder();                     // ��ٸ� ������ �̵�
                if (climbable) { }
            }
        }
    }

    // ���� ����
    public void Jump()
    {
        if (canMove && isGrounded && !(ladder || climbable))
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    // �Ʒ����� ����
    public void DownJump()
    {
        if (canMove && isGrounded && canDownJump && !(ladder || climbable))
        {
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), true);
            ignoredPlatform = detectedPlatform;
            StartCoroutine(DownJumpCooldown());
        }
    }

    // �Ʒ����� ��Ÿ�� ����
    IEnumerator DownJumpCooldown()
    {
        canDownJump = false;
        yield return new WaitForSeconds(.5f);
        canDownJump = true;
    }

    // ��ü �پ�ѱ� ����
    // float deltaX: �÷��̾��� axis �Է� ��
    public void Vault(float deltaX)
    {
        if (canMove)    // ������ �� �ִ� ���
        {
            float direction = Mathf.Sign(deltaX);   // ���� ����

            Vector2[] origin = direction > 0 ? colPoints.right : colPoints.left;    // ���⿡ ���� raycast ������ ����

            for (int i = 0; i < hRayNumber; i++)
            {
                UpdateCollisionPoints();

                // ���������κ���, �÷��̾ �ٶ󺸴� ��������, 1.5f ��ŭ raycast �߻�
                RaycastHit2D hit = Physics2D.Raycast(origin[i], Vector2.right * direction, 1.5f + colPadding, LayerMask.GetMask("Solid"));

                if (hit && hit.collider.CompareTag("Vaultable"))                        // hit�� ��ü�� Vaultable�̶��
                {
                    GameObject vaultObj = hit.collider.gameObject;                      // ��ü �ӽ� ����
                    BoxCollider2D vaultCol = vaultObj.GetComponent<BoxCollider2D>();    // boxCollider �ӽ� ����

                    // ��ġ ���
                    Vector3 mid = vaultObj.transform.position + Vector3.up * (vaultCol.size.y + cc.bounds.size.y) / 2f;
                    Vector3 end = new Vector3(vaultObj.transform.position.x + direction * (vaultCol.bounds.size.x + cc.bounds.size.x) / 2f, vaultObj.transform.position.y + (cc.bounds.size.y - vaultCol.bounds.size.y) / 2f, 0);

                    StartCoroutine(ExecuteVault(mid, end));                             // ��ü �پ�ѱ� �ڷ�ƾ ȣ��
                    return;
                }
            }
        }
    }

    // ��ٸ��� �ö�Ÿ��
    public void AttachToLadder(float deltaX = 0, bool fromAbove = false)
    {
        if (canMove && canAttachLadder && !(ladder || climbable))  // ������ �� �ְ�, ������ ��ٸ� �Ǵ� ���� Ÿ�� �ִ� ��찡 �ƴ� ���
        {
            float direction = Mathf.Sign(deltaX);                                   // �÷��̾ �ٶ󺸰� �ִ� ����

            UpdateCollisionPoints();                                                // raycast ������ ����
            Vector2[] origin = direction > 0 ? colPoints.left : colPoints.right;    // �÷��̾� ���⿡ ���� ������ ����

            for (int i = 0; i < hRayNumber; i++)
            {
                // raycast ���������κ���, �÷��̾ �ٶ󺸴� �������� raycast �߻�
                RaycastHit2D hit = Physics2D.Raycast(origin[i], Vector2.right * direction * (cc.bounds.size.x + .5f), cc.bounds.size.x / 2f);

                if (hit && hit.collider.CompareTag("Ladder") && hit.distance < 1f && ((!fromAbove && transform.position.y < hit.collider.bounds.max.y) || fromAbove))   // ��ٸ��� hit�� ���
                {
                    // ������, ���� ���� �� ������ �ʱ�ȭ
                    detectedPlatform = null;
                    ignoredPlatform = null;
                    isGrounded = false;

                    Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), true);

                    rb.gravityScale = 0f;               // �߷� ��Ȱ��ȭ
                    rb.velocity = Vector2.zero;         // �÷��̾� �ӵ� �ʱ�ȭ
                    ladder = hit.collider.gameObject;   // ���� Ÿ�� �ִ� ��ٸ��� ladder ������ ����

                    // �÷��̾��� ��ǥ�� ��ٸ� ��ġ�� �̵�
                    Bounds ladderBounds = ladder.GetComponent<BoxCollider2D>().bounds;
                    float y = Mathf.Clamp(transform.position.y, ladderBounds.min.y + cc.bounds.size.y / 2f + .1f, float.MaxValue);
                    transform.position = new Vector2(ladderBounds.center.x, y);

                    return;
                }
            }
        }
    }

    // �Ʒ� Ű�� ������ �Ʒ� ��ٸ��� �ö�Ÿ��
    private void AttachToLadderBelow()
    {
        if (canMove && !(ladder || climbable))      // ������ �� �ְ�, ������ ��ٸ� �Ǵ� ���� Ÿ�� �ִ� ��찡 �ƴ� ���
        {
            UpdateCollisionPoints();                // raycast ������ ����
            Vector2[] origin = colPoints.bottom;    // �������� �÷��̾��� �Ʒ���

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

    // ��ٸ� �ȿ��� �̵�
    private void MoveInsideLadder()
    {
        Bounds bound = ladder.GetComponent<BoxCollider2D>().bounds;

        // �÷��̾ ��ٸ� ����, ���Ʒ��� ������ ��� �����
        if ((cc.bounds.min.y >= bound.max.y) || (cc.bounds.min.y <= bound.min.y))
        {
            DetachFromLadder();
            StartCoroutine(LadderCooldown());
        }
    }

    // ��ٸ� Ÿ�� ��Ÿ�� ����
    IEnumerator LadderCooldown()
    {
        canAttachLadder = false;
        yield return new WaitForSeconds(.2f);
        canAttachLadder = true;
    }

    // ��ٸ����� ����� 
    // bool isJumping: ������ ��ٸ����� ������� ����
    public void DetachFromLadder(bool isJumping = false)
    {
        if (canMove && ladder)  // ������ �� �ְ�, ���� ��ٸ��� Ÿ�� �ִٸ�
        {
            rb.gravityScale = 90f;               // �߷� �ٽ� ���� ��
            ladder = null;                      // ladder ���� �ʱ�ȭ

            // ������ ��ٸ����� ����� ���
            if (isJumping) rb.AddForce(Vector2.up * jumpForce / 2f, ForceMode2D.Impulse);
        }
    }

    // ��ü �پ�ѱ� ���� �ڷ�ƾ
    // Vector2 mid: �߰��� ��ġ�� ��ġ
    // Vector2 end: ���� ���� ��ġ
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

    // raycast ������ ����
    private void UpdateCollisionPoints()
    {
        Bounds bound = cc.bounds;                               // ĸ�� �ݶ��̴��� ����
        float hRaySpacing = bound.size.y / (hRayNumber - 1);    // �������� �߻��� raycast�� ����
        float vRaySpacing = bound.size.x / (vRayNumber - 1);    // �������� �߻��� raycast�� ����

        // ���� ���� raycast�� ������ ����
        for (int i = 0; i < hRayNumber; i++)
        {
            float x, y;

            y = bound.min.y + hRaySpacing * i;  // y ��ǥ ���

            // ĸ�� �ݶ��̴��� ���¿� �°� x ��ǥ ���
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

            // ������ ����
            colPoints.left[i].Set(bound.center.x - x, y);
            colPoints.right[i].Set(bound.center.x + x, y);
        }

        // ���� ���� raycast�� ������ ����
        for (int i = 0; i < vRayNumber; i++)
        {
            float x, y;

            // x, y ��ǥ ���
            x = bound.min.x + vRaySpacing * i;
            y = Mathf.Sqrt(Mathf.Abs(Mathf.Pow(bound.size.x / 2f, 2) - Mathf.Pow(Mathf.Abs(x - bound.center.x), 2))) - colPadding;

            // ������ ����
            colPoints.top[i].Set(x, bound.max.y - (bound.size.x / 2f) + y);
            colPoints.bottom[i].Set(x, bound.min.y + (bound.size.x / 2f) - y);
        }
    }
}