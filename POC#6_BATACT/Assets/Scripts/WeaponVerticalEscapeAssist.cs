using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WeaponVerticalEscapeAssist : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponTerrainProbe2D terrainProbe;
    [SerializeField] private SpearTerrainMovementAssist spearAssist;
    [SerializeField] private ScissorsTerrainGripAssist scissorsAssist;
    [SerializeField] private WeaponTerrainContactLimiter2D contactLimiter;
    [SerializeField] private PlayerTopDownMovement playerMovement;
    [SerializeField] private WeaponEscapeSpaceChecker2D spaceChecker;

    [Header("Escape Settings")]
    [SerializeField] private bool enableVerticalEscape = true;
    [SerializeField] private float verticalEscapeSpeed = 8f;
    [SerializeField] private float verticalEscapeAcceleration = 30f;
    [SerializeField] private float verticalEscapeMaxDuration = 0.5f;
    [SerializeField] private float verticalEscapeCooldown = 0.2f;

    [Header("Jitter Detection")]
    [Tooltip("How many consecutive frames of continuous contact or assist before escape triggers.")]
    [SerializeField] private int jitterFrameThreshold = 12;
    [Tooltip("If velocity falls below this magnitude while trying to push, it's considered stuck/jittering.")]
    [SerializeField] private float jitterVelocityThreshold = 0.5f;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private bool debugIsEscaping;
    [SerializeField] private int debugConsecutiveFrames;
    [SerializeField] private float debugAccumulatedContactTime;
    [SerializeField] private Vector2 debugLastEscapeDirection;
    [SerializeField] private float debugLastEscapeSpeed;
    [SerializeField] private string debugBlockReason;

    private Rigidbody2D body;
    private Collider2D lastContactCollider;
    private float escapeTimer;
    private float lastEscapeTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (terrainProbe == null) terrainProbe = GetComponentInChildren<WeaponTerrainProbe2D>();
        if (spearAssist == null) spearAssist = GetComponent<SpearTerrainMovementAssist>();
        if (scissorsAssist == null) scissorsAssist = GetComponent<ScissorsTerrainGripAssist>();
        if (contactLimiter == null) contactLimiter = GetComponent<WeaponTerrainContactLimiter2D>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerTopDownMovement>();
        if (spaceChecker == null) spaceChecker = GetComponent<WeaponEscapeSpaceChecker2D>();
    }

    private void FixedUpdate()
    {
        if (!enableVerticalEscape)
        {
            ResetState("Disabled");
            return;
        }

        if (terrainProbe == null)
        {
            ResetState("Missing Probe");
            return;
        }

        // 1. Vault Override: If Vault was just executed, disable escape temporarily to avoid conflict
        if (spearAssist != null && Time.time < spearAssist.LastVaultTime + 0.3f)
        {
            ResetState("Vault Active");
            return;
        }

        // 2. Cooldown check
        if (Time.time < lastEscapeTime + verticalEscapeCooldown && !debugIsEscaping)
        {
            ResetState("Cooldown");
            return;
        }

        // 3. Contact Tracking
        if (terrainProbe.IsContact)
        {
            if (lastContactCollider != terrainProbe.ContactCollider)
            {
                lastContactCollider = terrainProbe.ContactCollider;
                debugAccumulatedContactTime = 0f;
                debugConsecutiveFrames = 0;
            }
            
            debugAccumulatedContactTime += Time.fixedDeltaTime;
        }
        else
        {
            ResetState("No Contact");
            return;
        }

        // 4. Jitter / Assist Evaluation
        bool isActivelyAssisting = false;
        
        if (spearAssist != null && spearAssist.IsActivelyAssisting) isActivelyAssisting = true;
        if (scissorsAssist != null && scissorsAssist.IsGripping) isActivelyAssisting = true;
        if (contactLimiter != null && (contactLimiter.IsActivelyLimitingAim || contactLimiter.IsActivelyLimitingPivot)) isActivelyAssisting = true;

        bool isVelocityLow = body.linearVelocity.magnitude <= jitterVelocityThreshold;

        if (isActivelyAssisting && isVelocityLow)
        {
            debugConsecutiveFrames++;
        }
        else if (debugConsecutiveFrames > 0 && !debugIsEscaping)
        {
            // Only decay if not escaping so we don't snap out immediately
            debugConsecutiveFrames--; 
        }

        // 5. Trigger Escape
        if (!debugIsEscaping && debugConsecutiveFrames >= jitterFrameThreshold)
        {
            if (spaceChecker != null)
            {
                Vector2 normal = terrainProbe.ContactNormal;
                Vector2 facingDir = playerMovement != null ? new Vector2(playerMovement.FacingSign, 0f) : Vector2.right;
                if (spaceChecker.TryFindSafeEscapeDirection(normal, facingDir, out Vector2 safeDir))
                {
                    debugIsEscaping = true;
                    escapeTimer = verticalEscapeMaxDuration;
                    debugLastEscapeDirection = safeDir; // Store for ExecuteEscape
                }
                else
                {
                    ResetState("Blocked / No Escape Space");
                    return; // Abort
                }
            }
            else
            {
                debugIsEscaping = true;
                escapeTimer = verticalEscapeMaxDuration;
                debugLastEscapeDirection = Vector2.up;
            }
        }

        // 6. Execute Escape
        if (debugIsEscaping)
        {
            ExecuteEscape();
            
            escapeTimer -= Time.fixedDeltaTime;
            if (escapeTimer <= 0f)
            {
                EndEscape();
            }
        }
    }

    private void ExecuteEscape()
    {
        debugBlockReason = "Escaping";
        Vector2 vel = body.linearVelocity;

        // Apply escape acceleration along the chosen safe direction
        Vector2 targetEscapeVel = debugLastEscapeDirection * verticalEscapeSpeed;
        
        // If the safe direction has an upward component, strongly kill downward velocity
        if (debugLastEscapeDirection.y > 0.1f && vel.y < 0f)
        {
            vel.y = Mathf.Lerp(vel.y, 0f, 0.8f);
        }
        
        // Smoothly accelerate towards the target escape velocity
        vel = Vector2.MoveTowards(vel, targetEscapeVel, verticalEscapeAcceleration * Time.fixedDeltaTime);

        body.linearVelocity = vel;

        debugLastEscapeSpeed = vel.magnitude;
    }

    private void EndEscape()
    {
        debugIsEscaping = false;
        debugConsecutiveFrames = 0;
        lastEscapeTime = Time.time;
        debugBlockReason = "Finished Escape";
    }

    private void ResetState(string reason)
    {
        if (debugIsEscaping)
        {
            EndEscape();
        }
        
        debugConsecutiveFrames = 0;
        debugAccumulatedContactTime = 0f;
        lastContactCollider = null;
        debugBlockReason = reason;
    }
}
