using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerParkour : MonoBehaviour
{
    float _jumpPower = 8f;
    float _verticalSpeed = 1f;
    public Transform _ignoredPlatform;
    public Transform _detectedPlatform;
    Rigidbody2D _rigidbody2d;
    private void Awake()
    {
        _rigidbody2d = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        _detectedPlatform = Physics2D.Raycast(transform.position + Vector3.down , Vector2.down, 0.1f, LayerMask.GetMask("Platform")).transform;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _rigidbody2d.AddForce(Vector2.up * _jumpPower, ForceMode2D.Impulse);
        }
        if(Input.GetKey(KeyCode.DownArrow))
        {
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), true);
            _ignoredPlatform = _detectedPlatform;
        }
        if(_detectedPlatform != null && _ignoredPlatform != null)
        {
            if(_detectedPlatform != _ignoredPlatform)
            {
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), false);
                _ignoredPlatform = null;
            }
        }
    }
}
