using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private float damage = 12f;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private Vector2 extraImpulse = new Vector2(0f, 2f);
    [SerializeField] private float damageCooldown = 0.6f;

    private float lastDamageTime = -999f;

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (Time.time - lastDamageTime < damageCooldown)
        {
            return;
        }

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead)
        {
            return;
        }

        if (!player.IsHurtboxCollider(other))
        {
            return;
        }

        Vector2 direction = player.transform.position - transform.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }

        Vector2 impulse = direction.normalized * knockbackForce + extraImpulse;
        if (player.TryTakeDamage(damage, impulse, gameObject))
        {
            lastDamageTime = Time.time;
        }
    }
}
