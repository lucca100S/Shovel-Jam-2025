using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Parameters")]
    public AnimationCurve accelerationCurve;
    public AnimationCurve decelerationCurve;
    public float turnSpeed;
    public float maxSpeed;

    [Header("Jump Parameters")]
    public float jumpHeight;
    public float jumpSpeed;

    [Header("Input Map")]
    [SerializeField] InputActionAsset inputActions;
    InputAction moveAction;
    InputAction jumpAction;

    Rigidbody rb;
    float moveInput;
    Vector3 currentVelocity = Vector3.zero;
    float desiredVelocity;
    float currentAcceleration;
    float accelerationTimer = 0;
    float decelerationTimer = 1;

    private void OnEnable()
    {
        inputActions.FindActionMap("Player").Enable();
    }

    private void OnDisable()
    {
        inputActions.FindActionMap("Player").Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        moveAction = inputActions.FindAction("Move");
        jumpAction = inputActions.FindAction("Jump");

        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Move();

        if (jumpAction.WasPressedThisFrame())
        {
            Jump();
        }
    }

    #region Movement
    void Move()
    {
        moveInput = moveAction.ReadValue<float>();
        desiredVelocity = moveInput * maxSpeed;

        if (moveInput != 0)
        {
            decelerationTimer = 0;
            if (Mathf.Sign(moveInput) == Mathf.Sign(currentVelocity.x))
            {
                //currentAcceleration = acceleration * Time.deltaTime;
                currentVelocity.x = desiredVelocity * accelerationCurve.Evaluate(accelerationTimer);
            }
            else
            {
                currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, desiredVelocity, turnSpeed * Time.deltaTime);
                accelerationTimer = 0;
            }
            accelerationTimer += Time.deltaTime;
        }
        else
        {
            currentVelocity.x = currentVelocity.x * decelerationCurve.Evaluate(decelerationTimer);

            decelerationTimer += Time.deltaTime;
            accelerationTimer = 0;
        }
        rb.linearVelocity = currentVelocity;
    }

    void Jump()
    {
        // Alterar calculo de movimento
        Vector3 jumpPos = transform.position + new Vector3(0, jumpHeight, 0);
        transform.position = Vector3.Lerp(transform.position, jumpPos, jumpSpeed*Time.deltaTime);
    }
    #endregion
}
