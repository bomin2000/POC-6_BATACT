using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnemyWaveDefinition
{
    public string waveName;
    public int basicEnemyCount;
    public int fastEnemyCount;
    public int rangedEnemyCount;
    
    [Tooltip("적 하나 스폰 후 대기 시간")]
    public float spawnInterval;
    [Tooltip("이 웨이브가 시작되기 전 대기 시간")]
    public float waveDelay;
    [Tooltip("이 웨이브의 적이 전멸한 후 다음 웨이브까지 대기 시간")]
    public float nextWaveDelay;
}

public sealed class EnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject fastEnemyPrefab;
    [SerializeField] private GameObject rangedEnemyPrefab;
    [SerializeField] private Transform target;

    [Header("Wave Settings")]
    [SerializeField] private EnemyWaveDefinition[] waves;
    [SerializeField] private bool loopWaves = false;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float spawnHeightOffset = 1f;
    [SerializeField] private float minimumSpawnSpacing = 1.75f;
    [SerializeField] private int spawnPositionAttempts = 8;

    [Header("Rules")]
    [SerializeField] private int maxAlive = 3;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private int debugCurrentWaveIndex;
    [SerializeField] private int debugEnemiesRemainingToSpawn;
    [SerializeField] private int debugAliveEnemiesCount;
    [SerializeField] private string debugState;

    public int CurrentWaveIndex => debugCurrentWaveIndex;
    public int EnemiesRemainingToSpawn => debugEnemiesRemainingToSpawn;
    public int AliveEnemiesCount => debugAliveEnemiesCount;
    public string CurrentState => debugState;
    public EnemyWaveDefinition[] Waves => waves;
    public bool HasWaves => waves != null && waves.Length > 0;

    private readonly List<PrototypeEnemyHitReceiver> aliveEnemies = new List<PrototypeEnemyHitReceiver>();
    private Coroutine spawnRoutine;

    private void Start()
    {
        ResolveTarget();

        // Create some default waves if empty, just so it works immediately
        if (waves == null || waves.Length == 0)
        {
            waves = new EnemyWaveDefinition[]
            {
                new EnemyWaveDefinition { waveName = "Wave 1", basicEnemyCount = 3, spawnInterval = 1f, waveDelay = 1f, nextWaveDelay = 2f },
                new EnemyWaveDefinition { waveName = "Wave 2", basicEnemyCount = 1, fastEnemyCount = 2, spawnInterval = 1f, waveDelay = 1f, nextWaveDelay = 2f },
                new EnemyWaveDefinition { waveName = "Wave 3", basicEnemyCount = 2, fastEnemyCount = 1, rangedEnemyCount = 2, spawnInterval = 1f, waveDelay = 1f, nextWaveDelay = 2f }
            };
        }

        if (spawnOnStart)
        {
            spawnRoutine = StartCoroutine(WaveRoutine());
        }
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator WaveRoutine()
    {
        debugCurrentWaveIndex = 0;

        while (true)
        {
            if (waves == null || waves.Length == 0)
            {
                debugState = "No Waves Defined";
                yield break;
            }

            if (debugCurrentWaveIndex >= waves.Length)
            {
                if (loopWaves)
                {
                    debugCurrentWaveIndex = 0;
                }
                else
                {
                    debugState = "All Waves Completed";
                    yield break;
                }
            }

            EnemyWaveDefinition currentWave = waves[debugCurrentWaveIndex];
            debugState = $"Waiting for Wave Delay ({currentWave.waveName})";
            yield return new WaitForSeconds(currentWave.waveDelay);

            debugState = $"Spawning Wave ({currentWave.waveName})";
            
            // Prepare spawn list
            List<GameObject> enemiesToSpawn = new List<GameObject>();
            for (int i = 0; i < currentWave.basicEnemyCount; i++) if (enemyPrefab != null) enemiesToSpawn.Add(enemyPrefab);
            for (int i = 0; i < currentWave.fastEnemyCount; i++) if (fastEnemyPrefab != null) enemiesToSpawn.Add(fastEnemyPrefab);
            for (int i = 0; i < currentWave.rangedEnemyCount; i++) if (rangedEnemyPrefab != null) enemiesToSpawn.Add(rangedEnemyPrefab);

            // Shuffle
            for (int i = 0; i < enemiesToSpawn.Count; i++)
            {
                GameObject temp = enemiesToSpawn[i];
                int randomIndex = Random.Range(i, enemiesToSpawn.Count);
                enemiesToSpawn[i] = enemiesToSpawn[randomIndex];
                enemiesToSpawn[randomIndex] = temp;
            }

            debugEnemiesRemainingToSpawn = enemiesToSpawn.Count;

            // Spawn one by one
            for (int i = 0; i < enemiesToSpawn.Count; i++)
            {
                // Wait if max alive reached
                while (true)
                {
                    CleanupDeadEntries();
                    debugAliveEnemiesCount = aliveEnemies.Count;
                    if (aliveEnemies.Count < maxAlive)
                    {
                        break;
                    }
                    debugState = "Waiting for Max Alive Room";
                    yield return new WaitForSeconds(0.5f);
                }

                debugState = $"Spawning Wave ({currentWave.waveName})";
                SpawnSpecificEnemy(enemiesToSpawn[i]);
                debugEnemiesRemainingToSpawn--;

                if (i < enemiesToSpawn.Count - 1 && currentWave.spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(currentWave.spawnInterval);
                }
            }

            // Wait for all to die
            debugState = "Waiting for enemies to be cleared";
            while (true)
            {
                CleanupDeadEntries();
                debugAliveEnemiesCount = aliveEnemies.Count;
                if (aliveEnemies.Count == 0)
                {
                    break;
                }
                yield return new WaitForSeconds(0.5f);
            }

            debugState = "Wave Cleared! Waiting for next wave...";
            yield return new WaitForSeconds(currentWave.nextWaveDelay);

            debugCurrentWaveIndex++;
        }
    }

    private void SpawnSpecificEnemy(GameObject prefabToSpawn)
    {
        CleanupDeadEntries();

        Vector3 position = GetSpawnPosition();
        GameObject enemyObject = Instantiate(prefabToSpawn, position, Quaternion.identity);

        EnemySideViewChaser chaser = enemyObject.GetComponent<EnemySideViewChaser>();
        if (chaser != null && target != null)
        {
            chaser.SetTarget(target);
            chaser.RefreshEnemyCollisionIgnores();
        }

        EnemyRangedShooter shooter = enemyObject.GetComponent<EnemyRangedShooter>();
        if (shooter != null && target != null)
        {
            shooter.SetTarget(target);
        }

        PrototypeEnemyHitReceiver receiver = enemyObject.GetComponent<PrototypeEnemyHitReceiver>();
        if (receiver != null)
        {
            aliveEnemies.Add(receiver);
            receiver.Died.AddListener(() => aliveEnemies.Remove(receiver));
        }
        
        debugAliveEnemiesCount = aliveEnemies.Count;
    }

    private void ResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        DualBladeWeaponController weaponController = FindFirstObjectByType<DualBladeWeaponController>();
        if (weaponController != null)
        {
            target = weaponController.transform;
        }
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 bestPosition = target != null ? target.position : transform.position;

        for (int attempt = 0; attempt < Mathf.Max(1, spawnPositionAttempts); attempt++)
        {
            Vector3 candidate = GetRawSpawnPosition();
            if (IsFarEnoughFromAliveEnemies(candidate))
            {
                return candidate;
            }

            bestPosition = candidate;
        }

        return bestPosition;
    }

    private Vector3 GetRawSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (point != null)
            {
                return point.position;
            }
        }

        Vector3 center = target != null ? target.position : transform.position;
        float direction = Random.value < 0.5f ? -1f : 1f;
        return center + new Vector3(direction * spawnRadius, spawnHeightOffset, 0f);
    }

    private bool IsFarEnoughFromAliveEnemies(Vector3 position)
    {
        if (minimumSpawnSpacing <= 0f)
        {
            return true;
        }

        CleanupDeadEntries();

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            if (Vector2.Distance(position, aliveEnemies[i].transform.position) < minimumSpawnSpacing)
            {
                return false;
            }
        }

        return true;
    }

    private void CleanupDeadEntries()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null || aliveEnemies[i].IsDead)
            {
                aliveEnemies.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(spawnPoints[i].position, 0.35f);
                }
            }

            return;
        }

        Vector3 center = target != null ? target.position : transform.position;
        Gizmos.DrawWireSphere(center + new Vector3(spawnRadius, spawnHeightOffset, 0f), 0.35f);
        Gizmos.DrawWireSphere(center + new Vector3(-spawnRadius, spawnHeightOffset, 0f), 0.35f);
    }
}
