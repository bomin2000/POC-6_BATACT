using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameRestartUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private Button restartButton;

    private void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(restartKey))
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Reset time scale in case it was paused
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
