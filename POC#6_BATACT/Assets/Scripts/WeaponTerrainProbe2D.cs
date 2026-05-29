using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponTerrainProbe2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DualBladeWeaponController weaponController;
    [SerializeField] private Transform probeOrigin;

    [Header("Target Environment")]
    [SerializeField] private LayerMask terrainLayers;

    [Header("Spear Settings")]
    [SerializeField] private float spearProbeLength = 1.8f;
    [SerializeField] private float spearProbeRadius = 0.15f;

    [Header("Boomerang Settings")]
    [SerializeField] private bool enableBoomerangProbe = false;
    [SerializeField] private float boomerangProbeRadius = 0.5f;

    [Header("Scissors Settings")]
    [SerializeField] private float scissorsProbeRadius = 0.75f;

    [Header("Debugging Options")]
    [SerializeField] private bool enableDebugLogging = false;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private WeaponState debugActiveState;
    [SerializeField] private Vector3 debugStartPoint;
    [SerializeField] private Vector3 debugDirection;
    [SerializeField] private float debugDistance;
    [SerializeField] private bool debugIsContact;
    [SerializeField] private string debugContactDirection;
    [SerializeField] private Vector2 debugContactPoint;
    [SerializeField] private Vector2 debugContactNormal;
    [SerializeField] private string debugContactColliderName;
    [SerializeField] private string debugContactColliderLayerName;

    // Public properties exposing collision status to other scripts/events
    public bool IsContact { get; private set; }
    public Vector2 ContactPoint { get; private set; }
    public Vector2 ContactNormal { get; private set; }
    public Collider2D ContactCollider { get; private set; }
    public WeaponState ActiveState => weaponController != null ? weaponController.CurrentState : WeaponState.BareHand;

    private bool hasWarnedLayers = false;

    private void Awake()
    {
        if (weaponController == null)
        {
            weaponController = GetComponent<DualBladeWeaponController>();
            if (weaponController == null)
            {
                weaponController = GetComponentInParent<DualBladeWeaponController>();
            }
        }

        if (probeOrigin == null && weaponController != null)
        {
            probeOrigin = weaponController.WeaponRoot;
        }

        if (probeOrigin == null)
        {
            probeOrigin = transform;
        }
    }

    private void FixedUpdate()
    {
        EvaluateTerrainContact();
    }

    private void EvaluateTerrainContact()
    {
        // Warn once if terrainLayers is empty (value == 0)
        if (terrainLayers.value == 0 && !hasWarnedLayers)
        {
            hasWarnedLayers = true;
            Debug.LogWarning("[WeaponTerrainProbe2D] Warning: terrainLayers is set to 'Nothing' or empty. Probe will not detect any collisions.", this);
        }

        // Reset runtime public state
        IsContact = false;
        ContactPoint = Vector2.zero;
        ContactNormal = Vector2.zero;
        ContactCollider = null;
        debugContactDirection = "None";

        if (weaponController == null)
        {
            UpdateDebugFields(WeaponState.BareHand, Vector3.zero, Vector3.zero, 0f);
            return;
        }

        WeaponState state = weaponController.CurrentState;
        debugActiveState = state;
        if (state == WeaponState.BareHand)
        {
            UpdateDebugFields(state, Vector3.zero, Vector3.zero, 0f);
            return;
        }

        Vector3 originPos = probeOrigin != null ? probeOrigin.position : transform.position;
        Vector3 aimDir = probeOrigin != null ? probeOrigin.right : transform.right;
        float currentLength = 0f;

        switch (state)
        {
            case WeaponState.Spear:
                currentLength = spearProbeLength;
                PerformSpearDualProbe(originPos, aimDir);
                if (enableDebugLogging)
                {
                    DebugSpearExclusions(originPos, aimDir);
                    DebugSpearExclusions(originPos, -aimDir);
                }
                break;

            case WeaponState.Boomerang:
                if (enableBoomerangProbe)
                {
                    currentLength = boomerangProbeRadius;
                    PerformCircularOverlapProbe(originPos, boomerangProbeRadius);
                    if (enableDebugLogging)
                    {
                        DebugOverlapExclusions(originPos, boomerangProbeRadius);
                    }
                }
                break;

            case WeaponState.Scissors:
                currentLength = scissorsProbeRadius;
                PerformCircularOverlapProbe(originPos, scissorsProbeRadius);
                if (enableDebugLogging)
                {
                    DebugOverlapExclusions(originPos, scissorsProbeRadius);
                }
                break;
        }

        UpdateDebugFields(state, originPos, aimDir, currentLength);
    }

    private void PerformSpearDualProbe(Vector3 origin, Vector3 direction)
    {
        RaycastHit2D hitFwd = Physics2D.CircleCast(origin, spearProbeRadius, direction, spearProbeLength, terrainLayers);
        RaycastHit2D hitBwd = Physics2D.CircleCast(origin, spearProbeRadius, -direction, spearProbeLength, terrainLayers);

        bool fwdValid = hitFwd.collider != null && !IsIgnoredCollider(hitFwd.collider);
        bool bwdValid = hitBwd.collider != null && !IsIgnoredCollider(hitBwd.collider);

        if (fwdValid && bwdValid)
        {
            if (hitFwd.distance <= hitBwd.distance)
            {
                ApplyContact(hitFwd, "Forward");
            }
            else
            {
                ApplyContact(hitBwd, "Backward");
            }
        }
        else if (fwdValid)
        {
            ApplyContact(hitFwd, "Forward");
        }
        else if (bwdValid)
        {
            ApplyContact(hitBwd, "Backward");
        }
    }

    private void PerformCircularOverlapProbe(Vector3 origin, float radius)
    {
        Collider2D col = Physics2D.OverlapCircle(origin, radius, terrainLayers);
        if (col != null && !IsIgnoredCollider(col))
        {
            IsContact = true;
            ContactPoint = col.ClosestPoint(origin);
            ContactNormal = ((Vector2)origin - ContactPoint).normalized;
            ContactCollider = col;
            debugContactDirection = "Center";
        }
    }

    private void ApplyContact(RaycastHit2D hit, string dirString)
    {
        IsContact = true;
        ContactPoint = hit.point;
        ContactNormal = hit.normal;
        ContactCollider = hit.collider;
        debugContactDirection = dirString;
    }

    private void DebugSpearExclusions(Vector3 origin, Vector3 direction)
    {
        // Query ALL layers to log exclusions
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, spearProbeRadius, direction, spearProbeLength, ~0);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            // Check LayerMask exclusion
            if ((terrainLayers.value & (1 << hit.collider.gameObject.layer)) == 0)
            {
                Debug.Log($"[Probe Debug] Spear sweep touched '{hit.collider.name}' but it was EXCLUDED because its layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}' is not in terrainLayers.", this);
            }
            // Check Player/Weapon exclusion
            else if (IsIgnoredCollider(hit.collider))
            {
                Debug.Log($"[Probe Debug] Spear sweep touched '{hit.collider.name}' but it was EXCLUDED because it belongs to the player, weapon controller, or probe game object itself.", this);
            }
        }
    }

    private void DebugOverlapExclusions(Vector3 origin, float radius)
    {
        // Query ALL layers to log exclusions
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, radius, ~0);
        foreach (var col in colliders)
        {
            if (col == null) continue;

            // Check LayerMask exclusion
            if ((terrainLayers.value & (1 << col.gameObject.layer)) == 0)
            {
                Debug.Log($"[Probe Debug] Overlap touched '{col.name}' but it was EXCLUDED because its layer '{LayerMask.LayerToName(col.gameObject.layer)}' is not in terrainLayers.", this);
            }
            // Check Player/Weapon exclusion
            else if (IsIgnoredCollider(col))
            {
                Debug.Log($"[Probe Debug] Overlap touched '{col.name}' but it was EXCLUDED because it belongs to the player, weapon controller, or probe game object itself.", this);
            }
        }
    }

    private bool IsIgnoredCollider(Collider2D col)
    {
        if (col == null)
        {
            return true;
        }

        // Exclude the player's own game object hierarchy
        if (col.transform == transform || col.transform.IsChildOf(transform))
        {
            return true;
        }

        // Exclude weapon visual system colliders if known
        if (weaponController != null && weaponController.ContainsWeaponCollider(col))
        {
            return true;
        }

        return false;
    }

    private void UpdateDebugFields(WeaponState state, Vector3 origin, Vector3 dir, float dist)
    {
        debugActiveState = state;
        debugStartPoint = origin;
        debugDirection = dir;
        debugDistance = dist;
        debugIsContact = IsContact;
        debugContactPoint = ContactPoint;
        debugContactNormal = ContactNormal;
        debugContactColliderName = ContactCollider != null ? ContactCollider.name : "None";
        debugContactColliderLayerName = ContactCollider != null ? LayerMask.LayerToName(ContactCollider.gameObject.layer) : "None";
    }

    private void OnDrawGizmos()
    {
        bool isConfigValid = terrainLayers.value != 0 && (debugActiveState != WeaponState.Spear || debugDistance > 0f);

        if (debugActiveState == WeaponState.BareHand)
        {
            return;
        }

        Vector3 start = debugStartPoint;
        Vector3 dir = debugDirection;
        float dist = debugDistance;

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = transform.right;
        }

        switch (debugActiveState)
        {
            case WeaponState.Spear:
                Color fwdColor = isConfigValid ? (debugContactDirection == "Forward" ? Color.green : Color.yellow) : Color.red;
                Color bwdColor = isConfigValid ? (debugContactDirection == "Backward" ? Color.green : Color.yellow) : Color.red;

                DrawSpearCapsuleGizmo(start, dir, dist, spearProbeRadius, fwdColor);
                DrawSpearCapsuleGizmo(start, -dir, dist, spearProbeRadius, bwdColor);
                break;

            case WeaponState.Boomerang:
                if (enableBoomerangProbe)
                {
                    Gizmos.color = isConfigValid ? (debugIsContact ? Color.green : Color.yellow) : Color.red;
                    Gizmos.DrawWireSphere(start, boomerangProbeRadius);
                    DrawCross(start, 0.08f);
                }
                break;

            case WeaponState.Scissors:
                Gizmos.color = isConfigValid ? (debugIsContact ? Color.green : Color.yellow) : Color.red;
                Gizmos.DrawWireSphere(start, scissorsProbeRadius);
                DrawCross(start, 0.08f);
                break;
        }

        // Draw explicit contact point and normal line
        if (debugIsContact)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(debugContactPoint, 0.08f);
            Gizmos.DrawWireSphere(debugContactPoint, 0.15f);
            Gizmos.DrawRay(debugContactPoint, debugContactNormal * 0.5f);
        }
    }

    private void DrawSpearCapsuleGizmo(Vector3 start, Vector3 dir, float dist, float radius, Color color)
    {
        Gizmos.color = color;
        Vector3 end = start + dir * dist;
        Gizmos.DrawLine(start, end);
        
        Vector3 sideOffset = new Vector3(-dir.y, dir.x, 0f) * radius;
        Gizmos.DrawLine(start + sideOffset, end + sideOffset);
        Gizmos.DrawLine(start - sideOffset, end - sideOffset);

        Gizmos.DrawWireSphere(start, radius);
        Gizmos.DrawWireSphere(end, radius);

        DrawCross(start, 0.08f);
        DrawCross(end, 0.08f);
    }

    private void DrawCross(Vector3 position, float size)
    {
        Gizmos.DrawLine(position - Vector3.right * size, position + Vector3.right * size);
        Gizmos.DrawLine(position - Vector3.up * size, position + Vector3.up * size);
    }
}
