using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerInputManager : MonoBehaviour
{
    private PlayerController controller;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        float deltaX = Input.GetAxis("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space)) { controller.Jump(); }
        if (Input.GetKeyDown(KeyCode.F)) { controller.Vault(deltaX); }

        controller.Move(deltaX, Time.deltaTime);
    }
}
