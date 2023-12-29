using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]    //PlayerInputManager�� ���� ������Ʈ�� �ݵ�� PlayerController�� ���� ������ �־��
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
        float deltaY = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space)) { controller.Jump(); controller.DetachFromLadder(true); }
        if (Input.GetKeyDown(KeyCode.F)) { controller.Vault(deltaX); }

        controller.Move(deltaX, deltaY);
    }
}
