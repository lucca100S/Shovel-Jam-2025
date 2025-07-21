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
    public bool unlockedWallJump = false;
    [SerializeField] int maxNbOfWallJumps = 1;
    [SerializeField] float wallJumpHorizontalStrength;
    [SerializeField] float coyoteTime = 0.2f;
    [SerializeField] float jumpBuffer = 0.2f;
    [SerializeField] LayerMask groundLayerMask;
    [SerializeField] LayerMask wallLayerMask;

    [Header("Input Map")]
    public InputActionAsset inputActions;
    InputAction moveAction;
    InputAction jumpAction;

    public Rigidbody rb;
    float moveInput;
    Vector3 currentVelocity;
    Vector3 acceleration = Vector3.zero;
    float accelerationTimer;
    float decelerationTimer;
    GameObject body;
    Vector3 cursorPos;
    Vector3 newScale;

    PlayerStates state = PlayerStates.OnGround;
    bool pressingJump = false;
    public bool onGround;
    float groundRaycastLength = 1.1f;

    bool increasedGravityApplied = false;
    float changeJumpGravityThreshold = -0.01f;

    float coyoteTimeCounter;
    float jumpBufferCounter = 0;

    bool onWall = false;
    int currentNbOfWallJumps;

    Animator playerAnimator;

    private void OnEnable()
    {
        moveAction = inputActions.FindAction("Move");
        jumpAction = inputActions.FindAction("Jump");

        jumpAction.performed += StartJumbBufferCounter;
        jumpAction.canceled += StoppedPressingJump;
        EventBus.Instance.checkOnGround += CheckOnGround;
        EventBus.Instance.hookAttached += ResetGravity;
        EventBus.Instance.hookReleased += IncreaseGravity;
    }

    private void OnDisable()
    { 
        jumpAction.performed -= StartJumbBufferCounter;
        jumpAction.canceled -= StoppedPressingJump;
        EventBus.Instance.checkOnGround -= CheckOnGround;
        EventBus.Instance.hookAttached -= ResetGravity;
        EventBus.Instance.hookReleased -= IncreaseGravity;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        accelerationTimer = accelerationCurve[0].time; // Set timer to the beginning of the curve
        decelerationTimer = decelerationCurve[decelerationCurve.length - 1].time; // Set timer to the end of the curve

        body = GameObject.Find("Body");

        coyoteTimeCounter = coyoteTime;
        currentNbOfWallJumps = maxNbOfWallJumps;
        IncreaseGravity();

        playerAnimator = GetComponent<Animator>();
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

        if ((rb.linearVelocity.y < changeJumpGravityThreshold || (!pressingJump && !onWall)) && !increasedGravityApplied)
        {
            increasedGravityApplied = true;
            IncreaseGravity();
        }

        playerAnimator.SetFloat("horizontalSpeed", Mathf.Abs(rb.linearVelocity.x));
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

        if (!onWall)
        {
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
        if (!onWall)
        {
            cursorPos = CursorTracker.Instance.cursorPos;
            cursorPos.z = cursorPos.z - (Camera.main.transform.position.z);
            cursorPos = Camera.main.ScreenToWorldPoint(cursorPos);

            if (body.transform.localScale.z != 1 && cursorPos.x >= transform.position.x)
            {
                newScale = transform.localScale;
                newScale.z = 1;
                body.transform.localScale = newScale;
            }
            else if (body.transform.localScale.z != -1 && cursorPos.x < transform.position.x)
            {
                newScale = transform.localScale;
                newScale.z = -1;
                body.transform.localScale = newScale;
            }
        }
    }
    #endregion

    #region Jump
    void Jump()
    {
        if ((onWall || coyoteTimeCounter > 0) && jumpBufferCounter > 0)
        {
            Debug.Log("Jump");

            playerAnimator.SetBool("jumping", true);
            ResetGravity();
            float initialVerticalVelocity = GetInitialVerticalVelocity();
 
            currentVelocity.y = initialVerticalVelocity;

            coyoteTimeCounter = 0;
            jumpBufferCounter = 0;
            state = PlayerStates.Jumping;
            increasedGravityApplied = false;

            if (onWall)
            {
                Debug.Log(moveInput);
                rb.AddForce(moveInput * wallJumpHorizontalStrength * Vector3.right);
                onWall = false;
                playerAnimator.SetBool("onWall", onWall);
            }

            rb.linearVelocity = currentVelocity;
        }
    }

    bool CheckOnGround()
    {
        onGround = Physics.Raycast(transform.position, Vector3.down, groundRaycastLength, groundLayerMask);

        playerAnimator.SetBool("onGround", onGround);
        return onGround;
    }

    void UpdateGravity(float gravityMultiplier)
    {
        float jumpHalfDuration = jumpTotalDuration / 2;

        float gravity = -2 * jumpHeight / Mathf.Pow(jumpHalfDuration, 2) * gravityMultiplier;
        Physics.gravity = new Vector3(0, gravity, 0);
    }

    void ResetGravity()
    {
        UpdateGravity(1);
    }

    void IncreaseGravity()
    {
        UpdateGravity(landingGravityIncreaseMultiplier);
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
                currentNbOfWallJumps = maxNbOfWallJumps;

                EventBus.Instance.landedOnGround.Invoke();
            }
        }
    }
    #endregion

    #region Wall Jump
    private void OnCollisionEnter(Collision collision)
    {
        GrapplingHookStates ghstate = transform.GetComponent<GrapplingHook>().state;

        if (unlockedWallJump && currentNbOfWallJumps > 0 && collision.gameObject.CompareTag("Wall") && !onGround && ghstate != GrapplingHookStates.Attached)
        {
            Debug.Log("AttachToWall");
            currentVelocity = Vector3.zero;
            rb.linearVelocity = currentVelocity;
            UpdateGravity(0);
            onWall = true;
            currentNbOfWallJumps--;
            if (collision.transform.position.x > transform.position.x)
            {
                newScale = transform.localScale;
                newScale.z = 1;
                body.transform.localScale = newScale;
            }
            else
            {
                newScale = transform.localScale;
                newScale.z = -1;
                body.transform.localScale = newScale;
            }

            playerAnimator.SetBool("onWall", onWall);
        }

        if (collision.gameObject.CompareTag("Ground"))
        {
            state = PlayerStates.OnGround;
            coyoteTimeCounter = coyoteTime;
            currentNbOfWallJumps = maxNbOfWallJumps;
            playerAnimator.SetBool("jumping", false);

            EventBus.Instance.landedOnGround.Invoke();
        }
    }
    #endregion
}
