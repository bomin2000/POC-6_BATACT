using UnityEngine;
using System.Collections;

public class ProjectileWarningMarker : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color startColor = new Color(1f, 0f, 0f, 0f);
    [SerializeField] private Color endColor = new Color(1f, 0f, 0f, 0.6f);
    [SerializeField] private Vector3 maxScale = new Vector3(1.5f, 0.4f, 1f); // Ellipse on the ground

    private void Awake()
    {
        if (sr == null)
        {
            sr = GetComponentInChildren<SpriteRenderer>();
        }
    }

    public void ShowWarning(float duration)
    {
        if (sr != null) sr.color = startColor;
        transform.localScale = Vector3.zero;
        StartCoroutine(WarningRoutine(duration));
    }

    private IEnumerator WarningRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (sr != null)
            {
                sr.color = Color.Lerp(startColor, endColor, t);
            }
            transform.localScale = Vector3.Lerp(Vector3.zero, maxScale, t);
            yield return null;
        }
        
        // Blink or stay at max for a split second before destroy
        if (sr != null) sr.color = endColor;
        transform.localScale = maxScale;
        yield return new WaitForSeconds(0.1f);
        
        Destroy(gameObject);
    }
}
