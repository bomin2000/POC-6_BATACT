using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRangedShooter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 5.5f;
    [SerializeField] private float retreatDistance = 2.5f;
    [SerializeField] private float acceleration = 25f;

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 2f;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    private Rigidbody2D body;
    private float fireCooldown;
    private int facingSign = 1;

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
        if (fireCooldown > 0f) fireCooldown -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            Decelerate();
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
            Shoot();
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

    private void Shoot()
    {
        fireCooldown = fireRate;
        if (projectilePrefab != null && firePoint != null)
        {
            Vector2 direction = ((Vector2)target.position - (Vector2)firePoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(0f, 0f, angle));
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
