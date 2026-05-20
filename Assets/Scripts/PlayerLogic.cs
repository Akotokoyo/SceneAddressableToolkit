using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    private PlayerAction controls;
    private CharacterController controller;

    private Vector2 moveInput;

    void Awake()
    {
        controls = new PlayerAction();
        controller = GetComponent<CharacterController>();

        controls.Map.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Map.Movement.canceled += _ => moveInput = Vector2.zero;
    }

    void Update()
    {
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        controller.Move(move * moveSpeed * Time.deltaTime);
    }


    private void OnEnable()
    {
        controls.Map.Enable();
    }

    private void OnDisable()
    {
        controls.Map.Disable();
    }
}
