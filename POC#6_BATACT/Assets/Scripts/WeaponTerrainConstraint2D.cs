using UnityEngine;

public class WeaponTerrainConstraint2D : MonoBehaviour
{
    [Header("Settings")]
    public bool enableConstraints = true;
    public LayerMask terrainLayers;
    public float spearLength = 2.5f; // Should match spear visual length roughly
    public float scissorsRadius = 0.5f;
    public float probeRadius = 0.2f;

    [Header("Pole Vault")]
    public KeyCode jumpKey = KeyCode.Space;
    public float poleVaultUpImpulse = 18f;
    public float poleVaultForwardImpulse = 5f;
    public float vaultCooldown = 0.4f;

    [Header("Angle & Lift Settings")]
    public float plantAngleThreshold = 25f; // within 25 degrees of vertical (straight down)
    public float smoothLiftSpeed = 10f; // Speed for smooth damp/lerp

    private float vaultTimer;
    private bool isSpearPlanted;
    private bool isVerticalPoleState;
    private Vector2 plantWorldPosition;
    private float targetLiftY;

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

        if (controller != null && controller.CurrentState != WeaponState.Spear)
        {
            ResetStates();
            return;
        }

        if (isVerticalPoleState && vaultTimer <= 0f && Input.GetKeyDown(jumpKey))
        {
            TryVault();
        }
    }

    private void FixedUpdate()
    {
        if (controller == null || controller.CurrentState != WeaponState.Spear || playerBody == null)
        {
            return;
        }

        if (isVerticalPoleState)
        {
            Vector2 pos = playerBody.position;
            // Smoothly lift the player to targetLiftY over ~0.1-0.2 seconds
            float currentY = Mathf.Lerp(pos.y, targetLiftY, smoothLiftSpeed * Time.fixedDeltaTime);
            playerBody.position = new Vector2(pos.x, currentY);

            // Prevent falling while in pole pose
            if (playerBody.linearVelocity.y < 0f)
            {
                playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, 0f);
            }
        }
    }

    private void TryVault()
    {
        if (playerMovement == null) return;

        vaultTimer = vaultCooldown;
        // Strong upward force + slight forward force based on player facing
        int facing = playerMovement.FacingSign;
        Vector2 impulse = new Vector2(facing * poleVaultForwardImpulse, poleVaultUpImpulse);

        playerMovement.ApplyExternalKnockback(impulse, 0.25f);
        ResetStates();
    }

    public void ResetStates()
    {
        isSpearPlanted = false;
        isVerticalPoleState = false;
        if (playerMovement != null)
        {
            playerMovement.SetMovementRestrictions(false, false);
        }
    }

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
            
            bool restrictLeft = false;
            bool restrictRight = false;

            if (hit.collider != null && !IsIgnoredCollider(hit.collider))
            {
                // Slide weapon backwards by overlap so it doesn't penetrate visually
                float overlap = targetLength - hit.distance;
                if (overlap > 0)
                {
                    pivotOffset -= (Vector3)(aimDirection * overlap);
                }

                if (Mathf.Abs(hit.normal.x) > 0.5f)
                {
                    // Hit a wall
                    isBlocked = true;
                    if (hit.normal.x > 0f) restrictLeft = true;
                    else restrictRight = true;
                    
                    isVerticalPoleState = false;
                }
                else if (hit.normal.y > 0.7f) // Ground hit
                {
                    if (!isSpearPlanted)
                    {
                        isSpearPlanted = true;
                        plantWorldPosition = hit.point;
                    }

                    // Check for vertical pole pose
                    float angleToVertical = Vector2.Angle(aimDirection, Vector2.down);
                    if (angleToVertical <= plantAngleThreshold)
                    {
                        isVerticalPoleState = true;
                        
                        // Calculate exact Y to lift player so spear perfectly touches ground
                        float pivotWorldOffsetY = transform.TransformPoint(pivotOffset).y - playerBody.position.y;
                        targetLiftY = plantWorldPosition.y - aimDirection.y * targetLength - pivotWorldOffsetY - 0.05f;
                    }
                    else
                    {
                        isVerticalPoleState = false;
                    }
                }
            }
            else
            {
                isSpearPlanted = false;
                isVerticalPoleState = false;
            }

            if (playerMovement != null)
            {
                playerMovement.SetMovementRestrictions(restrictLeft, restrictRight);
            }
        }
        else if (state == WeaponState.Scissors)
        {
            ResetStates();

            Vector3 worldPivot = transform.TransformPoint(pivotOffset);
            Collider2D col = Physics2D.OverlapCircle(worldPivot, scissorsRadius, terrainLayers);
            if (col != null && !IsIgnoredCollider(col))
            {
                Vector2 closest = col.ClosestPoint(worldPivot);
                float dist = Vector2.Distance(worldPivot, closest);
                float overlap = scissorsRadius - dist;
                
                if (overlap > 0)
                {
                    isBlocked = true;
                    pivotOffset = pivotOffset.normalized * Mathf.Max(0, pivotOffset.magnitude - overlap);
                }
            }
        }
        else
        {
            ResetStates();
        }

        return isBlocked;
    }

    private bool IsIgnoredCollider(Collider2D col)
    {
        if (col == null) return true;
        if (col.isTrigger) return true;
        if (col.transform == playerBody.transform || col.transform.IsChildOf(playerBody.transform)) return true;
        if (controller != null && controller.ContainsWeaponCollider(col)) return true;
        return false;
    }
}
