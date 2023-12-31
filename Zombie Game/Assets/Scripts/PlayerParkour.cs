using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class PlayerParkour : MonoBehaviour
{
    float _jumpPower = 8f;
    float _verticalSpeed = 1f;
    // Solid 레이어와 Platform 레이어 두개를 검사
    int _layerMaskCombined = (1 << 7) | (1 << 8);
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
        _detectedPlatform = Physics2D.Raycast(transform.position + Vector3.down , Vector2.down, 0.1f, _layerMaskCombined).transform;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _rigidbody2d.AddForce(Vector2.up * _jumpPower, ForceMode2D.Impulse);
        }
        if(Input.GetKey(KeyCode.DownArrow))
        {
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), true);
            _ignoredPlatform = _detectedPlatform;
        }
        if (_detectedPlatform != null && _detectedPlatform != _ignoredPlatform)
        {
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), false);
            _ignoredPlatform = null;
        }
    }
}
