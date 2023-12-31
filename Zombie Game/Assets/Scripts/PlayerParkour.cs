using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerParkour : MonoBehaviour
{
    float _jumpPower = 8f;
    float _verticalSpeed = 1f;

    Rigidbody2D _rigidbody2d;
    private void Awake()
    {
        _rigidbody2d = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            _rigidbody2d.AddForce(Vector2.up * _jumpPower, ForceMode2D.Impulse);
        }
        if(Input.GetKey(KeyCode.DownArrow))
        {
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), true);
        }
        else
        {
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), false);
        }
    }
}
