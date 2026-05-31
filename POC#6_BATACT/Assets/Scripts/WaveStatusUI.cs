using UnityEngine;
using TMPro;

public class WaveStatusUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private EnemySpawner spawner;

    private void Awake()
    {
        if (waveText == null)
        {
            waveText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<EnemySpawner>();
        }
    }

    private void Update()
    {
        if (spawner == null || waveText == null)
        {
            return;
        }

        if (!spawner.HasWaves)
        {
            waveText.text = "No Waves";
            return;
        }

        int index = spawner.CurrentWaveIndex;
        if (index >= spawner.Waves.Length)
        {
            waveText.text = "All Waves Completed";
            return;
        }

        string waveName = spawner.Waves[index].waveName;
        int remainingToSpawn = spawner.EnemiesRemainingToSpawn;
        int alive = spawner.AliveEnemiesCount;
        int totalRemaining = remainingToSpawn + alive;
        string state = spawner.CurrentState;

        // If waiting to clear, highlight it
        if (state.Contains("cleared") || state.Contains("Delay"))
        {
            waveText.text = $"<color=#FFD700>{waveName}</color>\n<size=70%>Status: {state}</size>";
        }
        else
        {
            waveText.text = $"<color=#FFD700>{waveName}</color>\n<size=80%>Enemies Left: {totalRemaining}</size>\n<size=60%>{state}</size>";
        }
    }
}
