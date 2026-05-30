using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ScissorsTerrainGripAssist : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponTerrainProbe2D terrainProbe;

    [Header("Grip Settings")]
    [SerializeField] private bool isActive = true;
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
    
    [Tooltip("How long the grip (braking) effect lasts in seconds.")]
    [SerializeField] private float gripDuration = 0.15f;
    
    [Tooltip("How strongly the velocity is pulled to zero per physics frame during the grip. (0.5 = halves every frame, near instant stop)")]
    [Range(0f, 1f)]
    [SerializeField] private float gripVelocityDamping = 0.5f;
    
    [Tooltip("Cooldown before the grip can trigger again.")]
    [SerializeField] private float gripCooldown = 0.4f;

    [Header("Debugging")]
    [SerializeField] private bool enableDebugLogging = false;

    [Header("Grip Status (Read-Only)")]
    [SerializeField] private bool debugCanGrip;
    [SerializeField] private bool debugIsGripping;
    [SerializeField] private Collider2D debugLastGripCollider;
    [SerializeField] private float debugLastGripTime = -999f;
    [SerializeField] private string debugGripBlockReason;

    private Rigidbody2D body;
    private int groundLayer;
    private float currentGripTimer;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        groundLayer = LayerMask.NameToLayer("Ground");

        if (terrainProbe == null)
        {
            terrainProbe = GetComponentInChildren<WeaponTerrainProbe2D>();
        }
    }

    private void Update()
    {
        if (currentGripTimer > 0f)
        {
            currentGripTimer -= Time.deltaTime;
            if (currentGripTimer <= 0f)
            {
                debugIsGripping = false;
            }
        }

        if (Input.GetKeyDown(attackKey))
        {
            TryGrip();
        }
    }

    private void FixedUpdate()
    {
        UpdateDebugStatus();

        if (debugIsGripping)
        {
            // Apply heavy damping to simulate "biting" the terrain and halting momentum
            body.linearVelocity = Vector2.Lerp(body.linearVelocity, Vector2.zero, gripVelocityDamping);
        }
    }

    private void TryGrip()
    {
        debugCanGrip = false;
        debugGripBlockReason = "None";

        if (!isActive)
        {
            SetBlockReason("Assist Disabled");
            return;
        }

        if (Time.time < debugLastGripTime + gripCooldown)
        {
            SetBlockReason("Cooldown");
            return;
        }

        if (terrainProbe == null || body == null)
        {
            SetBlockReason("Missing References");
            return;
        }

        if (terrainProbe.ActiveState != WeaponState.Scissors)
        {
            SetBlockReason("Not Scissors");
            return;
        }

        if (!terrainProbe.IsContact)
        {
            SetBlockReason("No Contact");
            return;
        }

        Collider2D hitCol = terrainProbe.ContactCollider;
        if (hitCol == null || hitCol.gameObject.layer != groundLayer)
        {
            SetBlockReason("Contact Not Ground");
            return;
        }

        // All checks passed, trigger the Grip!
        debugCanGrip = true;
        debugIsGripping = true;
        debugLastGripTime = Time.time;
        debugLastGripCollider = hitCol;
        currentGripTimer = gripDuration;

        if (enableDebugLogging)
        {
            Debug.Log($"[ScissorsGrip] Terrain Grip Activated against '{hitCol.name}'!");
        }
    }

    private void UpdateDebugStatus()
    {
        if (!isActive) { debugGripBlockReason = "Assist Disabled"; return; }
        if (terrainProbe == null) { debugGripBlockReason = "Probe Missing"; return; }
        if (terrainProbe.ActiveState != WeaponState.Scissors) { debugGripBlockReason = "Not Scissors"; return; }
        if (!terrainProbe.IsContact) { debugGripBlockReason = "No Contact"; return; }
        if (terrainProbe.ContactCollider == null || terrainProbe.ContactCollider.gameObject.layer != groundLayer) 
        { 
            debugGripBlockReason = "Contact Not Ground"; 
            return; 
        }
        
        if (Time.time < debugLastGripTime + gripCooldown)
        {
            debugGripBlockReason = "Cooldown";
            return;
        }

        debugGripBlockReason = "None (Ready)";
        debugCanGrip = true;
    }

    private void SetBlockReason(string reason)
    {
        debugGripBlockReason = reason;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && debugIsGripping && terrainProbe != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(terrainProbe.ContactPoint, 0.3f);
            
            // Draw teeth-like markers
            Gizmos.DrawLine(terrainProbe.ContactPoint + new Vector2(-0.2f, 0.2f), terrainProbe.ContactPoint);
            Gizmos.DrawLine(terrainProbe.ContactPoint + new Vector2(0.2f, 0.2f), terrainProbe.ContactPoint);
            Gizmos.DrawLine(terrainProbe.ContactPoint + new Vector2(-0.2f, -0.2f), terrainProbe.ContactPoint);
            Gizmos.DrawLine(terrainProbe.ContactPoint + new Vector2(0.2f, -0.2f), terrainProbe.ContactPoint);
        }
    }
}
