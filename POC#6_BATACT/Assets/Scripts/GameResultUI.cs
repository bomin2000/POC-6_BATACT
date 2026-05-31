using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameResultUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Image bgImage;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private StageGoalTrigger goal;
    [SerializeField] private bool stopTimeOnResult = false;

    private bool hasTriggered = false;

    private void Awake()
    {
        if (resultText == null)
            resultText = GetComponentInChildren<TextMeshProUGUI>();
            
        if (bgImage == null)
            bgImage = GetComponent<Image>();

        if (resultText != null)
            resultText.enabled = false;
            
        if (bgImage != null)
            bgImage.enabled = false;
    }

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerHealth>();
        }
        if (goal == null)
        {
            goal = FindFirstObjectByType<StageGoalTrigger>();
        }
    }

    private void Update()
    {
        if (hasTriggered || resultText == null)
        {
            return;
        }

        if (goal == null)
        {
            goal = FindFirstObjectByType<StageGoalTrigger>();
        }
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerHealth>();
        }

        if (player != null && player.IsDead)
        {
            TriggerResult("<color=red>GAME OVER</color>");
        }
        else if (goal != null && goal.IsReached)
        {
            TriggerResult("<color=#00FF00>STAGE CLEAR!</color>");
        }
    }

    private void TriggerResult(string message)
    {
        hasTriggered = true;
        
        if (resultText != null)
        {
            resultText.text = message;
            resultText.enabled = true;
        }

        if (bgImage != null)
        {
            bgImage.enabled = true;
        }

        if (stopTimeOnResult)
        {
            Time.timeScale = 0f;
        }
    }

    private void OnDestroy()
    {
        if (hasTriggered && stopTimeOnResult && Time.timeScale == 0f)
        {
            Time.timeScale = 1f; // Just in case UI is destroyed
        }
    }
}
