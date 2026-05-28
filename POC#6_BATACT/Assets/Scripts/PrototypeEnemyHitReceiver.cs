using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PrototypeEnemyHitReceiver : MonoBehaviour, IWeaponHitReceiver
{
    [SerializeField] private float hitStopSeconds = 0.035f;
    [SerializeField] private SpriteRenderer flashRenderer;
    [SerializeField] private Color hitColor = Color.white;

    private Rigidbody2D body;
    private Color originalColor;
    private Coroutine stunRoutine;

    public float CurrentHealth { get; private set; } = 100f;
    public bool IsStunned { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (flashRenderer != null)
        {
            originalColor = flashRenderer.color;
        }
    }

    public void ReceiveWeaponHit(HitReactionData reaction, Vector2 impulse, GameObject source)
    {
        CurrentHealth -= reaction.damage;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.AddForce(impulse, ForceMode2D.Impulse);
        }

        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
        }

        stunRoutine = StartCoroutine(StunRoutine(reaction.stunSeconds));
        StartCoroutine(FlashRoutine());

        if (hitStopSeconds > 0f)
        {
            StartCoroutine(HitStopRoutine(hitStopSeconds));
        }
    }

    private IEnumerator StunRoutine(float seconds)
    {
        IsStunned = seconds > 0f;
        yield return new WaitForSeconds(seconds);
        IsStunned = false;
        stunRoutine = null;
    }

    private IEnumerator FlashRoutine()
    {
        if (flashRenderer == null)
        {
            yield break;
        }

        flashRenderer.color = hitColor;
        yield return new WaitForSeconds(0.05f);
        flashRenderer.color = originalColor;
    }

    private IEnumerator HitStopRoutine(float seconds)
    {
        float previousScale = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(seconds);
        Time.timeScale = previousScale;
    }
}
