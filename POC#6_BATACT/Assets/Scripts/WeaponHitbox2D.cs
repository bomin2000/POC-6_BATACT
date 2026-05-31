using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class WeaponHitbox2D : MonoBehaviour
{
    [Header("Hitbox Target")]
    [SerializeField] private LayerMask targetLayers = ~0;
    
    [Header("Preview Settings")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private DualBladeWeaponController weaponController;
    [SerializeField] private Color previewColor = new Color(1f, 0.4f, 0f, 0.8f);

    private readonly Collider2D[] overlapBuffer = new Collider2D[32];
    private readonly HashSet<Collider2D> hitThisSwing = new HashSet<Collider2D>();
    private Coroutine attackRoutine;
    private WeaponHitboxProfile currentProfile;
    private Transform owner;

    public bool IsAttacking => attackRoutine != null;

    public System.Action<Collider2D, WeaponHitboxProfile> OnHitSuccessful;

    public void Initialize(Transform ownerTransform)
    {
        owner = ownerTransform;
        if (weaponController == null)
        {
            weaponController = ownerTransform.GetComponent<DualBladeWeaponController>();
        }
    }

    public bool TryAttack(WeaponHitboxProfile profile, Vector2 attackForward)
    {
        return TryAttack(profile, attackForward, null);
    }

    public bool TryAttack(WeaponHitboxProfile profile, Vector2 attackForward, Func<Vector2> dynamicWorldOffset)
    {
        return TryAttack(profile, attackForward, dynamicWorldOffset, false);
    }

    public bool TryAttack(WeaponHitboxProfile profile, Vector2 attackForward, Func<Vector2> dynamicWorldOffset, bool interruptCurrentAttack)
    {
        if (profile == null || attackRoutine != null)
        {
            if (!interruptCurrentAttack)
            {
                return false;
            }

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
        }

        if (profile == null)
        {
            return false;
        }

        currentProfile = profile;
        attackRoutine = StartCoroutine(AttackRoutine(profile, attackForward.normalized, dynamicWorldOffset));
        return true;
    }

    private IEnumerator AttackRoutine(WeaponHitboxProfile profile, Vector2 attackForward, Func<Vector2> dynamicWorldOffset)
    {
        hitThisSwing.Clear();

        if (profile.startupSeconds > 0f)
        {
            yield return new WaitForSeconds(profile.startupSeconds);
        }

        float endTime = Time.time + profile.activeSeconds;
        while (Time.time < endTime)
        {
            TickOverlap(profile, attackForward, dynamicWorldOffset);
            yield return null;
        }

        if (profile.recoverySeconds > 0f)
        {
            yield return new WaitForSeconds(profile.recoverySeconds);
        }

        hitThisSwing.Clear();
        attackRoutine = null;
    }

    private void TickOverlap(WeaponHitboxProfile profile, Vector2 attackForward, Func<Vector2> dynamicWorldOffset)
    {
        Vector2 center = GetWorldCenter(profile, attackForward, dynamicWorldOffset);
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
            Debug.Log($"WeaponHitbox2D Hit: {target.name} at center {center}");
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
                owner != null ? owner.position : transform.position,
                target.bounds.center);

            receiver.ReceiveWeaponHit(profile.reaction, impulse, owner != null ? owner.gameObject : gameObject);
            
            OnHitSuccessful?.Invoke(target, profile);

            if (profile.healOnHit > 0f && owner != null)
            {
                PlayerHealth health = owner.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.Heal(profile.healOnHit);
                }
            }
        }
    }

    private Vector2 GetWorldCenter(WeaponHitboxProfile profile, Vector2 attackForward, Func<Vector2> dynamicWorldOffset = null)
    {
        Vector2 right = attackForward.sqrMagnitude > 0.0001f ? attackForward.normalized : Vector2.right;
        Vector2 up = new Vector2(-right.y, right.x);
        Vector2 origin = owner != null ? (Vector2)owner.position : (Vector2)transform.position;
        Vector2 extraOffset = dynamicWorldOffset != null ? dynamicWorldOffset.Invoke() : Vector2.zero;

        return origin + extraOffset + right * profile.localOffset.x + up * profile.localOffset.y;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        WeaponHitboxProfile previewProfile = currentProfile;

        // If not actively attacking, preview the profile of the current weapon state
        if (!IsAttacking)
        {
            if (weaponController == null)
            {
                weaponController = GetComponentInParent<DualBladeWeaponController>();
            }

            if (weaponController != null)
            {
                switch (weaponController.CurrentState)
                {
                    case WeaponState.Spear:
                        previewProfile = weaponController.SpearProfile;
                        break;
                    case WeaponState.Boomerang:
                        previewProfile = weaponController.BoomerangProfile;
                        break;
                    case WeaponState.Scissors:
                        previewProfile = weaponController.ScissorsProfile;
                        break;
                }
            }
        }

        if (previewProfile == null)
        {
            return;
        }

        Vector2 forward = owner != null ? owner.right : transform.right;
        
        // Handle case where weaponRoot hasn't rotated yet but controller knows aim
        if (weaponController != null && weaponController.WeaponRoot != null)
        {
            forward = weaponController.WeaponRoot.right;
        }

        Vector2 center = GetWorldCenter(previewProfile, forward);
        Gizmos.color = previewColor;

        if (previewProfile.shape == HitboxShape.Circle)
        {
            Gizmos.DrawWireSphere(center, previewProfile.radius);
        }
        else
        {
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.FromToRotation(Vector3.right, forward), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, previewProfile.boxSize);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
