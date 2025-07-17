using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField] AnimationCurve accelerationCurve;
    [SerializeField] AnimationCurve decelerationCurve;
    [SerializeField] float turnSpeed = 70;
    [SerializeField] float maxSpeed = 20;

    [Header("Jump Parameters")]
    [SerializeField] float jumpHeight = 2;
    [SerializeField] float jumpTotalDuration = 1;
    [SerializeField] float landingGravityIncreaseMultiplier = 1;

    [Header("Input Map")]
    [SerializeField] InputActionAsset inputActions;
    InputAction moveAction;
    InputAction jumpAction;

    Rigidbody rb;
    float moveInput;
    Vector3 currentVelocity;
    float desiredVelocity;
    float accelerationTimer;
    float decelerationTimer;

    bool increasedGravityApplied = false;
    float changeJumpGravityThreshold = -0.01f;
    float gravityMultiplier = 1;

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
        accelerationTimer = accelerationCurve[0].time; // Set timer to the beginning of the curve
        decelerationTimer = decelerationCurve[decelerationCurve.length - 1].time; // Set timer to the end of the curve

        jumpAction.performed += Jump;
    }

    private void FixedUpdate()
    {
        currentVelocity = rb.linearVelocity;

        Move();

        if (rb.linearVelocity.y < changeJumpGravityThreshold && !increasedGravityApplied)
        {
            Debug.Log("Mudou");
            gravityMultiplier = landingGravityIncreaseMultiplier;
            increasedGravityApplied = true;
            UpdateGravity();
            gravityMultiplier = 1; // Restarts gravityMultiplier for next jump
        }
    }

    #region Movement
    void Move()
    {
        moveInput = moveAction.ReadValue<float>();
        desiredVelocity = moveInput * maxSpeed;

        if (moveInput != 0)
        {
            decelerationTimer = decelerationCurve[0].time; // Set timer to the beginning of the curve
            if (Mathf.Sign(moveInput) == Mathf.Sign(currentVelocity.x))
            {
                //currentAcceleration = acceleration * Time.deltaTime;
                currentVelocity.x = desiredVelocity * accelerationCurve.Evaluate(accelerationTimer);
            }
            else
            {
                currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, desiredVelocity, turnSpeed * Time.deltaTime);
                accelerationTimer = accelerationCurve[0].time; // Set timer to the beginning of the curve
            }
            accelerationTimer += Time.deltaTime;
        }
        else
        {
            currentVelocity.x = currentVelocity.x * decelerationCurve.Evaluate(decelerationTimer);

            decelerationTimer += Time.deltaTime;
            accelerationTimer = accelerationCurve[0].time; // Set timer to the beginning of the curve
        }
        rb.linearVelocity = currentVelocity;
    }

    void Jump(InputAction.CallbackContext context)
    { 
        UpdateGravity();
        float initialVerticalVelocity = GetInitialVerticalVelocity();

        increasedGravityApplied = false;
        currentVelocity.y = initialVerticalVelocity;
        rb.linearVelocity = currentVelocity;
    }
    #endregion

    void UpdateGravity()
    {
        float jumpHalfDuration = jumpTotalDuration / 2;

        float gravity = -2 * jumpHeight / Mathf.Pow(jumpHalfDuration, 2) * gravityMultiplier;
        Physics.gravity = new Vector3(0, gravity, 0);
        //Debug.Log($"Gravidade: {gravity}");
    }

    float GetInitialVerticalVelocity()
    {
        float jumpHalfDuration = jumpTotalDuration / 2;

        //Debug.Log($"Velocidade inicial: {2 * jumpHeight / jumpHalfDuration}");
        return 2 * jumpHeight / jumpHalfDuration;
    }
}
