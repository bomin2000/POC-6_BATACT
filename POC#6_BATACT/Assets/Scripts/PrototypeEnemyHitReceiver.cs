using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PrototypeEnemyHitReceiver : MonoBehaviour, IWeaponHitReceiver
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathDestroyDelay = 0.05f;

    [Header("Fall Death")]
    [SerializeField] private bool dieBelowY = true;
    [SerializeField] private float deathY = -12f;

    [Header("Hit Feel")]
    [SerializeField] private float hitStopSeconds = 0.018f;
    [SerializeField] private float hitStopCooldown = 0.08f;
    [SerializeField] private SpriteRenderer flashRenderer;
    [SerializeField] private Color hitColor = Color.white;
    [SerializeField] private float knockbackDrag = 6f;
    [SerializeField] private float pullDrag = 9f;
    [SerializeField] private float stunLockDrag = 18f;
    [SerializeField] private float minimumHitStun = 0.04f;

    [Header("Events")]
    public UnityEvent<float, float> HealthChanged;
    public UnityEvent Died;

    private Rigidbody2D body;
    private EnemySideViewChaser chaser;
    private Collider2D[] colliders;
    private Color originalColor;
    private Coroutine stunRoutine;
    private Coroutine restoreDragRoutine;
    private bool isDead;
    private float originalLinearDamping;
    private static Coroutine activeHitStopRoutine;
    private static float lastHitStopTime = -999f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsStunned { get; private set; }
    public bool IsDead => isDead;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        chaser = GetComponent<EnemySideViewChaser>();
        colliders = GetComponentsInChildren<Collider2D>();

        if (body != null)
        {
            originalLinearDamping = body.linearDamping;
        }

        if (flashRenderer == null)
        {
            Transform visual = transform.Find("visualRoot") ?? transform.Find("body");
            if (visual != null) flashRenderer = visual.GetComponent<SpriteRenderer>();
            if (flashRenderer == null) flashRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (flashRenderer != null)
        {
            originalColor = flashRenderer.color;
        }

        CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Update()
    {
        if (!isDead && dieBelowY && transform.position.y <= deathY)
        {
            Kill();
        }
    }

    public void Kill()
    {
        CurrentHealth = 0f;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        Die();
    }

    public void ReceiveWeaponHit(HitReactionData reaction, Vector2 impulse, GameObject source)
    {
        if (isDead)
        {
            return;
        }

        CurrentHealth -= reaction.damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.AddForce(impulse, ForceMode2D.Impulse);
            ApplyReactionDrag(reaction);
        }

        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
        }

        stunRoutine = StartCoroutine(StunRoutine(Mathf.Max(minimumHitStun, reaction.stunSeconds)));
        StartCoroutine(FlashRoutine());

        if (hitStopSeconds > 0f && Time.unscaledTime - lastHitStopTime >= hitStopCooldown)
        {
            lastHitStopTime = Time.unscaledTime;

            if (activeHitStopRoutine != null)
            {
                StopCoroutine(activeHitStopRoutine);
            }

            activeHitStopRoutine = StartCoroutine(HitStopRoutine(hitStopSeconds));
        }

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    private IEnumerator StunRoutine(float seconds)
    {
        IsStunned = seconds > 0f;
        if (chaser != null)
        {
            chaser.SetExternalStun(IsStunned);
        }

        yield return new WaitForSeconds(seconds);

        IsStunned = false;
        if (chaser != null)
        {
            chaser.SetExternalStun(false);
        }

        stunRoutine = null;
    }

    private void ApplyReactionDrag(HitReactionData reaction)
    {
        if (body == null)
        {
            return;
        }

        if (restoreDragRoutine != null)
        {
            StopCoroutine(restoreDragRoutine);
        }

        switch (reaction.reactionType)
        {
            case HitReactionType.PullToPlayer:
                body.linearDamping = pullDrag;
                break;
            case HitReactionType.StunLock:
                body.linearDamping = stunLockDrag;
                break;
            case HitReactionType.Knockback:
            default:
                body.linearDamping = knockbackDrag;
                break;
        }

        float restoreDelay = Mathf.Max(0.08f, reaction.stunSeconds);
        restoreDragRoutine = StartCoroutine(RestoreDragRoutine(restoreDelay));
    }

    private IEnumerator RestoreDragRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (body != null && !isDead)
        {
            body.linearDamping = originalLinearDamping;
        }

        restoreDragRoutine = null;
    }

    private IEnumerator FlashRoutine()
    {
        if (flashRenderer == null) yield break;

        flashRenderer.color = hitColor;
        yield return new WaitForSeconds(0.05f);
        
        if (!isDead && flashRenderer != null)
        {
            flashRenderer.color = originalColor;
        }
    }

    private IEnumerator HitStopRoutine(float seconds)
    {
        float previousScale = Time.timeScale;
        Time.timeScale = Mathf.Min(previousScale, 0.25f);
        yield return new WaitForSecondsRealtime(seconds);
        Time.timeScale = previousScale;
        activeHitStopRoutine = null;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        IsStunned = true;

        if (chaser != null)
        {
            chaser.SetExternalStun(true);
            chaser.enabled = false;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Died?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject, deathDestroyDelay);
        }
    }
}
