using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StageGoalTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    
    public bool IsReached { get; private set; }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsReached) return;

        PlayerHealth health = collision.GetComponentInParent<PlayerHealth>();
        if (health != null && health.IsHurtboxCollider(collision))
        {
            IsReached = true;
            Debug.Log("GOAL REACHED by PlayerHealth!");
        }
    }
}
