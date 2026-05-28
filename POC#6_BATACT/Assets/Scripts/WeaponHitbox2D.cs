using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponHitbox2D : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private bool drawGizmos = true;

    private readonly Collider2D[] overlapBuffer = new Collider2D[32];
    private readonly HashSet<Collider2D> hitThisSwing = new HashSet<Collider2D>();
    private Coroutine attackRoutine;
    private WeaponHitboxProfile currentProfile;
    private Transform owner;

    public bool IsAttacking => attackRoutine != null;

    public void Initialize(Transform ownerTransform)
    {
        owner = ownerTransform;
    }

    public bool TryAttack(WeaponHitboxProfile profile, Vector2 attackForward)
    {
        if (profile == null || attackRoutine != null)
        {
            return false;
        }

        currentProfile = profile;
        attackRoutine = StartCoroutine(AttackRoutine(profile, attackForward.normalized));
        return true;
    }

    private IEnumerator AttackRoutine(WeaponHitboxProfile profile, Vector2 attackForward)
    {
        hitThisSwing.Clear();

        if (profile.startupSeconds > 0f)
        {
            yield return new WaitForSeconds(profile.startupSeconds);
        }

        float endTime = Time.time + profile.activeSeconds;
        while (Time.time < endTime)
        {
            TickOverlap(profile, attackForward);
            yield return null;
        }

        if (profile.recoverySeconds > 0f)
        {
            yield return new WaitForSeconds(profile.recoverySeconds);
        }

        hitThisSwing.Clear();
        attackRoutine = null;
    }

    private void TickOverlap(WeaponHitboxProfile profile, Vector2 attackForward)
    {
        Vector2 center = GetWorldCenter(profile, attackForward);
        int hitCount;

        if (profile.shape == HitboxShape.Circle)
        {
            hitCount = Physics2D.OverlapCircleNonAlloc(center, profile.radius, overlapBuffer, targetLayers);
        }
        else
        {
            float angle = Mathf.Atan2(attackForward.y, attackForward.x) * Mathf.Rad2Deg;
            hitCount = Physics2D.OverlapBoxNonAlloc(center, profile.boxSize, angle, overlapBuffer, targetLayers);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D target = overlapBuffer[i];
            if (target == null || hitThisSwing.Contains(target))
            {
                continue;
            }

            IWeaponHitReceiver receiver = target.GetComponentInParent<IWeaponHitReceiver>();
            if (receiver == null)
            {
                continue;
            }

            hitThisSwing.Add(target);
            Vector2 impulse = profile.reaction.BuildImpulse(
                attackForward,
                owner.position,
                target.bounds.center);

            receiver.ReceiveWeaponHit(profile.reaction, impulse, owner.gameObject);
        }
    }

    private Vector2 GetWorldCenter(WeaponHitboxProfile profile, Vector2 attackForward)
    {
        Vector2 right = attackForward.sqrMagnitude > 0.0001f ? attackForward.normalized : Vector2.right;
        Vector2 up = new Vector2(-right.y, right.x);
        Vector2 origin = owner != null ? (Vector2)owner.position : (Vector2)transform.position;

        return origin + right * profile.localOffset.x + up * profile.localOffset.y;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || currentProfile == null)
        {
            return;
        }

        Vector2 forward = owner != null ? owner.right : transform.right;
        Vector2 center = GetWorldCenter(currentProfile, forward);
        Gizmos.color = Color.red;

        if (currentProfile.shape == HitboxShape.Circle)
        {
            Gizmos.DrawWireSphere(center, currentProfile.radius);
        }
        else
        {
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.FromToRotation(Vector3.right, forward), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, currentProfile.boxSize);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
