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
    [SerializeField] float coyoteTime = 0.2f;
    [SerializeField] LayerMask groundLayerMask;

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

    bool onGround;
    float groundRaycastLength = 1.1f;
    bool increasedGravityApplied = false;
    float changeJumpGravityThreshold = -0.01f;
    float gravityMultiplier = 1;
    bool pressingJump = false;
    bool currentlyJumping = false;
    float coyoteTimeCounter = 0;
    float coyoteTimeThreshold = 0.03f;

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
        jumpAction.canceled += StoppedPressingJump;
    }

    private void Update()
    {
        CheckOnGround();

        if (onGround)
        {
            currentlyJumping = false;
        }
        
        if (!currentlyJumping && !onGround)
        {
            coyoteTimeCounter += Time.deltaTime;
        }
        else
        {
            coyoteTimeCounter = 0;
        }
        // Debug.Log("Jumping " + currentlyJumping);
        // Debug.Log("OnGroun " + onGround);
        Debug.Log(coyoteTimeCounter);
    }

    private void FixedUpdate()
    {
        currentVelocity = rb.linearVelocity;

        Move();

        if ((rb.linearVelocity.y < changeJumpGravityThreshold || !pressingJump) && !increasedGravityApplied)
        {
            gravityMultiplier = landingGravityIncreaseMultiplier;
            increasedGravityApplied = true;
            UpdateGravity();
            gravityMultiplier = 1; // Restarts gravityMultiplier for next jump
        }
    }

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

    #region Jump
    void Jump(InputAction.CallbackContext context)
    {
        pressingJump = true;

        Debug.Log(onGround);
        if (onGround || (coyoteTimeCounter > coyoteTimeThreshold && coyoteTimeCounter < coyoteTime))
        {
            UpdateGravity();
            float initialVerticalVelocity = GetInitialVerticalVelocity();
 
            currentVelocity.y = initialVerticalVelocity;
            rb.linearVelocity = currentVelocity;

            currentlyJumping = true;
            coyoteTimeCounter = 0;
            increasedGravityApplied = false;
        }
    }

    void CheckOnGround()
    {
        onGround = Physics.Raycast(transform.position, Vector3.down, groundRaycastLength, groundLayerMask);
    }

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

    void StoppedPressingJump(InputAction.CallbackContext context)
    {
        pressingJump = false;
    }
    #endregion
}
