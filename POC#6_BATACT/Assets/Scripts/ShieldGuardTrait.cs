using UnityEngine;

[RequireComponent(typeof(EnemyPart))]
public sealed class ShieldGuardTrait : MonoBehaviour
{
    [Header("Guard Settings")]
    [Tooltip("전면에서 받은 피해를 얼마나 감소시킬 것인가 (0.0 ~ 1.0)")]
    [SerializeField] private float damageReduction = 0.8f;
    
    [Header("Visual")]
    [SerializeField] private GameObject shieldVisualRoot;

    private EnemyPart shieldPart;
    private StatusTagController ownerTags;
    private Transform ownerTransform;

    public float DamageReduction => damageReduction;

    private void Awake()
    {
        shieldPart = GetComponent<EnemyPart>();
        shieldPart.OnBroken.AddListener(HandleShieldBroken);

        // Find the main body owner
        ownerTransform = transform.parent;
        if (ownerTransform != null)
        {
            ownerTags = ownerTransform.GetComponent<StatusTagController>();
        }
    }

    public bool IsGuarding(Vector2 attackerPosition)
    {
        if (shieldPart.IsBroken || ownerTransform == null)
        {
            return false;
        }

        // Check if the attacker is in front of the enemy
        float facingSign = 1f;
        var chaser = ownerTransform.GetComponent<EnemySideViewChaser>();
        if (chaser != null)
        {
            facingSign = chaser.FacingSign;
        }

        float dirToAttackerX = attackerPosition.x - ownerTransform.position.x;
        
        // Add a generous tolerance (e.g. 1.0 units) so that if the player is rubbing against the enemy, 
        // they don't accidentally bypass the shield just because their center point crossed the enemy's center.
        bool isFrontal = (facingSign > 0f && dirToAttackerX > -1.0f) || 
                         (facingSign < 0f && dirToAttackerX < 1.0f);

        return isFrontal;
    }

    private void HandleShieldBroken()
    {
        if (shieldVisualRoot != null)
        {
            shieldVisualRoot.SetActive(false);
        }

        if (ownerTags != null)
        {
            // Apply GuardBroken for 5 seconds when shield breaks
            ownerTags.AddTag(StatusTag.GuardBroken, 5f);
        }
    }

    private void Update()
    {
        if (shieldPart.IsBroken || ownerTransform == null) return;

        var chaser = ownerTransform.GetComponent<EnemySideViewChaser>();
        if (chaser != null)
        {
            float facingSign = chaser.FacingSign;

            // Automatically manage the shield's facing if it is directly attached to the root.
            if (transform.parent == ownerTransform)
            {
                Vector3 localPos = transform.localPosition;
                localPos.x = Mathf.Abs(localPos.x) * facingSign;
                transform.localPosition = localPos;

                Vector3 localScale = transform.localScale;
                localScale.x = Mathf.Abs(localScale.x) * facingSign;
                transform.localScale = localScale;
            }
        }
    }

    private void OnDestroy()
    {
        if (shieldPart != null)
        {
            shieldPart.OnBroken.RemoveListener(HandleShieldBroken);
        }
    }
}
