using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]    //PlayerInputManager를 가진 오브젝트는 반드시 PlayerController도 같이 가지고 있어야
public class PlayerInputManager : MonoBehaviour
{
    private PlayerController controller;

    void Start()
    {
        controller = GetComponent<PlayerController>();
    }


    void Update()
    {
        float deltaX = Input.GetAxis("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space)) { controller.Jump(); }
        if (Input.GetKeyDown(KeyCode.F)) { controller.Vault(deltaX); }

        controller.Move(deltaX, Time.deltaTime);
    }
}
