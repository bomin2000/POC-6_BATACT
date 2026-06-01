using UnityEngine;
using UnityEngine.Events;

public sealed class EnemyPart : MonoBehaviour, IWeaponHitReceiver
{
    [Header("Part Health")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private SpriteRenderer flashRenderer;
    [SerializeField] private Color hitColor = Color.white;

    [Header("Events")]
    public UnityEvent<float, float> HealthChanged;
    public UnityEvent OnBroken;

    private float currentHealth;
    private bool isBroken;
    private Color originalColor;

    public bool IsBroken => isBroken;

    private void Awake()
    {
        currentHealth = maxHealth;
        
        if (flashRenderer != null)
        {
            originalColor = flashRenderer.color;
        }

        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ReceiveWeaponHit(HitReactionData reaction, Vector2 impulse, GameObject source)
    {
        if (isBroken) return;

        // Apply part damage multiplier
        float multiplier = reaction.partDamageMultiplier > 0f ? reaction.partDamageMultiplier : 1f;
        float finalDamage = reaction.damage * multiplier;

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (flashRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        if (currentHealth <= 0f)
        {
            BreakPart();
        }
    }

    private void BreakPart()
    {
        if (isBroken) return;
        isBroken = true;

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        OnBroken?.Invoke();
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        if (flashRenderer == null) yield break;

        flashRenderer.color = hitColor;
        yield return new WaitForSeconds(0.05f);
        
        if (!isBroken && flashRenderer != null)
        {
            flashRenderer.color = originalColor;
        }
    }
}
