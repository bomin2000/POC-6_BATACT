using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile2D : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float knockbackForce = 15f;
    
    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    
    [Header("Collisions")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private string playerTag = "Player";

    [Header("Debug Info (Read-Only)")]
    [SerializeField] private string debugLastHitObject;
    [SerializeField] private string debugLastHitLayer;
    [SerializeField] private bool debugDidDamage;
    [SerializeField] private string debugDestroyReason;

    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f; // Keep dynamic so it can trigger against Static Ground
        
        if (groundLayer.value == 0)
        {
            groundLayer = 1 << LayerMask.NameToLayer("Ground");
        }
        if (playerLayer.value == 0)
        {
            playerLayer = 1 << LayerMask.NameToLayer("Player");
        }
    }

    private void Start()
    {
        body.linearVelocity = transform.right * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        debugLastHitObject = collision.gameObject.name;
        debugLastHitLayer = LayerMask.LayerToName(collision.gameObject.layer);

        // Check if it's Player (either by Layer or by Tag)
        bool isPlayer = ((1 << collision.gameObject.layer) & playerLayer) != 0 || collision.CompareTag(playerTag);
        
        if (isPlayer)
        {
            PlayerHealth playerHealth = collision.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector2 knockback = transform.right * knockbackForce;
                bool applied = playerHealth.TryTakeDamage(damage, knockback, gameObject);
                if (applied)
                {
                    debugDidDamage = true;
                }
            }
            
            debugDestroyReason = "Hit Player";
            Destroy(gameObject);
        }
        // Check if it's Ground
        else if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            debugDestroyReason = "Hit Ground";
            Destroy(gameObject);
        }
    }
}
