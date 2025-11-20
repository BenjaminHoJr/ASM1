using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float movespeed = 5.0f;

    private Rigidbody rb;
    private Vector2 moveInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError("PlayerController requires a Rigidbody component.");
    }

    // Update is called once per frame
    void Update()
    {
    }

    void FixedUpdate()
    {
        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);
        rb.MovePosition(rb.position + movement * movespeed * Time.fixedDeltaTime);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}
