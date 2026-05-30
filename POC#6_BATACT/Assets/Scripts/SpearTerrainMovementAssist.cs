using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SpearTerrainMovementAssist : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponTerrainProbe2D terrainProbe;
    [SerializeField] private PlayerTopDownMovement playerMovement;
    [SerializeField] private WeaponEscapeSpaceChecker2D spaceChecker;
    
    [Header("Resistance Settings")]
    [SerializeField] private bool isActive = true;
    [Tooltip("How much to reduce velocity pushing into a wall (0 = none, 1 = total stop)")]
    [Range(0f, 1f)]
    [SerializeField] private float wallVelocityDamping = 0.85f;
    [Tooltip("How much to reduce falling velocity when touching the floor (0 = none, 1 = total stop)")]
    [Range(0f, 1f)]
    [SerializeField] private float fallVelocityDamping = 0.6f;

    [Header("Vault Settings")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private float floorVaultUpImpulse = 18f;
    [SerializeField] private float floorVaultSideImpulse = 12f;
    [SerializeField] private float wallVaultUpImpulse = 14f;
    [SerializeField] private float wallVaultSideImpulse = 15f;
    [SerializeField] private float vaultCooldown = 0.4f;
    [SerializeField] private float vaultControlLockSeconds = 0.15f;

    [Header("Threshold Settings")]
    [SerializeField] private float minimumNormalXForWall = 0.5f;
    [SerializeField] private float minimumNormalYForFloor = 0.5f;

    [Header("Debugging")]
    [SerializeField] private bool enableDebugLogging = false;

    [Header("Assist Status (Read-Only)")]
    [SerializeField] private bool debugHasProbe;
    [SerializeField] private bool debugHasRigidbody;
    [SerializeField] private WeaponState debugActiveState;
    [SerializeField] private bool debugProbeIsContact;
    [SerializeField] private Vector2 debugContactNormal;
    [SerializeField] private bool debugCorrectionApplied;
    [SerializeField] private string debugBlockReason;
    [SerializeField] private Vector2 debugVelocityBefore;
    [SerializeField] private Vector2 debugVelocityAfter;

    [Header("Vault Status (Read-Only)")]
    [SerializeField] private bool debugCanVault;
    [SerializeField] private float debugLastVaultTime;
    [SerializeField] private Vector2 debugLastVaultDirection;
    [SerializeField] private Vector2 debugLastVaultImpulse;
    [SerializeField] private string debugVaultBlockReason;

    public bool IsActivelyAssisting => debugCorrectionApplied;
    public float LastVaultTime => debugLastVaultTime;

    private Rigidbody2D body;
    private int groundLayer;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        groundLayer = LayerMask.NameToLayer("Ground");

        if (terrainProbe == null) terrainProbe = GetComponentInChildren<WeaponTerrainProbe2D>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerTopDownMovement>();
        if (spaceChecker == null) spaceChecker = GetComponent<WeaponEscapeSpaceChecker2D>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(jumpKey))
        {
            TryVault();
        }
    }

    private void FixedUpdate()
    {
        UpdateAssistStatus();
    }

    private void TryVault()
    {
        debugCanVault = false;
        debugVaultBlockReason = "None";

        if (!isActive)
        {
            debugVaultBlockReason = "Assist Disabled";
            return;
        }

        if (Time.time < debugLastVaultTime + vaultCooldown)
        {
            debugVaultBlockReason = "Cooldown";
            return;
        }

        if (terrainProbe == null || playerMovement == null || body == null)
        {
            debugVaultBlockReason = "Missing References";
            return;
        }

        if (terrainProbe.ActiveState != WeaponState.Spear)
        {
            debugVaultBlockReason = "Not Spear";
            return;
        }

        if (!terrainProbe.IsContact)
        {
            debugVaultBlockReason = "No Contact";
            return;
        }

        Collider2D hitCol = terrainProbe.ContactCollider;
        if (hitCol == null || hitCol.gameObject.layer != groundLayer)
        {
            debugVaultBlockReason = "Contact Not Ground";
            return;
        }

        Vector2 normal = terrainProbe.ContactNormal;
        Vector2 facingDir = new Vector2(playerMovement.FacingSign, 0f);

        if (spaceChecker != null && !spaceChecker.TryFindSafeEscapeDirection(normal, facingDir, out Vector2 _))
        {
            debugVaultBlockReason = "Blocked / No Escape Space";
            return;
        }

        // Vault is valid
        debugCanVault = true;
        debugLastVaultTime = Time.time;
        Vector2 impulse = Vector2.zero;
        int facingSign = playerMovement.FacingSign;

        // Determine if we are pushing off a Floor or a Wall
        if (normal.y >= minimumNormalYForFloor)
        {
            // Floor Vault: Vault UP and FORWARD
            debugLastVaultDirection = new Vector2(facingSign, 1f).normalized;
            impulse = new Vector2(facingSign * floorVaultSideImpulse, floorVaultUpImpulse);
        }
        else if (Mathf.Abs(normal.x) >= minimumNormalXForWall)
        {
            // Wall Vault: Vault UP and AWAY from the wall
            float awayFromWallSign = Mathf.Sign(normal.x); // Normal points away from the wall
            debugLastVaultDirection = new Vector2(awayFromWallSign, 1f).normalized;
            impulse = new Vector2(awayFromWallSign * wallVaultSideImpulse, wallVaultUpImpulse);
        }
        else
        {
            // Fallback for weird angles: treat as a weaker wall vault
            float awayFromWallSign = Mathf.Sign(normal.x);
            debugLastVaultDirection = new Vector2(awayFromWallSign, 1f).normalized;
            impulse = new Vector2(awayFromWallSign * wallVaultSideImpulse * 0.8f, wallVaultUpImpulse * 0.8f);
        }

        debugLastVaultImpulse = impulse;
        
        // This locks regular movement controls to prevent the default jump from firing
        // and instantly applies our vault impulse.
        playerMovement.ApplyExternalKnockback(impulse, vaultControlLockSeconds);

        if (enableDebugLogging)
        {
            Debug.Log($"[SpearVault] Vault Executed! Normal: {normal}, Impulse: {impulse}");
        }
    }

    private void UpdateAssistStatus()
    {
        debugHasProbe = terrainProbe != null;
        debugHasRigidbody = body != null;
        debugActiveState = debugHasProbe ? terrainProbe.ActiveState : WeaponState.BareHand;
        debugProbeIsContact = debugHasProbe && terrainProbe.IsContact;
        debugCorrectionApplied = false;
        debugBlockReason = "None";
        debugVelocityBefore = debugHasRigidbody ? body.linearVelocity : Vector2.zero;
        debugVelocityAfter = debugVelocityBefore;

        if (terrainProbe != null && terrainProbe.ContactCollider != null)
        {
            debugContactNormal = terrainProbe.ContactNormal;
        }
        else
        {
            debugContactNormal = Vector2.zero;
        }

        if (!isActive) { SetBlockReason("Correction Disabled"); return; }
        if (terrainProbe == null) { SetBlockReason("Probe Missing"); return; }
        if (body == null) { SetBlockReason("Rigidbody Missing"); return; }
        if (terrainProbe.ActiveState != WeaponState.Spear) { SetBlockReason("Not Spear"); return; }
        if (!terrainProbe.IsContact) { SetBlockReason("No Contact"); return; }
        
        Collider2D hitCol = terrainProbe.ContactCollider;
        if (hitCol == null || hitCol.gameObject.layer != groundLayer) { SetBlockReason("Contact Not Ground"); return; }

        Vector2 normal = terrainProbe.ContactNormal;
        Vector2 pushDirection = -normal;
        Vector2 currentVelocity = body.linearVelocity;
        float velocityIntoTerrain = Vector2.Dot(currentVelocity, pushDirection);

        if (velocityIntoTerrain <= 0.01f) { SetBlockReason("No Velocity Toward Contact"); return; }

        Vector2 facingDir = new Vector2(playerMovement.FacingSign, 0f);
        if (spaceChecker != null && !spaceChecker.TryFindSafeEscapeDirection(normal, facingDir, out Vector2 _))
        {
            SetBlockReason("Blocked / No Escape Space");
            return;
        }

        debugBlockReason = "None";
        debugCorrectionApplied = true;
        
        Vector2 correction = Vector2.zero;
        if (normal.y >= minimumNormalYForFloor)
        {
            float dampening = velocityIntoTerrain * fallVelocityDamping;
            correction = -pushDirection * dampening;
        }
        else if (Mathf.Abs(normal.x) >= minimumNormalXForWall)
        {
            float dampening = velocityIntoTerrain * wallVelocityDamping;
            correction = -pushDirection * dampening;
        }
        else
        {
            float dampening = velocityIntoTerrain * wallVelocityDamping;
            correction = -pushDirection * dampening;
        }

        body.linearVelocity += correction;
        debugVelocityAfter = body.linearVelocity;
    }

    private void SetBlockReason(string reason)
    {
        debugBlockReason = reason;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || terrainProbe == null) return;

        if (debugCorrectionApplied)
        {
            Gizmos.color = Color.cyan;
            Vector3 startPos = terrainProbe.ContactPoint;
            Vector3 diff = (Vector3)(debugVelocityAfter - debugVelocityBefore);
            Gizmos.DrawLine(startPos, startPos + diff);
            Gizmos.DrawSphere(startPos + diff, 0.05f);
        }

        // Briefly show vault direction
        if (Time.time < debugLastVaultTime + 0.2f)
        {
            Gizmos.color = Color.magenta;
            Vector3 startPos = transform.position;
            Vector3 vaultVec = (Vector3)debugLastVaultImpulse * 0.1f;
            Gizmos.DrawLine(startPos, startPos + vaultVec);
            Gizmos.DrawWireSphere(startPos + vaultVec, 0.2f);
        }
    }
}
