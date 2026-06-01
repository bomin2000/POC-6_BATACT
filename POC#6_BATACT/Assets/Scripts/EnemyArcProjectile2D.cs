using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyArcProjectile2D : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockbackForce = 15f;
    [SerializeField] private float lifetime = 5f;
    
    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    
    [Header("Collisions")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private string playerTag = "Player";

    private Rigidbody2D body;
    private bool isInitialized = false;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        
        if (groundLayer.value == 0)
        {
            groundLayer = 1 << LayerMask.NameToLayer("Ground");
        }
        if (playerLayer.value == 0)
        {
            playerLayer = 1 << LayerMask.NameToLayer("Player");
        }
    }

    public void Initialize(Vector2 initialVelocity)
    {
        body.linearVelocity = initialVelocity;
        isInitialized = true;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (isInitialized && visualRoot != null && body.linearVelocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(body.linearVelocity.y, body.linearVelocity.x) * Mathf.Rad2Deg;
            visualRoot.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if it's Player
        bool isPlayer = ((1 << collision.gameObject.layer) & playerLayer) != 0 || collision.CompareTag(playerTag);
        
        if (isPlayer)
        {
            PlayerHealth playerHealth = collision.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Knockback direction is derived from velocity
                Vector2 knockbackDir = body.linearVelocity.normalized;
                if (knockbackDir == Vector2.zero) knockbackDir = Vector2.right; // fallback
                
                Vector2 knockback = knockbackDir * knockbackForce;
                playerHealth.TryTakeDamage(damage, knockback, gameObject);
            }
            
            Destroy(gameObject);
        }
        // Check if it's Ground
        else if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            // Spawn splash effect if needed here
            Destroy(gameObject);
        }
    }
}
