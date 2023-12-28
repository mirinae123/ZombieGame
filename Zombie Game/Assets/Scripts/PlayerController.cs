using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Transactions;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.UI.Image;


// ���� �̵��� �ε巴�� ó���ϱ� ���� ���� ���� ���� ���߿� �ִ� ����� gravityScale�� �ٸ��� ��
// �������� gravityScale�� �� ũ�� �Ͽ� ���ο��� ���� ����� �ʵ��� ����


// �÷����� Platform, Contacted Platform ���̾�� ����
//  - Platform: �Ϲ����� �÷���
//  - Contacted Platform: ���� �÷��̾ ���� ���� �÷���
//
// Platform ���̾ ���Ե� �÷����� �÷��̾��� �̵� ���¿� ���� �浹 ���ΰ� ����
// �÷��̾ �������� �����鼭, ���θ� �������� ���� �ƴ� ���� �浹 Ȱ��ȭ
//
// Contacted Platform ���̾ ���Ե� �÷����� �׻� �浹 Ȱ��ȭ


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
    private GameObject ground;
    private GameObject downJumpGround;
    private bool isGrounded = true;

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
        // �÷��̾��� ��ġ�� ���� �ֺ� �÷����� �浹 ���� ������Ʈ
        UpdatePlatforms();

        if (rb.velocity.y >= 0) // �÷��̾ ������ �� �ö󰡰� �ִ� ���
        {
            // �÷��̾�� �Ϲ� �÷����� �浹 ��Ȱ��ȭ
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"));
        }
        else if (!isGrounded)   // �÷��̾ �������� �����鼭, ���� ������� ���� ���(���θ� �������� ���� ���� ���)
        {
            // �÷��̾�� �Ϲ� �÷����� �浹 Ȱ��ȭ
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), false);
        }
    }

    // �÷��̾��� �¿� �̵� ����
    // float deltaX: �÷��̾��� axis �Է� ��
    public void Move(float deltaX)
    {
        if (canMove)                                            // ������ �� �ִ� ���
        {
            velocity.Set(deltaX * walkSpeed, rb.velocity.y);    // �ӵ� ����� ������ ������ ��
            rb.velocity = velocity;                             // rigidBody�� ����
        }
    }

    // ���� ����
    public void Jump()
    {
        if (isGrounded && canMove)                                      // ���� ��� �ְ�, ������ �� �ִ� ���
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);   // jumpForce ����
            rb.gravityScale = 3f;                                       // ���߿� ���� ���� �߷� ����
        }
    }

    // �Ʒ����� ����
    public void DownJump()
    {
        if (isGrounded && canMove && ground.CompareTag("Platform"))         // ���� ��� �ְ�, ������ �� �ְ�, ��� �ִ� ���� �÷����� ���
        {
            downJumpGround = ground;                                        // �Ͻ������� �浹�� ��Ȱ��ȭ�� �÷����� downJumpGround ������ ����
            downJumpGround.GetComponent<BoxCollider2D>().isTrigger = true;  // �浹 ��Ȱ��ȭ
            downJumpGround.layer = LayerMask.NameToLayer("Platform");       // ���̾ �Ϲ� �÷������� ����

            isGrounded = false;                                             // ���� ���� ��Ȱ��ȭ
            rb.gravityScale = 3f;                                           // ���߿� ���� ���� �߷� ����
        }
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
                // ���������κ���, �÷��̾ �ٶ󺸴� ��������, 1.5f ��ŭ raycast �߻�
                RaycastHit2D hit = Physics2D.Raycast(origin[i], Vector2.right * direction, 1.5f + colPadding, LayerMask.GetMask("Solid"));

                if (hit && hit.collider.CompareTag("Vaultable"))                        // hit�� ��ü�� Vaultable�̶��
                {
                    GameObject vaultObj = hit.collider.gameObject;                      // ��ü �ӽ� ����
                    BoxCollider2D vaultCol = vaultObj.GetComponent<BoxCollider2D>();    // boxCollider �ӽ� ����

                    float endOffset = (cc.bounds.size.x + vaultCol.bounds.size.x);      // �÷��̾ ���������� �̵��ؾ� �� �Ÿ� ���

                    Vector3 mid = vaultObj.transform.position + Vector3.up * vaultCol.size.y;                       // �߰� ��ġ ���
                    Vector3 end = transform.position + Vector3.right * endOffset * direction + Vector3.up * .1f;    // ���� ��ġ ���
                    StartCoroutine(ExecuteVault(mid, end));                             // ��ü �پ�ѱ� �ڷ�ƾ ȣ��
                    return;
                }
            }
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

    // �÷��̾ �������� ���� �ִ� �÷����� �浹 ��Ȱ��ȭ
    // �Ʒ��� �ִ� �÷����� �浹 Ȱ��ȭ
    private void UpdatePlatforms()
    {
        UpdateCollisionPoints();    // raycast ������ ����

        for (int i = 0; i < vRayNumber; i++)
        {
            // �÷��̾��� �Ʒ������κ���, �� ��������, ĸ�� �ݶ��̴��� 2�� �Ÿ���ŭ raycast �߻�
            RaycastHit2D topHit = Physics2D.Raycast(colPoints.bottom[i], Vector2.up, cc.bounds.size.y * 2, LayerMask.GetMask("Platform"));
            // �÷��̾��� �Ʒ������κ���, �Ʒ� ��������, 1f �Ÿ���ŭ raycast �߻�
            RaycastHit2D bottomHit = Physics2D.Raycast(colPoints.bottom[i], Vector2.down, 1f, LayerMask.GetMask("Platform"));

            // ���� �ִ� �÷����� �浹 ��Ȱ��ȭ
            if (topHit) topHit.collider.gameObject.GetComponent<BoxCollider2D>().isTrigger = true;
            // �Ʒ��� �����鼭, �Ͻ������� �浹�� ��Ȱ��ȭ�� �÷���(downJumpGround)�� �ƴ϶�� �浹 Ȱ��ȭ
            if (bottomHit && bottomHit.collider.gameObject != downJumpGround) bottomHit.collider.gameObject.GetComponent<BoxCollider2D>().isTrigger = false;
        }
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
            y = Mathf.Sqrt(Mathf.Pow(bound.size.x / 2f, 2) - Mathf.Pow(Mathf.Abs(x - bound.center.x), 2)) - colPadding;

            // ������ ����
            colPoints.top[i].Set(x, bound.max.y - (bound.size.x / 2f) + y);
            colPoints.bottom[i].Set(x, bound.min.y + (bound.size.x / 2f) - y);
        }

    }

    // �浹 ���� �� ȣ��
    // ���� ���� �Ǵ�, �Ʒ� ������ ���� ��Ȱ���� �÷��� ����
    private void OnCollisionEnter2D(Collision2D collision)
    {
        List<ContactPoint2D> contactPoints = new List<ContactPoint2D>();    // ���� ���� ����� ����
        collision.GetContacts(contactPoints);                               // ���� ���� ����
        Vector2 point = contactPoints[0].point;                             // ���� ���� ��ġ

        // �÷��̾ ���� ���� ����
        // ���� ������ x ��ǥ�� �÷��̾� ���ʿ� �ְ�, ���� ������ y ��ǥ�� �÷��̾� �Ʒ��� �ִ� ��� ����
        if (point.x >= cc.bounds.min.x && point.x <= cc.bounds.max.x && point.y <= transform.position.y)
        {
            isGrounded = true;                      // ���� ���� ����
            ground = collision.collider.gameObject; // ������ ����

            // ���� �÷����� ������ ���̾�� ����
            if (ground.CompareTag("Platform")) ground.layer = LayerMask.NameToLayer("Contacted Platform");

            // �Ʒ������� ���� �浹�� ��Ȱ��ȭ�� �÷����� �ִٸ�
            if (downJumpGround)
            {
                downJumpGround.GetComponent<BoxCollider2D>().isTrigger = false; // �浹 Ȱ��ȭ ��
                downJumpGround = null;                                          // ���� �ʱ�ȭ
            }

            rb.gravityScale = 5f;   // ���߿� ���� ���� �߷� ����
        }
    }

    // �浹 ���� �� ȣ��
    // ���� ���� ����
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.gameObject == ground)                    // ��� ��ü�� ���� �������̾��� ���
        {
            isGrounded = false;                                         // ���� ���� ����

            // ���� �÷����� �Ϲ� �÷������� ����
            if (ground.layer == LayerMask.NameToLayer("Contacted Platform")) ground.layer = LayerMask.NameToLayer("Platform");
            ground = null;

            rb.gravityScale = 3f;   // ���߿� ���� ���� �߷� ����
        }
    }
}