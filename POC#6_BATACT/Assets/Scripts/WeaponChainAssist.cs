using UnityEngine;

[RequireComponent(typeof(DualBladeWeaponController))]
public sealed class WeaponChainAssist : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode assistKey = KeyCode.Mouse1;
    [SerializeField] private float assistCooldown = 0.5f;

    [Header("Targeting")]
    [SerializeField] private float scanRadius = 8f;
    [SerializeField] private float closeThreshold = 4f;
    [SerializeField] private LayerMask enemyLayerMask = Physics2D.AllLayers;

    [Header("Debug")]
    [SerializeField] private bool debugAssist = true;
    [SerializeField, TextArea] private string lastAssistReason;

    private DualBladeWeaponController weaponController;
    private float lastAssistTime = -999f;
    private Collider2D[] scanResults = new Collider2D[16];

    private void Awake()
    {
        weaponController = GetComponent<DualBladeWeaponController>();
    }

    private WeaponState lastFrameState;

    private void Update()
    {
        bool justCaughtBoomerang = (lastFrameState == WeaponState.BareHand && weaponController.CurrentState == WeaponState.Boomerang);

        if (Input.GetKeyDown(assistKey) || (Input.GetKey(assistKey) && justCaughtBoomerang))
        {
            TryExecuteAssist();
        }
        else if (Input.GetKey(assistKey))
        {
            if (weaponController.CanAutoAttackNow)
            {
                TryExecuteAssist();
            }
            weaponController.TryAutoAttackFromAssist();
        }

        lastFrameState = weaponController.CurrentState;
    }

    private void TryExecuteAssist()
    {
        if (Time.time < lastAssistTime + assistCooldown)
        {
            return; // Don't spam debug reasons during hold
        }

        if (weaponController.CurrentState == WeaponState.BareHand)
        {
            return;
        }

        StatusTagController bestTarget = FindBestTarget();

        if (bestTarget == null)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, bestTarget.transform.position);

        // Rule 1: Pulled + Close => Scissors Primary
        if (bestTarget.HasTag(StatusTag.Pulled) && distance <= closeThreshold)
        {
            TryTransitionTo(WeaponState.Scissors, false, "Target is Pulled and Close -> Scissors Execution.");
            return;
        }

        // Rule 2: GuardBroken + Close => Scissors Secondary
        if (bestTarget.HasTag(StatusTag.GuardBroken) && distance <= closeThreshold)
        {
            TryTransitionTo(WeaponState.Scissors, true, "Target is GuardBroken and Close -> Scissors Finisher.");
            return;
        }

        // Rule 3: Exposed + Mid/Far => Spear Secondary (Charge)
        if (bestTarget.HasTag(StatusTag.Exposed) && distance > closeThreshold)
        {
            TryTransitionTo(WeaponState.Spear, true, "Target is Exposed and Far -> Spear Charge.");
            return;
        }

        // Rule 4: Far => Boomerang Primary
        if (distance > closeThreshold)
        {
            TryTransitionTo(WeaponState.Boomerang, false, "Target is Far -> Boomerang Throw.");
            return;
        }

        // Rule 5: Close, but no specific tag matched => Default to Scissors Primary
        TryTransitionTo(WeaponState.Scissors, false, "Target is Close (No Tag) -> Scissors Attack.");
    }

    private void TryTransitionTo(WeaponState state, bool isSecondary, string reason)
    {
        if (weaponController.CurrentState == state && !isSecondary)
        {
            SetDebugReason("Already in correct state. Yielding to normal combo.");
            return; // Yield to weaponController.TryAutoAttackFromAssist()
        }

        SetDebugReason(reason);
        lastAssistTime = Time.time;
        
        if (weaponController.CurrentState != state)
        {
            weaponController.RequestMorph(state);
        }
        
        weaponController.ExecuteAssistAttack(isSecondary);
    }

    private StatusTagController FindBestTarget()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, scanRadius, scanResults, enemyLayerMask);
        if (count == 0) return null;

        Vector2 aimDir = weaponController.AimDirection;
        
        StatusTagController bestReceiver = null;
        float bestScore = -float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var receiver = scanResults[i].GetComponentInParent<StatusTagController>();
            if (receiver == null) continue;

            // Optional: check if dead
            var oldReceiver = receiver.GetComponent<PrototypeEnemyHitReceiver>();
            if (oldReceiver != null && oldReceiver.IsDead) continue;
            var body = receiver.GetComponent<EnemyBody>();
            if (body != null && body.IsDead) continue;

            Vector2 toTarget = (receiver.transform.position - transform.position);
            float dist = toTarget.magnitude;
            if (dist < 0.1f) dist = 0.1f;
            Vector2 dirToTarget = toTarget / dist;

            // Dot product to check if target is in front of aim
            float dot = Vector2.Dot(aimDir, dirToTarget);
            
            // Only consider targets roughly in a 180 degree cone, unless very close
            if (dot < 0f && dist > 2f) continue;
            
            float score = (dot * 10f) - dist;
            if (receiver.HasAnyTag())
            {
                score += 50f; // Strongly prefer tagged enemies
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestReceiver = receiver;
            }
        }

        return bestReceiver;
    }

    private void SetDebugReason(string reason)
    {
        lastAssistReason = reason;
        if (debugAssist)
        {
            Debug.Log($"[WeaponChainAssist] {reason}");
        }
    }
}
