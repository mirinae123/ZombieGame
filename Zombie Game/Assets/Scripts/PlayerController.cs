using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Transactions;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.Image;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 10f;
    public float runSpeed = 20f;
    public float crouchSpeed = 5f;

    public float jumpForce = 0.007f;
    public float gravity = 0.01f;

    private BoxCollider2D bc;   //�ڽ��� �ݶ��̴�

    private Vector2 velocity;

    public const int hRayNumber = 6;    //HorizontalCheck���� Raycast �߻� Ƚ��
    public const int vRayNumber = 6;
    private float hRaySpacing, vRaySpacing;
    private const float colPadding = .1f;

    private bool disableMovement = false;

    struct CollisionPoint   
    {
        internal Vector2 topLeft, topRight;
        internal Vector2 bottomLeft, bottomRight;
    }
    private CollisionPoint colPoint;    //�е� ������ �ݶ��̴��� ������

    void Start()
    {
        bc = GetComponent<BoxCollider2D>(); //�ݶ��̴�

        hRaySpacing = (bc.size.y - colPadding * 2f) / (hRayNumber - 1);
        vRaySpacing = (bc.size.x - colPadding * 2f) / (vRayNumber - 1);
    }

    public void Move(float deltaX, float deltaTime)
    {
        if (disableMovement) return;    //�̵����� ���� �˻�

        velocity.x = deltaX * deltaTime * walkSpeed;    //x�� ���ӵ�
        velocity.y -= gravity * deltaTime;              //�߷°��ӵ�

        if (velocity.y < -1f) velocity.y = -1f;     //���ܼӵ�

        UpdateCollisionPoint();     
        if (velocity.x != 0) HorizontalCheck(); //�浹�˻�
        if (velocity.y != 0) VerticalCheck();   //�浹�˻�
        transform.Translate(velocity);      //�̵�
    }

    void HorizontalCheck()
    {
        float hDir = Mathf.Sign(velocity.x);                //Raycast����, x�� �̵� ����
        float hLen = Mathf.Abs(velocity.x) + colPadding;    //Raycast����, x�� �̵� �ӷ�

        Vector2 origin = hDir > 0 ? colPoint.bottomRight : colPoint.bottomLeft; //Raycast �߻� ��ġ

        for (int i = 0; i < hRayNumber; i++)    //���̸� �������� Raycast�߻�
        {
            //���� �����ӿ� �浹���� ����
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * hDir, hLen, LayerMask.GetMask("Solid"));

            if (hit)
            {
                hLen = hit.distance;    //�� �ڵ�� ���� �뵵?
                velocity.x = (hit.distance - colPadding) * hDir;    //�浹���� �ʰ� �ӵ� ����
            }

            origin += Vector2.up * hRaySpacing; //�߻� ���� ���
        }
    }

    void VerticalCheck()
    {
        float vDir = Mathf.Sign(velocity.y);                //Raycast����, y�� �̵� ����
        float vLen = Mathf.Abs(velocity.y) + colPadding;    //Raycast����, y�� �̵� �ӷ�

        Vector2 origin = vDir > 0 ? colPoint.topLeft : colPoint.bottomLeft; //Raycast �߻� ��ġ

        for (int i = 0; i < vRayNumber; i++)    //��ġ�� �ٲ㰡�� Raycast�߻�
        {
            //���� �����ӿ� �浹���� ����
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up * vDir, vLen, LayerMask.GetMask("Solid"));

            if (hit)
            {
                vLen = hit.distance;    //�� �ڵ�� ���� �뵵?
                velocity.y = (hit.distance - colPadding) * vDir;    //�浹���� �ʰ� �ӵ� ����
            }

            origin += Vector2.right * vRaySpacing;  //�߻� ��ġ ����
        }
    }

    public void Jump()
    {
        Vector2 origin = colPoint.bottomLeft;

        //�ٴڿ� ��Ҵ��� �˻�
        for (int i = 0; i < vRayNumber; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, colPadding * 2f, LayerMask.GetMask("Solid"));

            if (hit)    //�ٴڿ� ��Ҵٸ�
            {
                velocity.y = jumpForce; //����
                return;
            }

            origin += Vector2.right * vRaySpacing;
        }
    }

    void UpdateCollisionPoint()
    {
        Bounds bound = bc.bounds;       //�ݶ��̴��� ��ġ, ũ�� ����
        bound.Expand(colPadding * -2);  //�е���ŭ ���� ���

        //colPoint�� �ݶ��̴��� ��ġ���� ����
        colPoint.topLeft = new Vector2(bound.min.x, bound.max.y);
        colPoint.topRight = new Vector2(bound.max.x, bound.max.y);
        colPoint.bottomLeft = new Vector2(bound.min.x, bound.min.y);
        colPoint.bottomRight = new Vector2(bound.max.x, bound.min.y);
    }

    public void Vault(float deltaX)
    {
        if (disableMovement) return;    //�̵� ���� ���� �˻�

        float hDir = Mathf.Sign(deltaX);    //Raycast����
        float hLen = 1.5f + colPadding;     //Raycast����

        Vector2 origin = hDir > 0 ? colPoint.bottomRight : colPoint.bottomLeft; //Raycast�߻� ��ġ

        //�浹 �˻�
        for (int i = 0; i < hRayNumber; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * hDir, hLen, LayerMask.GetMask("Solid"));   //Raycast�߻�
            Debug.DrawRay(origin, Vector2.right * hDir, Color.red, .5f);

            if (hit && hit.collider.CompareTag("Vaultable"))    //Vaultable�� ��Ҵٸ�
            {
                GameObject vaultObj = hit.collider.gameObject;
                BoxCollider2D vaultCol = vaultObj.GetComponent<BoxCollider2D>();

                float endOffset = (bc.size.x + vaultCol.size.x) / 2f;   //�ڽ��� ���̿� ��ü�� ������ ���

                Vector3 mid = vaultObj.transform.position + Vector3.up * vaultCol.size.y / 2f;  //x=��ü�� �߾�, y=��ü�� ����
                Vector3 end = vaultObj.transform.position + Vector3.right * endOffset * hDir + Vector3.up * .1f;
                Debug.Log("-1");
                StartCoroutine(ExecuteVault(mid, end));
                return;
            }

            origin += Vector2.up * hRaySpacing;
        }
    }

    IEnumerator ExecuteVault(Vector2 mid, Vector2 end)
    {
        disableMovement = true; //������ �� ����

        velocity = Vector2.zero;
        Debug.Log("0");
        yield return new WaitForSeconds(.5f);
        Debug.Log("1");
        transform.position = mid;
        yield return new WaitForSeconds(.5f);
        Debug.Log("2");
        transform.position = end;

        disableMovement = false;
    }
}
