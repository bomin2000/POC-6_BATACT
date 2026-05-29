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

    // Public properties exposing collision status to other scripts/events
    public bool IsContact { get; private set; }
    public Vector2 ContactPoint { get; private set; }
    public Vector2 ContactNormal { get; private set; }
    public Collider2D ContactCollider { get; private set; }
    public WeaponState ActiveState => weaponController != null ? weaponController.CurrentState : WeaponState.BareHand;

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
        // Reset state
        IsContact = false;
        ContactPoint = Vector2.zero;
        ContactNormal = Vector2.zero;
        ContactCollider = null;

        if (weaponController == null)
        {
            return;
        }

        WeaponState state = weaponController.CurrentState;
        if (state == WeaponState.BareHand)
        {
            return;
        }

        Vector3 originPos = probeOrigin != null ? probeOrigin.position : transform.position;
        Vector3 aimDir = probeOrigin != null ? probeOrigin.right : transform.right;

        switch (state)
        {
            case WeaponState.Spear:
                PerformSpearProbe(originPos, aimDir);
                break;

            case WeaponState.Boomerang:
                if (enableBoomerangProbe)
                {
                    PerformCircularOverlapProbe(originPos, boomerangProbeRadius);
                }
                break;

            case WeaponState.Scissors:
                PerformCircularOverlapProbe(originPos, scissorsProbeRadius);
                break;
        }
    }

    private void PerformSpearProbe(Vector3 origin, Vector3 direction)
    {
        // We use CircleCast to simulate a capsule probe along the spear direction.
        RaycastHit2D hit = Physics2D.CircleCast(origin, spearProbeRadius, direction, spearProbeLength, terrainLayers);

        if (hit.collider != null && !IsIgnoredCollider(hit.collider))
        {
            IsContact = true;
            ContactPoint = hit.point;
            ContactNormal = hit.normal;
            ContactCollider = hit.collider;
        }
    }

    private void PerformCircularOverlapProbe(Vector3 origin, float radius)
    {
        Collider2D col = Physics2D.OverlapCircle(origin, radius, terrainLayers);
        if (col != null && !IsIgnoredCollider(col))
        {
            IsContact = true;
            // For overlap, approximate contact point at the closest point of the collider, or the collider's center.
            ContactPoint = col.ClosestPoint(origin);
            ContactNormal = ((Vector2)origin - ContactPoint).normalized;
            ContactCollider = col;
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

    private void OnDrawGizmos()
    {
        if (weaponController == null)
        {
            return;
        }

        WeaponState state = weaponController.CurrentState;
        if (state == WeaponState.BareHand)
        {
            return;
        }

        Vector3 originPos = probeOrigin != null ? probeOrigin.position : transform.position;
        Vector3 aimDir = probeOrigin != null ? probeOrigin.right : transform.right;

        Gizmos.color = IsContact ? Color.red : Color.yellow;

        switch (state)
        {
            case WeaponState.Spear:
                // Draw path of the Spear CircleCast
                Vector3 endPos = originPos + aimDir * spearProbeLength;
                Gizmos.DrawLine(originPos, endPos);
                Gizmos.DrawWireSphere(originPos, spearProbeRadius);
                Gizmos.DrawWireSphere(endPos, spearProbeRadius);
                break;

            case WeaponState.Boomerang:
                if (enableBoomerangProbe)
                {
                    Gizmos.DrawWireSphere(originPos, boomerangProbeRadius);
                }
                break;

            case WeaponState.Scissors:
                Gizmos.DrawWireSphere(originPos, scissorsProbeRadius);
                break;
        }

        if (IsContact)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(ContactPoint, 0.12f);
            Gizmos.DrawRay(ContactPoint, ContactNormal * 0.4f);
        }
    }
}
