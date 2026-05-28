using UnityEngine;

[System.Serializable]
public struct HitReactionData
{
    [Header("Reaction")]
    public HitReactionType reactionType;
    public float damage;
    public float knockbackForce;
    public float stunSeconds;

    [Header("Feel")]
    [Tooltip("적용 직전 넉백 벡터에 더하는 보조 충격량입니다. 사이드뷰 띄우기나 탑다운 미세 보정에 사용합니다.")]
    public Vector2 extraImpulse;

    public Vector2 BuildImpulse(Vector2 attackerForward, Vector2 attackerPosition, Vector2 targetPosition)
    {
        Vector2 direction;

        switch (reactionType)
        {
            case HitReactionType.PullToPlayer:
                direction = attackerPosition - targetPosition;
                break;

            case HitReactionType.StunLock:
                // 가위는 이동 제한 손맛이 핵심이므로 밀어내기는 낮게 쓰고 경직 시간을 크게 둡니다.
                direction = targetPosition - attackerPosition;
                break;

            case HitReactionType.Knockback:
            default:
                direction = attackerForward;
                break;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }

        return direction.normalized * knockbackForce + extraImpulse;
    }
}
