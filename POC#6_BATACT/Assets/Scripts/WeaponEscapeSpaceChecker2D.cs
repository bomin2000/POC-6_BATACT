using UnityEngine;

public class WeaponEscapeSpaceChecker2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CapsuleCollider2D playerCollider;

    [Header("Settings")]
    [Tooltip("How far to check in the candidate directions for safe space.")]
    [SerializeField] private float escapeCheckDistance = 0.2f;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private bool debugHasEscapeSpace;
    [SerializeField] private Vector2 debugSelectedEscapeDirection;
    [SerializeField] private string debugLastBlockedReason = "None";
    [SerializeField] private string debugLastOverlapColliderName = "None";
    
    // Storing check history for Gizmos
    private Vector2 lastCheckPos;
    private Vector2 lastSafeDirection;
    private Vector2[] lastCheckedDirections;

    private int groundLayerMask;

    private void Awake()
    {
        if (playerCollider == null)
        {
            playerCollider = GetComponent<CapsuleCollider2D>();
        }
        groundLayerMask = 1 << LayerMask.NameToLayer("Ground");
    }

    /// <summary>
    /// Checks a series of candidate directions to find one where the player's capsule can safely fit.
    /// </summary>
    public bool TryFindSafeEscapeDirection(Vector2 contactNormal, Vector2 playerFacingDir, out Vector2 safeDirection)
    {
        if (playerCollider == null)
        {
            debugLastBlockedReason = "Missing Collider";
            safeDirection = Vector2.zero;
            return false;
        }

        Vector2 origin = (Vector2)transform.position + playerCollider.offset;
        lastCheckPos = origin;
        
        // Define candidate directions based on the prompt priority
        Vector2[] candidateDirections = new Vector2[]
        {
            Vector2.up,
            contactNormal.normalized,
            (contactNormal + Vector2.up).normalized,
            -playerFacingDir.normalized
        };

        lastCheckedDirections = candidateDirections;

        foreach (Vector2 dir in candidateDirections)
        {
            if (dir == Vector2.zero) continue;

            Vector2 checkPos = origin + dir.normalized * escapeCheckDistance;
            
            // OverlapCapsule parameters match the CapsuleCollider2D
            Collider2D overlap = Physics2D.OverlapCapsule(
                checkPos, 
                playerCollider.size, 
                playerCollider.direction, 
                0f, 
                groundLayerMask
            );

            if (overlap == null || IsIgnoredCollider(overlap))
            {
                // We found a safe direction!
                debugHasEscapeSpace = true;
                debugSelectedEscapeDirection = dir.normalized;
                debugLastBlockedReason = "None";
                debugLastOverlapColliderName = "None";
                safeDirection = dir.normalized;
                lastSafeDirection = safeDirection;
                return true;
            }
            else
            {
                debugLastOverlapColliderName = overlap.name;
            }
        }

        // None of the candidate directions were safe
        debugHasEscapeSpace = false;
        debugSelectedEscapeDirection = Vector2.zero;
        debugLastBlockedReason = "Blocked / No Escape Space";
        safeDirection = Vector2.zero;
        lastSafeDirection = Vector2.zero;
        return false;
    }

    private bool IsIgnoredCollider(Collider2D col)
    {
        // Add any additional logic to ignore specific colliders (like one-way platforms if needed)
        // For now, if it's on the Ground layer, it blocks us.
        return false;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || playerCollider == null || lastCheckedDirections == null) return;

        // Draw candidate directions
        foreach (Vector2 dir in lastCheckedDirections)
        {
            if (dir == Vector2.zero) continue;
            
            Vector2 checkPos = lastCheckPos + dir.normalized * escapeCheckDistance;
            
            Gizmos.color = (dir.normalized == lastSafeDirection) ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
            
            // Approximate drawing of the capsule check
            Gizmos.DrawWireSphere(checkPos + Vector2.up * (playerCollider.size.y / 4f), playerCollider.size.x / 2f);
            Gizmos.DrawWireSphere(checkPos - Vector2.up * (playerCollider.size.y / 4f), playerCollider.size.x / 2f);
        }
    }
}
