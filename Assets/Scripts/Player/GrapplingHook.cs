using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(PlayerController))]
public class GrapplingHook : MonoBehaviour
{
    public bool grapplingHookEnabled = false;

    [Header("Shooting Settings")]
    [SerializeField] float maxDistance = 8;
    [SerializeField] float shootingSpeed = 40;
    [SerializeField] float retrieveSpeed = 40;
    [SerializeField] float retrieveDelayTime;
    [SerializeField] LayerMask canAttatchTo;
    public int maxNbOfRopes = 1;
    public int currentNbOfRopes;

    [Header("Grappling Hook Parts")]
    [SerializeField] Transform gun;
    [SerializeField] Transform gunShootingPoint;
    [SerializeField] Transform gunEnd;
    [SerializeField] GameObject gunTip;

    [Header("Rope Settings")]
    [SerializeField] JointParameters jointParameters;
    [SerializeField] LineRenderer lineRenderer;

    [Header("Rapel Settings")]
    [SerializeField] bool rapelEnabled = false;
    [SerializeField] float shortenSpeed = 100;
    [SerializeField] float extendSpeed = 200;
    [SerializeField] float minRopeLength = 2;
    [SerializeField] float maxRopeLength = 11;
    

    InputActionAsset inputActions;
    InputAction shootAction;
    InputAction retrieveAction;
    InputAction rapelAction;

    Vector3 gunTipStartLocalPos;
    Vector3 gunTipLastPosition;
    GrapplingHookStates state = GrapplingHookStates.Ready;
    Vector3 lookAtPos;
    SpringJoint joint;
    Rigidbody gunTipRb;

    float rapelInput;
    float initialRopeLength;

    Animator playerAnimator;

    private void Awake()
    {
        inputActions = GetComponent<PlayerController>().inputActions;

        gunTipRb = gunTip.GetComponent<Rigidbody>();

        currentNbOfRopes = maxNbOfRopes;
        lineRenderer.positionCount = 0;
        gunTipStartLocalPos = gunTip.transform.localPosition;

        playerAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        shootAction = inputActions.FindAction("Shoot");
        retrieveAction = inputActions.FindAction("Retrieve");
        rapelAction = inputActions.FindAction("Rapel");

        shootAction.performed += ShootHook;
        retrieveAction.performed += RetrieveHook;
        EventBus.Instance.landedOnGround += ResetGrapplingHook;
    }

    private void OnDisable()
    {
        shootAction.performed -= ShootHook;
        retrieveAction.performed -= RetrieveHook;
        EventBus.Instance.landedOnGround -= ResetGrapplingHook;
    }

    private void LateUpdate()
    {
        LookAtTarget();

        DrawRope();

        Rapel();
    }

    #region Shooting
    void ShootHook(InputAction.CallbackContext context)
    {
        if (grapplingHookEnabled && (state != GrapplingHookStates.Shooting && state != GrapplingHookStates.Attached) && currentNbOfRopes > 0)
        {
            float shootingDuration;
            state = GrapplingHookStates.Shooting;
            lineRenderer.positionCount = 2;
            Vector3 shootingDirection = lookAtPos - gunShootingPoint.position;
            if (Physics.Raycast(gunShootingPoint.position, shootingDirection, out RaycastHit hit, maxDistance, canAttatchTo))
            {
                playerAnimator.SetBool("swinging", true);
                shootingDuration = Vector3.Distance(gunTip.transform.position, hit.point) / shootingSpeed;
                Debug.Log("Hit");
                currentNbOfRopes--;
                gunTip.transform.DOMove(hit.point, shootingDuration).OnComplete(() => SetUpGrapplingHook()).SetEase(Ease.Linear);
            }
            else
            {
                shootingDuration = maxDistance / shootingSpeed;
                gunTip.transform.DOMove(gunEnd.position + gunEnd.forward * maxDistance, shootingDuration).OnComplete(() => Invoke("CallRetrieveHook", retrieveDelayTime)).SetEase(Ease.OutCubic);
                Debug.Log("Not hit");
            }
        }
    }

