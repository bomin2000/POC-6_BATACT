using UnityEngine;

public class WeaponTerrainConstraint2D : MonoBehaviour
{
    [Header("Settings")]
    public bool enableConstraints = true;
    public LayerMask terrainLayers;
    public float spearLength = 2.5f; // Should match spear visual length roughly
    public float scissorsRadius = 0.5f;
    public float probeRadius = 0.2f;

    [Header("Push Limits")]
    public float maxPushDelta = 0.5f; 

    [Header("Pole Vault")]
    public KeyCode jumpKey = KeyCode.Space;
    public float floorVaultUpImpulse = 18f;
    public float wallVaultSideImpulse = 15f;
    public float wallVaultUpImpulse = 14f;
    public float vaultCooldown = 0.4f;

    private float vaultTimer;
    private bool isSpearPlanted;
    private Vector2 lastPlantNormal;

    private Rigidbody2D playerBody;
    private Collider2D playerCollider;
    private PlayerTopDownMovement playerMovement;
    private DualBladeWeaponController controller;

    private void Awake()
    {
        playerBody = GetComponentInParent<Rigidbody2D>();
        playerCollider = GetComponentInParent<Collider2D>();
        playerMovement = GetComponentInParent<PlayerTopDownMovement>();
        controller = GetComponent<DualBladeWeaponController>();
    }

    private void Update()
    {
        if (vaultTimer > 0f) vaultTimer -= Time.deltaTime;

        if (isSpearPlanted && vaultTimer <= 0f && Input.GetKeyDown(jumpKey))
        {
            TryVault();
        }
        
        // Reset plant status each frame; it will be set again if constrained
        isSpearPlanted = false;
    }

    private void TryVault()
    {
        if (playerMovement == null) return;

        vaultTimer = vaultCooldown;
        Vector2 impulse = Vector2.zero;

        if (lastPlantNormal.y > 0.5f) // Floor Vault
        {
            impulse = new Vector2(0f, floorVaultUpImpulse);
        }
        else // Wall Vault
        {
            float awayFromWallSign = Mathf.Sign(lastPlantNormal.x);
            impulse = new Vector2(awayFromWallSign * wallVaultSideImpulse, wallVaultUpImpulse);
        }

        playerMovement.ApplyExternalKnockback(impulse, 0.25f);
    }

    /// <summary>
    /// Checks for penetration. If penetration occurs, attempts to push the player.
    /// If push is not enough/blocked, it returns true to indicate the weapon should be visually blocked.
    /// </summary>
    public bool ApplyConstraint(ref Vector2 aimDirection, ref Vector3 pivotOffset, ref float lengthScale, WeaponState state)
    {
        if (!enableConstraints || playerBody == null)
            return false;

        bool isBlocked = false;

        if (state == WeaponState.Spear)
        {
            Vector3 worldPivot = transform.TransformPoint(pivotOffset);
            Vector2 origin = worldPivot;
            float targetLength = spearLength * lengthScale;

            RaycastHit2D hit = Physics2D.CircleCast(origin, probeRadius, aimDirection, targetLength, terrainLayers);
            
            if (hit.collider != null && !IsIgnoredCollider(hit.collider))
            {
                float overlap = targetLength - hit.distance;

                // Instead of violently pushing the player instantly when swiping down,
                // we limit the vertical push velocity, or we reduce lengthScale if overlap is too big.
                // If it's a floor and overlap is large, it means the player just swiped down.
                // We only want to lift the player smoothly.
                
                Vector2 pushDelta = hit.normal * overlap;

                // Restrict how much we can be pushed in one frame to prevent teleportation
                if (pushDelta.magnitude > maxPushDelta)
                {
                    // If the push is too large (like a sudden mouse swipe), we block the weapon visually
                    // instead of teleporting the player.
                    pushDelta = pushDelta.normalized * maxPushDelta;
                    // Shrink the spear visually to hide the remaining penetration
                    lengthScale = Mathf.Max(0f, (hit.distance + maxPushDelta) / spearLength);
                    isBlocked = true;
                }

                if (CanMovePlayer(pushDelta))
                {
                    if (playerMovement != null) playerMovement.ApplyWeaponPushDelta(pushDelta);
                    else playerBody.position += pushDelta;

                    isSpearPlanted = true;
                    lastPlantNormal = hit.normal;
                }
                else
                {
                    if (targetLength > 0.01f)
                    {
                        lengthScale = Mathf.Max(0f, hit.distance / spearLength);
                    }
                    isBlocked = true;

                    // If the aim is restricted by the floor, it's also planted
                    if (hit.normal.y > 0.5f)
                    {
                        isSpearPlanted = true;
                        lastPlantNormal = hit.normal;
                    }
                }
            }
        }
        else if (state == WeaponState.Scissors)
        {
            Vector3 worldPivot = transform.TransformPoint(pivotOffset);
            Collider2D col = Physics2D.OverlapCircle(worldPivot, scissorsRadius, terrainLayers);
            if (col != null && !IsIgnoredCollider(col))
            {
                // Scissors shouldn't push the player (no pole vaulting), just block the weapon movement
                Vector2 closest = col.ClosestPoint(worldPivot);
                float dist = Vector2.Distance(worldPivot, closest);
                float overlap = scissorsRadius - dist;
                
                if (overlap > 0)
                {
                    isBlocked = true;
                    // Reduce pivot offset distance to prevent visual penetration
                    pivotOffset = pivotOffset.normalized * Mathf.Max(0, pivotOffset.magnitude - overlap);
                }
            }
        }

        return isBlocked;
    }

    private bool CanMovePlayer(Vector2 delta)
    {
        if (delta.sqrMagnitude < 0.0001f) return true;
        
        // Clamp large deltas
        if (delta.magnitude > maxPushDelta)
        {
            delta = delta.normalized * maxPushDelta;
        }

        // Simple check: sweep the player collider
        if (playerCollider != null)
        {
            int hitCount = playerCollider.Cast(delta.normalized, new ContactFilter2D { layerMask = terrainLayers, useLayerMask = true }, new RaycastHit2D[1], delta.magnitude);
            if (hitCount > 0)
            {
                return false; // Path is blocked
            }
        }
        return true;
    }

    private bool IsIgnoredCollider(Collider2D col)
    {
        if (col == null) return true;
        if (col.isTrigger) return true; // Ignore trigger zones like EncounterTrigger or StageGoal
        if (col.transform == playerBody.transform || col.transform.IsChildOf(playerBody.transform)) return true;
        if (controller != null && controller.ContainsWeaponCollider(col)) return true;
        return false;
    }
}
