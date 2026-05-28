using UnityEngine;

public sealed class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Side View Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);
    [SerializeField] private float smoothTime = 0.12f;
    [SerializeField] private float lookAheadDistance = 1.8f;
    [SerializeField] private float lookAheadSharpness = 6f;
    [SerializeField] private bool keepCurrentZ = true;

    private Vector3 velocity;
    private float currentLookAhead;
    private float lastTargetX;
    private bool initialized;

    private void Start()
    {
        if (target == null)
        {
            return;
        }

        lastTargetX = target.position.x;
        SnapToTarget();
        initialized = true;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (!initialized)
        {
            lastTargetX = target.position.x;
            initialized = true;
        }

        float deltaX = target.position.x - lastTargetX;
        float desiredLookAhead = Mathf.Abs(deltaX) > 0.0001f ? Mathf.Sign(deltaX) * lookAheadDistance : currentLookAhead;
        float lookAheadT = 1f - Mathf.Exp(-lookAheadSharpness * Time.deltaTime);
        currentLookAhead = Mathf.Lerp(currentLookAhead, desiredLookAhead, lookAheadT);

        Vector3 targetPosition = target.position + offset + Vector3.right * currentLookAhead;
        if (keepCurrentZ)
        {
            targetPosition.z = transform.position.z;
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        lastTargetX = target.position.x;
    }

    public void SetTarget(Transform newTarget, bool snap)
    {
        target = newTarget;
        initialized = false;

        if (snap)
        {
            SnapToTarget();
        }
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position + offset;
        if (keepCurrentZ)
        {
            targetPosition.z = transform.position.z;
        }

        transform.position = targetPosition;
        velocity = Vector3.zero;
    }
}
