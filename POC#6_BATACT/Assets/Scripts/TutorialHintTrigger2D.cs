using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialHintTrigger2D : MonoBehaviour
{
    [TextArea(3, 10)]
    public string hintText;
    public float duration = 4f;
    public bool isOneShot = true;
    public string targetTag = "Player";

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            if (TutorialHintUI.Instance != null)
            {
                Debug.Log($"[TutorialTrigger] Entered zone. Showing hint: {hintText}");
                TutorialHintUI.Instance.ShowTutorialHint(hintText);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            if (TutorialHintUI.Instance != null)
            {
                TutorialHintUI.Instance.ShowTutorialHint(hintText);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayer(collision))
        {
            if (TutorialHintUI.Instance != null)
            {
                Debug.Log($"[TutorialTrigger] Exited zone. Hiding hint: {hintText}");
                TutorialHintUI.Instance.HideTutorialHint(hintText);
            }

            if (isOneShot)
            {
                GetComponent<Collider2D>().enabled = false;
            }
        }
    }

    private bool IsPlayer(Collider2D collision)
    {
        // Tag check is fastest, but fallback to component check if tag is missing
        if (collision.CompareTag(targetTag)) return true;
        if (collision.GetComponent<PlayerHealth>() != null) return true;
        if (collision.GetComponent<PlayerTopDownMovement>() != null) return true;
        return false;
    }
}
