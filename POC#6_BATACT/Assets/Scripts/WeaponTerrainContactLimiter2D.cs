using UnityEngine;

public class WeaponTerrainContactLimiter2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponTerrainProbe2D terrainProbe;

    [Header("Settings")]
    [SerializeField] private bool isActive = true;
    [Tooltip("How much margin to leave when clamping scissors lunge.")]
    [SerializeField] private float scissorsBladeMargin = 0.5f;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private bool debugIsLimitingAim;
    [SerializeField] private bool debugIsLimitingPivot;
    [SerializeField] private WeaponState debugActiveState;
    [SerializeField] private Vector2 debugContactNormal;
    [SerializeField] private Vector2 debugOriginalAim;
    [SerializeField] private Vector2 debugLimitedAim;
    [SerializeField] private string debugLimiterReason;

    public bool IsActivelyLimitingAim => debugIsLimitingAim;
    public bool IsActivelyLimitingPivot => debugIsLimitingPivot;

    private void Awake()
    {
        if (terrainProbe == null)
            terrainProbe = GetComponentInChildren<WeaponTerrainProbe2D>();
    }

    public Vector2 LimitAimDirection(Vector2 rawAim, WeaponState state)
    {
        debugIsLimitingAim = false;
        debugOriginalAim = rawAim;
        debugLimitedAim = rawAim;
        debugActiveState = state;

        if (!isActive || terrainProbe == null)
        {
            debugLimiterReason = "Inactive or Missing Probe";
            return rawAim;
        }

        if (state != WeaponState.Spear)
        {
            debugLimiterReason = "Not Spear";
            return rawAim;
        }

        if (!terrainProbe.IsContact)
        {
            debugLimiterReason = "No Contact";
            return rawAim;
        }

        Vector2 normal = terrainProbe.ContactNormal;
        debugContactNormal = normal;

        Vector2 contactPt = terrainProbe.ContactPoint;
        Vector2 origin = terrainProbe.ProbeOrigin;
        float L = terrainProbe.SpearProbeLength;
        float R = terrainProbe.SpearProbeRadius;

        // Calculate perpendicular distance from the pivot to the surface tangent at the contact point
        float h = Vector2.Dot(origin - contactPt, normal);

        // We want the dot product of Aim and Normal to be >= (R - h) / L
        // so that the tip of the spear's bounding sphere exactly touches the plane.
        float minY = (R - h) / L;
        
        // Add a tiny tolerance to prevent floating point flip-flops
        minY -= 0.02f;

        float rawY = Vector2.Dot(rawAim, normal);
        if (rawY >= minY)
        {
            debugLimiterReason = "Aim is valid (does not penetrate)";
            return rawAim;
        }

        debugIsLimitingAim = true;
        debugLimiterReason = "Clamped to surface angle";

        // Clamp the local Y (normal component) to minY
        float y = Mathf.Clamp(minY, -1f, 1f);
        
        // Calculate the corresponding local X (tangent component)
        float xMag = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));

        Vector2 tangent = new Vector2(-normal.y, normal.x);
        float rawX = Vector2.Dot(rawAim, tangent);
        float x = xMag * Mathf.Sign(rawX == 0 ? 1 : rawX);

        Vector2 clampedAim = tangent * x + normal * y;

        debugLimitedAim = clampedAim;
        return clampedAim;
    }

    public Vector3 LimitPivotOffset(Vector3 targetPivot, WeaponState state, Transform weaponRoot)
    {
        debugIsLimitingPivot = false;

        if (!isActive || terrainProbe == null)
            return targetPivot;

        if (state != WeaponState.Scissors)
            return targetPivot;

        if (!terrainProbe.IsContact)
            return targetPivot;

        Vector2 normal = terrainProbe.ContactNormal;
        
        // Is the weapon aimed at the wall?
        float dot = Vector2.Dot(weaponRoot.right, normal);
        if (dot < -0.1f)
        {
            debugIsLimitingPivot = true;
            
            // Project vectors onto the weapon's local forward axis (weaponRoot.right)
            Vector2 toTarget = weaponRoot.TransformPoint(targetPivot) - weaponRoot.position;
            Vector2 toContact = (Vector3)terrainProbe.ContactPoint - weaponRoot.position;
            
            float targetX = Vector2.Dot(toTarget, weaponRoot.right);
            float contactX = Vector2.Dot(toContact, weaponRoot.right);
            
            // Prevent the pivot from moving past the contact point minus the blade length margin
            float maxPivotX = contactX - scissorsBladeMargin;
            
            Vector3 clampedPivot = targetPivot;
            
            // targetPivot.x is local, but we might have mirroring. 
            // DualBladeWeaponController applies Abs(scale.x) * aimSign.
            // For safety, since targetPivot is local to the pivot container, we just clamp its absolute magnitude if it exceeds maxPivotX.
            if (targetPivot.x > 0 && targetPivot.x > maxPivotX)
            {
                clampedPivot.x = Mathf.Max(0, maxPivotX);
            }
            
            return clampedPivot;
        }

        return targetPivot;
    }
}
