using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameResultUI : MonoBehaviour
{
    public enum GameState { Playing, GameOver, StageClear }

    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Image bgImage;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private StageGoalTrigger goal;
    
    [Header("Options")]
    [SerializeField] private bool stopTimeOnResult = true;
    [SerializeField] private bool blockPlayerInputOnGameOver = true;

    private GameState currentState = GameState.Playing;
    private PlayerTopDownMovement playerMovement;
    private DualBladeWeaponController weaponController;

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

        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerTopDownMovement>();
            weaponController = player.GetComponent<DualBladeWeaponController>();
        }
    }

    private void Update()
    {
        CheckForRestartInput();

        if (currentState != GameState.Playing)
        {
            return;
        }

        // Game Over condition
        if (player != null && player.IsDead)
        {
            SetGameState(GameState.GameOver);
        }
        // Clear condition
        else if (goal != null && goal.IsReached)
        {
            SetGameState(GameState.StageClear);
        }
    }

    private void SetGameState(GameState newState)
    {
        if (currentState != GameState.Playing)
        {
            return; // Once settled, do not allow override
        }

        currentState = newState;

        if (currentState == GameState.GameOver)
        {
            ShowResultUI("<color=red>GAME OVER</color>");
            if (blockPlayerInputOnGameOver && playerMovement != null)
            {
                playerMovement.enabled = false;
                if (weaponController != null) weaponController.enabled = false;
            }
        }
        else if (currentState == GameState.StageClear)
        {
            ShowResultUI("<color=#00FF00>STAGE CLEAR!</color>");
        }

        if (stopTimeOnResult)
        {
            Time.timeScale = 0f;
        }
    }

    private void ShowResultUI(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;
            resultText.enabled = true;
        }

        if (bgImage != null)
        {
            bgImage.enabled = true;
        }
    }

    private void CheckForRestartInput()
    {
        // R key restart (works even if timeScale is 0)
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (currentState != GameState.Playing && stopTimeOnResult && Time.timeScale == 0f)
        {
            Time.timeScale = 1f; // Just in case UI is destroyed
        }
    }
}
