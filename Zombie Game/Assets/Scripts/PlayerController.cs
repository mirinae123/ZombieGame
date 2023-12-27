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

    private BoxCollider2D bc;

    private Vector2 velocity;

    public const int hRayNumber = 6;
    public const int vRayNumber = 6;
    private float hRaySpacing, vRaySpacing;
    private const float colPadding = .1f;

    private bool disableMovement = false;

    struct CollisionPoint
    {
        internal Vector2 topLeft, topRight;
        internal Vector2 bottomLeft, bottomRight;
    }
    private CollisionPoint colPoint;

    // Start is called before the first frame update
    void Start()
    {
        bc = GetComponent<BoxCollider2D>();

        hRaySpacing = (bc.size.y - colPadding * 2f) / (hRayNumber - 1);
        vRaySpacing = (bc.size.x - colPadding * 2f) / (vRayNumber - 1);
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Move(float deltaX, float deltaTime)
    {
        if (disableMovement) return;

        velocity.x = deltaX * deltaTime * walkSpeed;
        velocity.y -= gravity * deltaTime;

        if (velocity.y < -1f) velocity.y = -1f;

        UpdateCollisionPoint();
        if (velocity.x != 0) HorizontalCheck();
        if (velocity.y != 0) VerticalCheck();
        transform.Translate(velocity);
    }

    void HorizontalCheck()
    {
        float hDir = Mathf.Sign(velocity.x);
        float hLen = Mathf.Abs(velocity.x) + colPadding;

        Vector2 origin = hDir > 0 ? colPoint.bottomRight : colPoint.bottomLeft;

        for (int i = 0; i < hRayNumber; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * hDir, hLen, LayerMask.GetMask("Solid"));

            if (hit)
            {
                hLen = hit.distance;
                velocity.x = (hit.distance - colPadding) * hDir;
            }

            origin += Vector2.up * hRaySpacing;
        }
    }

    void VerticalCheck()
    {
        float vDir = Mathf.Sign(velocity.y);
        float vLen = Mathf.Abs(velocity.y) + colPadding;

        Vector2 origin = vDir > 0 ? colPoint.topLeft : colPoint.bottomLeft;

        for (int i = 0; i < vRayNumber; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up * vDir, vLen, LayerMask.GetMask("Solid"));

            if (hit)
            {
                vLen = hit.distance;
                velocity.y = (hit.distance - colPadding) * vDir;
            }

            origin += Vector2.right * vRaySpacing;
        }
    }

    public void Jump()
    {
        Vector2 origin = colPoint.bottomLeft;

        for (int i = 0; i < vRayNumber; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, colPadding * 2f, LayerMask.GetMask("Solid"));

            if (hit)
            {
                velocity.y = jumpForce;
                return;
            }

            origin += Vector2.right * vRaySpacing;
        }
    }

    void UpdateCollisionPoint()
    {
        Bounds bound = bc.bounds;
        bound.Expand(colPadding * -2);
        colPoint.topLeft = new Vector2(bound.min.x, bound.max.y);
        colPoint.topRight = new Vector2(bound.max.x, bound.max.y);
        colPoint.bottomLeft = new Vector2(bound.min.x, bound.min.y);
        colPoint.bottomRight = new Vector2(bound.max.x, bound.min.y);
    }

    public void Vault(float deltaX)
    {
        if (disableMovement) return;

        float hDir = Mathf.Sign(deltaX);
        float hLen = 1.5f + colPadding;

        Vector2 origin = hDir > 0 ? colPoint.bottomRight : colPoint.bottomLeft;

        for (int i = 0; i < hRayNumber; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * hDir, hLen, LayerMask.GetMask("Solid"));
            Debug.DrawRay(origin, Vector2.right * hDir, Color.red, .5f);

            if (hit && hit.collider.CompareTag("Vaultable"))
            {
                GameObject vaultObj = hit.collider.gameObject;
                BoxCollider2D vaultCol = vaultObj.GetComponent<BoxCollider2D>();

                float endOffset = (bc.size.x + vaultCol.size.x) / 2f;

                Vector3 mid = vaultObj.transform.position + Vector3.up * vaultCol.size.y / 2f;
                Vector3 end = vaultObj.transform.position + Vector3.right * endOffset * hDir + Vector3.up * .1f;
                StartCoroutine(ExecuteVault(mid, end));
                return;
            }

            origin += Vector2.up * hRaySpacing;
        }
    }

    IEnumerator ExecuteVault(Vector2 mid, Vector2 end)
    {
        disableMovement = true;

        velocity = Vector2.zero;
        yield return new WaitForSeconds(.5f);
        transform.position = mid;
        yield return new WaitForSeconds(.5f);
        transform.position = end;

        disableMovement = false;
    }
}