    void CallRetrieveHook()
    {
        InputAction.CallbackContext context = new InputAction.CallbackContext();
        RetrieveHook(context);
    }

    void RetrieveHook(InputAction.CallbackContext context)
    {
        if (state == GrapplingHookStates.Attached)
        {
            bool onGround = EventBus.Instance.checkOnGround.Invoke();
            if (onGround)
            {
                state = GrapplingHookStates.Ready;
                currentNbOfRopes = maxNbOfRopes;
            }
        }

        playerAnimator.SetBool("swinging", false);
        state = GrapplingHookStates.Retrieving;
        EventBus.Instance.hookReleased.Invoke();
        Destroy(joint);

        gunTipLastPosition = gunTip.transform.position;
        gunTip.transform.parent = gun;
        float retrievingDuration = Vector3.Distance(gunTipStartLocalPos, gun.transform.localPosition) / retrieveSpeed;
        gunTip.transform.DOLocalMove(gunTipStartLocalPos, retrievingDuration).SetEase(Ease.InCubic).OnComplete(() =>
        {
            gunTip.transform.localEulerAngles = Vector3.zero;
            state = GrapplingHookStates.Released;
        });
    }

    void SetUpGrapplingHook()
    {
        joint = gameObject.AddComponent<SpringJoint>();

        SetGunTipConstraints();
        gunTip.transform.parent = null;
        joint.connectedBody = gunTipRb;

        joint.autoConfigureConnectedAnchor = false;
        initialRopeLength = Vector3.Distance(gunEnd.position, gunTip.transform.position);
        LoadJointParameters(initialRopeLength);

        state = GrapplingHookStates.Attached;
        EventBus.Instance.hookAttached.Invoke();
    }

    void ResetGrapplingHook()
    {
        if (state != GrapplingHookStates.Attached)
        {
            state = GrapplingHookStates.Ready;
            currentNbOfRopes = maxNbOfRopes;
        }
    }

    void SetGunTipConstraints()
    {
        gunTipRb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
    }

    void LoadJointParameters(float distance)
    {
        joint.maxDistance = distance + jointParameters.maxDistanceModifier;
        joint.minDistance = distance + jointParameters.minDistanceModifier;

        joint.spring = jointParameters.spring;
        joint.damper = jointParameters.damper;
        joint.massScale = jointParameters.massScale;
    }
    #endregion

    void Rapel()
    {
        // Work in progress

        if (rapelEnabled && state == GrapplingHookStates.Attached)
        {
            rapelInput = rapelAction.ReadValue<float>();

            if (rapelInput != 0)
            {
                float rapelSpeed = rapelInput == 1? shortenSpeed : extendSpeed;
                float rapelDistance = Mathf.Abs(Vector3.Distance(gunEnd.position, gunTip.transform.position)) - rapelInput * rapelSpeed * Time.deltaTime;

                if (minRopeLength < rapelDistance && rapelDistance < maxRopeLength)
                {
                    joint.maxDistance = rapelDistance + jointParameters.maxDistanceModifier;
                    joint.minDistance = rapelDistance + jointParameters.minDistanceModifier;
                }
            }
        }
    }

    void LookAtTarget()
    {
        switch (state)
        {
            case GrapplingHookStates.Ready:
            case GrapplingHookStates.Released:
                lookAtPos = CursorTracker.Instance.cursorPos;
                lookAtPos.z = lookAtPos.z - (Camera.main.transform.position.z);
                lookAtPos = Camera.main.ScreenToWorldPoint(lookAtPos);
                break;
            case GrapplingHookStates.Attached:
                lookAtPos = gunTip.transform.position;
                break;
            case GrapplingHookStates.Retrieving:
                lookAtPos = gunTipLastPosition;
                break;
        }
        gun.LookAt(lookAtPos);
    }

    void DrawRope()
    {
        if (lineRenderer.positionCount == 2)
        {
            lineRenderer.SetPosition(0, gunEnd.position);
            lineRenderer.SetPosition(1, gunTip.transform.position);
        }
    }
}
