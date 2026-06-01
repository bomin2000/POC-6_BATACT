using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRangedShooter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 6.0f;
    [SerializeField] private float retreatDistance = 3.5f;
    [SerializeField] private float acceleration = 25f;

    [Header("Arc Shooting")]
    [SerializeField] private GameObject arcProjectilePrefab;
    [SerializeField] private GameObject warningMarkerPrefab;
    [SerializeField] private float chargeDuration = 1.0f;
    [SerializeField] private float timeOfFlight = 1.5f;
    [SerializeField] private float fireRate = 2.5f;
    [SerializeField] private Transform firePoint;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    private Rigidbody2D body;
    private float fireCooldown;
    private int facingSign = 1;

    private enum State { Moving, Charging }
    private State currentState = State.Moving;
    private float chargeTimer;
    private Vector2 lockedTargetPosition;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null) target = player.transform;
        }
    }

    private void Update()
    {
        if (currentState == State.Moving && fireCooldown > 0f) 
        {
            fireCooldown -= Time.deltaTime;
        }
        
        if (currentState == State.Charging)
        {
            chargeTimer -= Time.deltaTime;
            if (chargeTimer <= 0f)
            {
                FireArcProjectile();
                currentState = State.Moving;
                fireCooldown = fireRate;
            }
        }
    }

    private void FixedUpdate()
    {
        if (target == null || currentState == State.Charging)
        {
            Decelerate();
            
            // Still face target while charging if desired, or keep locked. Let's keep facing locked to avoid moonwalking.
            UpdateVisualFacing();
            return;
        }

        float deltaX = target.position.x - transform.position.x;
        float distanceX = Mathf.Abs(deltaX);
        float distanceTotal = Vector2.Distance(transform.position, target.position);

        if (distanceX > 0.05f)
        {
            facingSign = deltaX > 0f ? 1 : -1;
        }

        UpdateVisualFacing();

        // Movement logic
        float desiredVelocityX = 0f;
        if (distanceX > stopDistance)
        {
            desiredVelocityX = facingSign * moveSpeed;
        }
        else if (distanceX < retreatDistance)
        {
            desiredVelocityX = -facingSign * moveSpeed;
        }

        float nextVelocityX = Mathf.MoveTowards(body.linearVelocity.x, desiredVelocityX, acceleration * Time.fixedDeltaTime);
        body.linearVelocity = new Vector2(nextVelocityX, body.linearVelocity.y);

        // Shooting logic
        if (distanceTotal <= stopDistance + 1f && fireCooldown <= 0f)
        {
            StartCharge();
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Decelerate()
    {
        float nextVelocityX = Mathf.MoveTowards(body.linearVelocity.x, 0f, acceleration * Time.fixedDeltaTime);
        body.linearVelocity = new Vector2(nextVelocityX, body.linearVelocity.y);
    }

    private void StartCharge()
    {
        currentState = State.Charging;
        chargeTimer = chargeDuration;
        lockedTargetPosition = target.position;

        // Ground check for marker (cast down from target)
        Vector2 markerPos = lockedTargetPosition;
        RaycastHit2D hit = Physics2D.Raycast(lockedTargetPosition, Vector2.down, 5f, LayerMask.GetMask("Ground"));
        if (hit.collider != null)
        {
            markerPos = hit.point;
        }
        else
        {
            // If no ground immediately below, just use player feet level roughly
            markerPos.y -= 0.5f;
        }

        lockedTargetPosition = markerPos; // projectile aims for the marker

        if (warningMarkerPrefab != null)
        {
            GameObject marker = Instantiate(warningMarkerPrefab, markerPos, Quaternion.identity);
            ProjectileWarningMarker warn = marker.GetComponent<ProjectileWarningMarker>();
            if (warn != null) warn.ShowWarning(chargeDuration);
        }
    }

    private void FireArcProjectile()
    {
        if (arcProjectilePrefab != null && firePoint != null)
        {
            GameObject proj = Instantiate(arcProjectilePrefab, firePoint.position, Quaternion.identity);
            EnemyArcProjectile2D arcProj = proj.GetComponent<EnemyArcProjectile2D>();
            
            if (arcProj != null)
            {
                Rigidbody2D projBody = proj.GetComponent<Rigidbody2D>();
                float gravity = Physics2D.gravity.y * projBody.gravityScale;
                
                // Calculate required velocity
                Vector2 startPos = firePoint.position;
                float dx = lockedTargetPosition.x - startPos.x;
                float dy = lockedTargetPosition.y - startPos.y;
                
                // timeOfFlight is T
                float vx = dx / timeOfFlight;
                float vy = (dy - 0.5f * gravity * timeOfFlight * timeOfFlight) / timeOfFlight;
                
                arcProj.Initialize(new Vector2(vx, vy));
            }
        }
    }

    private void UpdateVisualFacing()
    {
        if (visualRoot == null) return;
        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * facingSign;
        visualRoot.localScale = scale;
    }
}
