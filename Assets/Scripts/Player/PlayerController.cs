using System.Collections;
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
    [SerializeField] float jumpBuffer = 0.2f;
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

    PlayerStates state = PlayerStates.OnGround;
    bool pressingJump = false;
    bool onGround;
    float groundRaycastLength = 1.1f;

    bool increasedGravityApplied = false;
    float changeJumpGravityThreshold = -0.01f;
    float gravityMultiplier = 1;

    float coyoteTimeCounter;
    float jumpBufferCounter = 0;

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

        jumpAction.performed += StartJumbBufferCounter;
        jumpAction.canceled += StoppedPressingJump;

        coyoteTimeCounter = coyoteTime;
    }

    private void Update()
    {
        CheckOnGround();


        //Debug.Log(onGround);
        //Debug.Log(state);
        //Debug.Log(coyoteTimeCounter);
        Debug.Log(pressingJump);
    }

    private void FixedUpdate()
    {
        currentVelocity = rb.linearVelocity;

        Move();

        Jump();

        if ((rb.linearVelocity.y < changeJumpGravityThreshold || !pressingJump) && !increasedGravityApplied)
        {
            gravityMultiplier = landingGravityIncreaseMultiplier;
            increasedGravityApplied = true;
            UpdateGravity();
            gravityMultiplier = 1; // Restarts gravityMultiplier for next jump
        }
    }

    private void LateUpdate()
    {
        HandleState();
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
    void Jump()
    {
        if (coyoteTimeCounter > 0 && jumpBufferCounter > 0)
        {
            UpdateGravity();
            float initialVerticalVelocity = GetInitialVerticalVelocity();
 
            currentVelocity.y = initialVerticalVelocity;
            rb.linearVelocity = currentVelocity;

            coyoteTimeCounter = 0;
            jumpBufferCounter = 0;
            state = PlayerStates.Jumping;
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
    }

    float GetInitialVerticalVelocity()
    {
        float jumpHalfDuration = jumpTotalDuration / 2;

        return 2 * jumpHeight / jumpHalfDuration;
    }

    void StoppedPressingJump(InputAction.CallbackContext context)
    {
        pressingJump = false;
    }

    IEnumerator StartCoyoteTimeCounter()
    {
        coyoteTimeCounter = coyoteTime;

        while (coyoteTimeCounter > 0)
        {
            coyoteTimeCounter -= Time.deltaTime;
            yield return null;
        }
    }

    void StartJumbBufferCounter(InputAction.CallbackContext context)
    {
        pressingJump = true;

        StopCoroutine("JumpBufferCounterCoroutine");
        StartCoroutine(JumpBufferCounterCoroutine());
    }

    IEnumerator JumpBufferCounterCoroutine()
    {
        jumpBufferCounter = jumpBuffer;

        while(jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
            yield return null;
        }
    }

    void HandleState()
    {
        if (!onGround)
        {
            if (state == PlayerStates.OnGround)
            {
                state = PlayerStates.OnAir;
                StopCoroutine("StartCoyoteTimeCounter");
                StartCoroutine(StartCoyoteTimeCounter());
            }
        }
        else
        {
            if (state == PlayerStates.Jumping || state == PlayerStates.OnAir)
            {
                state = PlayerStates.OnGround;
                coyoteTimeCounter = coyoteTime;
            }
        }
    }
    #endregion
}
