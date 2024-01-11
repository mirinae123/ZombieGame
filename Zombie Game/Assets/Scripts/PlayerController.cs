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

    private bool canMove = true;        // �̵� ���� ����

    // ��ٸ�, ��Ÿ�� ���� ����
    private GameObject ladder;
    private GameObject climbable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();
    }

    private void Update()
    {
        // �Ʒ� �ִ� �÷��� ����
        detectedPlatform = CustomBoxCast(CustomBoxCastDirection.Down, 0.2f, layerMaskCombined).transform;

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
            float direction = Mathf.Sign(deltaX);
            // ���������κ���, �÷��̾ �ٶ󺸴� ��������, 1.5f ��ŭ raycast �߻�
            RaycastHit2D hit = CustomBoxCast(direction, 1.5f, LayerMask.GetMask("Solid"));

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

    // ��ٸ��� �ö�Ÿ��
    public void AttachToLadder(float deltaX = 0, bool fromAbove = false)
    {
        if (canMove && canAttachLadder && !(ladder || climbable))  // ������ �� �ְ�, ������ ��ٸ� �Ǵ� ���� Ÿ�� �ִ� ��찡 �ƴ� ���
        {
            // raycast ���������κ���, �÷��̾ �ٶ󺸴� �������� raycast �߻�
            RaycastHit2D hit = CustomBoxCast(deltaX, cc.bounds.extents.x, ~LayerMask.GetMask("Player"));

            if (hit && hit.collider.CompareTag("Ladder") && hit.distance < 1f && (transform.position.y < hit.collider.bounds.max.y || fromAbove))   // ��ٸ��� hit�� ���
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

    // �Ʒ� Ű�� ������ �Ʒ� ��ٸ��� �ö�Ÿ��
    private void AttachToLadderBelow()
    {
        if (canMove && !(ladder || climbable))      // ������ �� �ְ�, ������ ��ٸ� �Ǵ� ���� Ÿ�� �ִ� ��찡 �ƴ� ���
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


    #region CustomBoxCast
    enum CustomBoxCastDirection { Left, Right, Up, Down };  // CustomBoxCast ������ ���ϱ� ���� �Ű�����
    RaycastHit2D CustomBoxCast(CustomBoxCastDirection direction, float distance, int layerMask = -1)
    {
        // -1�� �������� ��Ÿ���� ��� ��Ʈ�� 1

        // Physics2D.Baxcast�� ȣ���ϴµ� �ʿ��� ��ҵ�
        Vector2 boxSize = new Vector2(1, 1);    // �ڽ� ũ��
        Vector2 origin = transform.position;    // ������
        Vector2 _direction = new Vector2();     // ����

        switch (direction)  // ��� �Ҵ�
        {
            case CustomBoxCastDirection.Left:
                origin += Vector2.left * cc.bounds.extents.x;   // x=������, y=�߾�
                boxSize.Set(0.01f, cc.bounds.size.y);
                _direction = Vector2.left;
                break;
            case CustomBoxCastDirection.Right:
                origin += Vector2.right * cc.bounds.extents.x;  // x=������, y=�߾�
                boxSize.Set(0.01f, cc.bounds.size.y);
                _direction = Vector2.right;
                break;
            case CustomBoxCastDirection.Up:
                origin += Vector2.up * cc.bounds.extents.y;     // x=�߾�, y=����
                boxSize.Set(cc.bounds.size.x, 0.01f);
                _direction = Vector2.up;
                break;
            case CustomBoxCastDirection.Down:
                origin += Vector2.down * cc.bounds.extents.y;   // x=�߾�, y=�Ʒ���
                boxSize.Set(cc.bounds.size.x, 0.01f);
                _direction = Vector2.down;
                break;
        }

        //  Physics2D.Baxcast ȣ��
        return Physics2D.BoxCast(origin, boxSize, 0, _direction, distance, layerMask);
    }

    RaycastHit2D CustomBoxCast(float deltaX, float distance, int layerMask = -1)
    {
        // -1�� �������� ��Ÿ���� ��� ��Ʈ�� 1

        if      (deltaX > 0)    return CustomBoxCast(CustomBoxCastDirection.Right, distance, layerMask);
        else if (deltaX < 0)    return CustomBoxCast(CustomBoxCastDirection.Left , distance, layerMask);
        else                    return CustomBoxCast(CustomBoxCastDirection.Down , distance, layerMask);
    }

    #endregion //CustomBoxCast
}