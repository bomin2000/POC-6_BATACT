using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool respawnOnDeath = true;
    [SerializeField] private float respawnDelay = 1f;
    [SerializeField] private Transform respawnPoint;

    [Header("Damage Response")]
    [SerializeField] private float invulnerableSeconds = 0.75f;
    [SerializeField] private float knockbackControlLockSeconds = 0.16f;
    [SerializeField] private float minimumDamageInterval = 0.15f;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer[] flashRenderers;
    [SerializeField] private Color hurtColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float flashInterval = 0.08f;

    [Header("Events")]
    public UnityEvent<float, float> HealthChanged;
    public UnityEvent Damaged;
    public UnityEvent Died;
    public UnityEvent Respawned;

    private Rigidbody2D body;
    private PlayerTopDownMovement movement;
    private DualBladeWeaponController weaponController;
    private Collider2D[] colliders;
    private Color[] originalColors;
    private Vector3 initialPosition;
    private float invulnerableTimer;
    private float lastDamageTime = -999f;
    private bool isDead;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsInvulnerable => invulnerableTimer > 0f;
    public bool IsDead => isDead;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerTopDownMovement>();
        weaponController = GetComponent<DualBladeWeaponController>();
        colliders = GetComponentsInChildren<Collider2D>();
        initialPosition = transform.position;

        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            flashRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        originalColors = new Color[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            originalColors[i] = flashRenderers[i].color;
        }

        CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Update()
    {
        if (invulnerableTimer > 0f)
        {
            invulnerableTimer -= Time.deltaTime;
        }
    }

    public bool IsHurtboxCollider(Collider2D candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (weaponController != null && weaponController.ContainsWeaponCollider(candidate))
        {
            return false;
        }

        return candidate.GetComponentInParent<PlayerHealth>() == this;
    }

    public bool TryTakeDamage(float damage, Vector2 impulse, GameObject source)
    {
        if (isDead || damage <= 0f || IsInvulnerable || Time.time - lastDamageTime < minimumDamageInterval)
        {
            return false;
        }

        lastDamageTime = Time.time;
        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0f, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        Damaged?.Invoke();

        if (movement != null)
        {
            movement.ApplyExternalKnockback(impulse, knockbackControlLockSeconds);
        }
        else if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.AddForce(impulse, ForceMode2D.Impulse);
        }

        if (CurrentHealth <= 0f)
        {
            Die();
        }
        else
        {
            invulnerableTimer = invulnerableSeconds;
            StartCoroutine(InvulnerableFlashRoutine(invulnerableSeconds));
        }

        return true;
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        invulnerableTimer = 0f;
        Died?.Invoke();

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        SetCollidersEnabled(false);
        SetRenderersEnabled(false);

        if (respawnOnDeath)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 targetPosition = respawnPoint != null ? respawnPoint.position : initialPosition;
        transform.position = targetPosition;
        CurrentHealth = maxHealth;
        isDead = false;

        if (body != null)
        {
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
        }

        SetCollidersEnabled(true);
        SetRenderersEnabled(true);

        invulnerableTimer = invulnerableSeconds;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        Respawned?.Invoke();
        StartCoroutine(InvulnerableFlashRoutine(invulnerableSeconds));
    }

    private IEnumerator InvulnerableFlashRoutine(float seconds)
    {
        float endTime = Time.time + seconds;
        bool showHurt = true;

        while (Time.time < endTime && !isDead)
        {
            SetRendererColors(showHurt ? hurtColor : (Color?)null);
            showHurt = !showHurt;
            yield return new WaitForSeconds(flashInterval);
        }

        SetRendererColors(null);
    }

    private void SetRendererColors(Color? color)
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
            {
                flashRenderers[i].color = color ?? originalColors[i];
            }
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
            {
                flashRenderers[i].enabled = enabled;
            }
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabled;
            }
        }
    }
}
