using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform target;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float spawnHeightOffset = 1f;
    [SerializeField] private float minimumSpawnSpacing = 1.75f;
    [SerializeField] private int spawnPositionAttempts = 8;

    [Header("Rules")]
    [SerializeField] private int maxAlive = 3;
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float spawnInterval = 2.5f;
    [SerializeField] private bool spawnOnStart = true;

    private readonly List<PrototypeEnemyHitReceiver> aliveEnemies = new List<PrototypeEnemyHitReceiver>();
    private Coroutine spawnRoutine;

    private void Start()
    {
        ResolveTarget();

        if (spawnOnStart)
        {
            spawnRoutine = StartCoroutine(SpawnRoutine());
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

    public void SpawnOne()
    {
        CleanupDeadEntries();

        if (enemyPrefab == null || aliveEnemies.Count >= maxAlive)
        {
            return;
        }

        Vector3 position = GetSpawnPosition();
        GameObject enemyObject = Instantiate(enemyPrefab, position, Quaternion.identity);

        EnemySideViewChaser chaser = enemyObject.GetComponent<EnemySideViewChaser>();
        if (chaser != null && target != null)
        {
            chaser.SetTarget(target);
            chaser.RefreshEnemyCollisionIgnores();
        }

        PrototypeEnemyHitReceiver receiver = enemyObject.GetComponent<PrototypeEnemyHitReceiver>();
        if (receiver != null)
        {
            aliveEnemies.Add(receiver);
            receiver.Died.AddListener(() => aliveEnemies.Remove(receiver));
        }
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (enabled)
        {
            SpawnOne();
            yield return new WaitForSeconds(spawnInterval);
        }
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
