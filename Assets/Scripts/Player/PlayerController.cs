using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField] AnimationCurve accelerationCurve;
    [SerializeField] AnimationCurve decelerationCurve;
    [SerializeField] float turnAcceleration = 70;
    [SerializeField] float maxHorizontalSpeed = 20;
    [SerializeField] float maxVerticalSpeed = 30;

    [Header("Jump Parameters")]
    [SerializeField] float jumpHeight = 2;
    [SerializeField] float jumpTotalDuration = 1;
    [SerializeField] float landingGravityIncreaseMultiplier = 1;
    [SerializeField] float coyoteTime = 0.2f;
    [SerializeField] float jumpBuffer = 0.2f;
    [SerializeField] LayerMask groundLayerMask;

    [Header("Input Map")]
    public InputActionAsset inputActions;
    InputAction moveAction;
    InputAction jumpAction;

    Rigidbody rb;
    float moveInput;
    Vector3 currentVelocity;
    Vector3 acceleration = Vector3.zero;
    float accelerationTimer;
    float decelerationTimer;
    GameObject bodyMesh;
    Vector3 cursorPos;
    Vector3 newScale;

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

        moveAction = inputActions.FindAction("Move");
        jumpAction = inputActions.FindAction("Jump");

        jumpAction.performed += StartJumbBufferCounter;
        jumpAction.canceled += StoppedPressingJump;
        EventBus.Instance.checkOnGround += CheckOnGround;
    }

    private void OnDisable()
    {
        inputActions.FindActionMap("Player").Disable();

        jumpAction.performed -= StartJumbBufferCounter;
        jumpAction.canceled -= StoppedPressingJump;
        EventBus.Instance.checkOnGround -= CheckOnGround;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        accelerationTimer = accelerationCurve[0].time; // Set timer to the beginning of the curve
        decelerationTimer = decelerationCurve[decelerationCurve.length - 1].time; // Set timer to the end of the curve

        bodyMesh = GameObject.Find("BodyMesh");

        coyoteTimeCounter = coyoteTime;
    }

    private void Update()
    {
        CheckOnGround();

        SetPlayerDirection();
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

        LimitPlayerHorizontalSpeed();
    }

    private void LateUpdate()
    {
        HandleState();
    }

    #region Move
    void Move()
    {
        moveInput = moveAction.ReadValue<float>();

        if (moveInput != 0)
        {
            decelerationTimer = decelerationCurve[0].time; // Set timer to the beginning of the curve
            if (Mathf.Sign(moveInput) == Mathf.Sign(currentVelocity.x))
            {
                //currentAcceleration = acceleration * Time.deltaTime;
                acceleration.x = Mathf.Sign(moveInput) * accelerationCurve.Evaluate(accelerationTimer);
            }
            else
            {
                acceleration.x = Mathf.Sign(moveInput) * turnAcceleration;
                accelerationTimer = accelerationCurve[0].time; // Set timer to the beginning of the curve
            }
            accelerationTimer += Time.deltaTime;
        }
        else
        {
            int decelerationSign = SetDecelerationSign();
            acceleration.x = decelerationSign * decelerationCurve.Evaluate(decelerationTimer);

            decelerationTimer += Time.deltaTime;
            accelerationTimer = accelerationCurve[0].time; // Set timer to the beginning of the curve
        }
        rb.linearVelocity += acceleration * Time.deltaTime;

    }

    void LimitPlayerHorizontalSpeed()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.x = Mathf.Clamp(velocity.x, -maxHorizontalSpeed, maxHorizontalSpeed);
        velocity.y = Mathf.Clamp(velocity.y, -maxVerticalSpeed, float.PositiveInfinity);
        rb.linearVelocity = velocity;
    }

    int SetDecelerationSign()
    {
        if (currentVelocity.x > 0)
        {
            return 1;
        } else if (currentVelocity.x < 0)
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }

    void SetPlayerDirection()
    {
        cursorPos = CursorTracker.Instance.cursorPos;
        cursorPos.z = cursorPos.z - (Camera.main.transform.position.z);
        cursorPos = Camera.main.ScreenToWorldPoint(cursorPos);

        if (bodyMesh.transform.localScale.x != 1 && cursorPos.x >= transform.position.x)
        {
            newScale = transform.localScale;
            newScale.x = 1;
            bodyMesh.transform.localScale = newScale;
        }
        else if (bodyMesh.transform.localScale.x != -1 && cursorPos.x < transform.position.x)
        {
            newScale = transform.localScale;
            newScale.x = -1;
            bodyMesh.transform.localScale = newScale;
        }
    }
    #endregion

    #region Jump
    void Jump()
    {
        if (coyoteTimeCounter > 0 && jumpBufferCounter > 0)
        {
            Debug.Log("Jump");
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

    bool CheckOnGround()
    {
        onGround = Physics.Raycast(transform.position, Vector3.down, groundRaycastLength, groundLayerMask);

        return onGround;
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
                EventBus.Instance.landedOnGround.Invoke();
            }
        }
    }
    #endregion
}
