using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class DualBladeBoomerangProjectile : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float outboundSpeed = 16f;
    [SerializeField] private float returnSpeed = 20f;
    [SerializeField] private float outboundSeconds = 0.35f;
    [SerializeField] private float maxBareHandSeconds = 1f;
    [SerializeField] private float catchRadius = 0.55f;
    [SerializeField] private float spinDegreesPerSecond = 1080f;

    [Header("Curved Return")]
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float arcHeight = 1.4f;

    [Header("Hit")]
    [SerializeField] private LayerMask targetLayers = ~0;

    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private readonly HashSet<Rigidbody2D> draggedBodies = new HashSet<Rigidbody2D>();
    private readonly Collider2D[] overlapBuffer = new Collider2D[16];

    private Transform owner;
    private WeaponHitboxProfile hitProfile;
    private Vector2 launchDirection;
    private Vector3 returnStartPosition;
    private float elapsed;
    private bool returning;
    private bool initialized;

    public bool IsReturning => returning;
    public System.Action<DualBladeBoomerangProjectile> Caught;
    public System.Action<Collider2D, WeaponHitboxProfile> OnHit;

    private void Awake()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.isTrigger = true;
        }
    }

    public void Launch(Transform ownerTransform, WeaponHitboxProfile profile, Vector2 direction)
    {
        owner = ownerTransform;
        hitProfile = profile;
        launchDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        elapsed = 0f;
        returning = false;
        initialized = true;
        
        hitTargets.Clear();
        draggedBodies.Clear();

        if (profile != null && profile.name.Contains("Empowered"))
        {
            transform.localScale = Vector3.one * 1.8f;
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1f, 0.5f, 0.5f); // Red tint for empowered
            }
        }
        else
        {
            transform.localScale = Vector3.one;
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.white;
            }
        }
    }

    private void Update()
    {
        if (!initialized || owner == null)
        {
            return;
        }

        elapsed += Time.deltaTime;
        transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);

        if (!returning)
        {
            transform.position += (Vector3)(launchDirection * outboundSpeed * Time.deltaTime);

            if (elapsed >= outboundSeconds || elapsed >= maxBareHandSeconds * 0.5f)
            {
                BeginReturn();
            }
        }
        else
        {
            TickCurvedReturn();
        }

        TickHitOverlap();
    }

    private void FixedUpdate()
    {
        if (!initialized || draggedBodies.Count == 0) return;

        draggedBodies.RemoveWhere(b => b == null || !b.gameObject.activeInHierarchy);

        foreach (var body in draggedBodies)
        {
            Vector2 dirToBoomerang = (Vector2)transform.position - body.position;
            // Drag the enemy continuously towards the boomerang
            body.linearVelocity = dirToBoomerang * 15f;
        }
    }

    private void BeginReturn()
    {
        returning = true;
        elapsed = 0f;
        returnStartPosition = transform.position;
        
        // Clear hit targets so they can be hit (and damaged) again on the return trip
        hitTargets.Clear();
    }

    private void TickCurvedReturn()
    {
        Vector3 target = owner.position;
        float distance = Vector3.Distance(returnStartPosition, target);
        float duration = Mathf.Max(0.05f, distance / returnSpeed);
        float t = Mathf.Clamp01(elapsed / duration);
        float curvedT = returnCurve.Evaluate(t);

        // 귀환 목표는 매 프레임 owner.position을 다시 읽습니다.
        // 플레이어가 대시해도 현재 좌표를 향해 베지어 중간점이 즉시 재계산됩니다.
        Vector3 mid = (returnStartPosition + target) * 0.5f + Vector3.up * arcHeight;
        Vector3 a = Vector3.Lerp(returnStartPosition, mid, curvedT);
        Vector3 b = Vector3.Lerp(mid, target, curvedT);
        transform.position = Vector3.Lerp(a, b, curvedT);

        if (Vector2.Distance(transform.position, target) <= catchRadius || elapsed >= maxBareHandSeconds)
        {
            draggedBodies.Clear();
            Caught?.Invoke(this);
        }
    }

    private void TickHitOverlap()
    {
        if (hitProfile == null)
        {
            return;
        }

        int count = Physics2D.OverlapCircleNonAlloc(transform.position, hitProfile.radius, overlapBuffer, targetLayers);
        for (int i = 0; i < count; i++)
        {
            Collider2D target = overlapBuffer[i];
            if (target == null || hitTargets.Contains(target))
            {
                continue;
            }

            IWeaponHitReceiver receiver = target.GetComponentInParent<IWeaponHitReceiver>();
            if (receiver == null || receiver.Equals(owner.GetComponent<IWeaponHitReceiver>()) || target.gameObject == owner.gameObject || target.transform.IsChildOf(owner))
            {
                continue;
            }

            hitTargets.Add(target);

            // Hook the target for dragging
            Rigidbody2D body = target.GetComponentInParent<Rigidbody2D>();
            if (body != null && !body.isKinematic)
            {
                draggedBodies.Add(body);
            }

            Vector2 impulse = hitProfile.reaction.BuildImpulse(
                launchDirection,
                owner.position,
                target.bounds.center);

            receiver.ReceiveWeaponHit(hitProfile.reaction, impulse, owner.gameObject);
            OnHit?.Invoke(target, hitProfile);

            if (hitProfile.healOnHit > 0f && owner != null)
            {
                PlayerHealth health = owner.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.Heal(hitProfile.healOnHit);
                }
            }
        }
    }
}
