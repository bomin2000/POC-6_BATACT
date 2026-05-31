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
            waveText.text = "웨이브 없음";
            return;
        }

        int index = spawner.CurrentWaveIndex;
        if (index >= spawner.Waves.Length)
        {
            waveText.text = "모든 웨이브 완료!";
            return;
        }

        string waveName = spawner.Waves[index].waveName;
        int remainingToSpawn = spawner.EnemiesRemainingToSpawn;
        int alive = spawner.AliveEnemiesCount;
        int totalRemaining = remainingToSpawn + alive;

        // Simplify UI to show only Name and Enemies Left
        waveText.text = $"<color=#FFD700>{waveName}</color>\n<size=80%>남은 적: {totalRemaining}</size>";
    }
}
