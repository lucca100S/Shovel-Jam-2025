using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class GrapplingHook : MonoBehaviour
{
    public bool grapplingHookEnabled = false;

    [Header("Shooting Settings")]
    [SerializeField] float maxDistance = 8;
    [SerializeField] LayerMask canAttatchTo;
    public int maxNbOfRopes = 1;
    [SerializeField] int currentNbOfRopes;

    [Header("Grappling Hook Parts")]
    [SerializeField] Transform gun;
    [SerializeField] Transform gunShootingPoint;
    [SerializeField] Transform gunEnd;
    [SerializeField] GameObject gunTip;

    [Header("Rope Settings")]
    [SerializeField] JointParameters jointParameters;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] float retrieveTime;

    InputActionAsset inputActions;
    InputAction shootAction;
    InputAction retrieveAction;

    Vector3 gunTipStartLocalPos;
    GrapplingHookStates state = GrapplingHookStates.Ready;
    Vector3 lookAtPos;
    SpringJoint joint;
    Rigidbody gunTipRb;

    private void Awake()
    {
        inputActions = GetComponent<PlayerController>().inputActions;

        gunTipRb = gunTip.GetComponent<Rigidbody>();

        currentNbOfRopes = maxNbOfRopes;
        lineRenderer.positionCount = 0;
        gunTipStartLocalPos = gunTip.transform.localPosition;
    }

    private void OnEnable()
    {
        inputActions.FindActionMap("Player").Enable();

        shootAction = inputActions.FindAction("Shoot");
        retrieveAction = inputActions.FindAction("Retrieve");

        shootAction.performed += ShootHook;
        retrieveAction.performed += RetrieveHook;
        EventBus.Instance.landedOnGround += ResetGrapplingHook;
    }

    private void OnDisable()
    {
        inputActions.FindActionMap("Player").Disable();

        shootAction.performed -= ShootHook;
        retrieveAction.performed -= RetrieveHook;
        EventBus.Instance.landedOnGround -= ResetGrapplingHook;
    }

    private void LateUpdate()
    {
        LookAtTarget();

        DrawRope();
    }

    void ShootHook(InputAction.CallbackContext context)
    {
        if (grapplingHookEnabled && state != GrapplingHookStates.Shooting && currentNbOfRopes > 0)
        {
            state = GrapplingHookStates.Shooting;
            lineRenderer.positionCount = 2;
            Vector3 shootingDirection = lookAtPos - gunShootingPoint.position;
            if (Physics.Raycast(gunShootingPoint.position, shootingDirection, out RaycastHit hit, maxDistance, canAttatchTo))
            {
                Debug.Log("Hit");
                currentNbOfRopes--;
                state = GrapplingHookStates.Attached;
                gunTip.transform.position = hit.point;

                joint = gameObject.AddComponent<SpringJoint>();
                
                SetGunTipConstraints();
                gunTip.transform.parent = null;
                joint.connectedBody = gunTipRb;

                joint.autoConfigureConnectedAnchor = false;
                float distanceFromHitPoint = Vector3.Distance(gunEnd.position, gunTip.transform.position);
                LoadJointParameters(distanceFromHitPoint);
            }
            else
            {
                gunTip.transform.position = gunEnd.position + gunEnd.forward * maxDistance;
                Invoke("CallRetrieveHook", retrieveTime);
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
        state = GrapplingHookStates.Released;
        Destroy(joint);
        gunTip.transform.parent = gun;
        gunTip.transform.localPosition = gunTipStartLocalPos;
        gunTip.transform.localEulerAngles = Vector3.zero;
    }

    void ResetGrapplingHook()
    {
        if (state != GrapplingHookStates.Attached)
        {
            state = GrapplingHookStates.Ready;
            currentNbOfRopes = maxNbOfRopes;
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
        }
        gun.LookAt(lookAtPos);
    }

    void SetGunTipConstraints()
    {
        gunTipRb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
    }

    void LoadJointParameters(float distance)
    {
        joint.maxDistance = distance * jointParameters.maxDistanceModifier;
        joint.minDistance = distance * jointParameters.minDistanceModifier;

        joint.spring = jointParameters.spring;
        joint.damper = jointParameters.damper;
        joint.massScale = jointParameters.massScale;
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
