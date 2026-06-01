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
        if (hasTriggered && isOneShot)
        {
            return;
        }

        if (collision.CompareTag(targetTag))
        {
            if (TutorialHintUI.Instance != null)
            {
                TutorialHintUI.Instance.ShowHint(hintText, duration);
            }
            else
            {
                Debug.LogWarning("TutorialHintTrigger2D: TutorialHintUI.Instance is null. Cannot show hint.");
            }
            
            hasTriggered = true;
        }
    }
}
