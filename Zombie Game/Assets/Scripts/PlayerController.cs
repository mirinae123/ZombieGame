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

    private BoxCollider2D bc;   //자신의 콜라이더

    private Vector2 velocity;

    public const int hRayNumber = 6;    //HorizontalCheck에서 Raycast 발사 횟수
    public const int vRayNumber = 6;
    private float hRaySpacing, vRaySpacing;
    private const float colPadding = .1f;

    private bool disableMovement = false;

    struct CollisionPoint   
    {
        internal Vector2 topLeft, topRight;
        internal Vector2 bottomLeft, bottomRight;
    }
    private CollisionPoint colPoint;    //패딩 적용한 콜라이더의 꼭짓점

    void Start()
    {
        bc = GetComponent<BoxCollider2D>(); //콜라이더

        hRaySpacing = (bc.size.y - colPadding * 2f) / (hRayNumber - 1);
        vRaySpacing = (bc.size.x - colPadding * 2f) / (vRayNumber - 1);
    }

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
        }
    }

    void VerticalCheck()
    {
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
        }
    }

    public void Jump()
    {
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
        }
    }

    void UpdateCollisionPoint()
    {
        Bounds bound = bc.bounds;       //콜라이더의 위치, 크기 정보
        bound.Expand(colPadding * -2);  //패딩만큼 범위 축소

        //colPoint에 콜라이더의 위치정보 저장
        colPoint.topLeft = new Vector2(bound.min.x, bound.max.y);
        colPoint.topRight = new Vector2(bound.max.x, bound.max.y);
        colPoint.bottomLeft = new Vector2(bound.min.x, bound.min.y);
        colPoint.bottomRight = new Vector2(bound.max.x, bound.min.y);
    }

    public void Vault(float deltaX)
    {
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
            {
                GameObject vaultObj = hit.collider.gameObject;
                BoxCollider2D vaultCol = vaultObj.GetComponent<BoxCollider2D>();

                float endOffset = (bc.size.x + vaultCol.size.x) / 2f;   //자신의 넓이와 물체의 넓이의 평균

                Vector3 mid = vaultObj.transform.position + Vector3.up * vaultCol.size.y / 2f;  //x=물체의 중앙, y=물체의 위끝
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
        disableMovement = true; //움직일 수 없음

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
