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
    private readonly HashSet<Transform> hitRootsThisSwing = new HashSet<Transform>();
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
        hitRootsThisSwing.Clear();

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

        hitRootsThisSwing.Clear();
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

        Dictionary<Transform, Collider2D> selectedColliders = new Dictionary<Transform, Collider2D>();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D target = overlapBuffer[i];
            if (target == null) continue;
            
            Transform root = target.transform.root;
            if (hitRootsThisSwing.Contains(root)) continue;

            if (!selectedColliders.ContainsKey(root))
            {
                selectedColliders[root] = target;
            }
            else
            {
                // Conflict. Pick the one with guarding ShieldGuardTrait if any, else fallback
                Collider2D existing = selectedColliders[root];
                ShieldGuardTrait newShield = target.GetComponent<ShieldGuardTrait>();
                ShieldGuardTrait existingShield = existing.GetComponent<ShieldGuardTrait>();

                Vector3 ownerPos = owner != null ? owner.position : transform.position;
                bool newIsGuarding = newShield != null && newShield.IsGuarding(ownerPos);
                bool existingIsGuarding = existingShield != null && existingShield.IsGuarding(ownerPos);

                if (newIsGuarding && !existingIsGuarding)
                {
                    selectedColliders[root] = target;
                }
                else if (!newIsGuarding && !existingIsGuarding)
                {
                    // Prefer EnemyBody (which usually sits on a parent)
                    if (target.GetComponent<EnemyBody>() != null)
                    {
                        selectedColliders[root] = target;
                    }
                }
            }
        }

        foreach (var kvp in selectedColliders)
        {
            Collider2D target = kvp.Value;
            Transform root = kvp.Key;

            IWeaponHitReceiver receiver = target.GetComponentInParent<IWeaponHitReceiver>();
            if (receiver == null) continue;

            // Prevent attacking through walls
            Vector2 rayOrigin = owner != null ? owner.position : transform.position;
            Vector2 targetPos = target.bounds.center;
            Vector2 dirToTarget = targetPos - rayOrigin;
            float distToTarget = dirToTarget.magnitude;
            
            int obstacleLayerMask = LayerMask.GetMask("Default", "Terrain", "Ground");
            if (obstacleLayerMask == 0) obstacleLayerMask = 1;

            RaycastHit2D wallHit = Physics2D.Raycast(rayOrigin, dirToTarget.normalized, distToTarget, obstacleLayerMask);
            if (wallHit.collider != null && !wallHit.collider.isTrigger)
            {
                continue; // Blocked by a wall
            }

            hitRootsThisSwing.Add(root);
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
