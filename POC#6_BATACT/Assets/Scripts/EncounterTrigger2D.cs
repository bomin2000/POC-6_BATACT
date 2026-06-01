using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EncounterSpawnPoint
{
    [Tooltip("스폰할 적 프리팹")]
    public GameObject enemyPrefab;
    
    [Tooltip("스폰될 정확한 위치 (Transform)")]
    public Transform spawnLocation;
    
    [Tooltip("트리거 발동 후 스폰까지의 지연 시간 (0이면 즉시)")]
    public float delay;
}

[RequireComponent(typeof(Collider2D))]
public class EncounterTrigger2D : MonoBehaviour
{
    [Header("Encounter Settings")]
    public string encounterName = "New Encounter";
    public bool isOneTimeOnly = true;

    [Tooltip("에셋으로 저장해둔 적 무리(포메이션) 프리셋")]
    public EncounterFormationProfile formationProfile;

    [Header("Targeting")]
    [Tooltip("비워두면 런타임에 DualBladeWeaponController를 찾아 플레이어를 타겟으로 삼습니다.")]
    public Transform target;

    private bool hasTriggered = false;
    private Collider2D triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        ResolveTarget();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggered && isOneTimeOnly) return;

        PlayerHealth player = collision.GetComponentInParent<PlayerHealth>();
        if (player != null && player.IsHurtboxCollider(collision))
        {
            TriggerEncounter();
        }
    }

    public void TriggerEncounter()
    {
        if (hasTriggered && isOneTimeOnly) return;
        
        hasTriggered = true;
        Debug.Log($"[Encounter] '{encounterName}' 발동!");

        // 프로필 스폰 실행
        if (formationProfile != null && formationProfile.spawns != null)
        {
            foreach (var data in formationProfile.spawns)
            {
                if (data.enemyPrefab == null) continue;
                
                Vector3 worldPos = transform.position + (Vector3)data.localOffset;
                
                if (data.delay > 0f)
                {
                    StartCoroutine(SpawnWithDelay(data.enemyPrefab, worldPos, data.delay));
                }
                else
                {
                    SpawnEnemy(data.enemyPrefab, worldPos);
                }
            }
        }
    }

    private IEnumerator SpawnWithDelay(GameObject prefab, Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnEnemy(prefab, position);
    }

    private void SpawnEnemy(GameObject prefab, Vector3 position)
    {
        GameObject enemyObj = Instantiate(prefab, position, Quaternion.identity);

        // 타겟 주입 로직 (기존 EnemySpawner와 동일)
        EnemySideViewChaser chaser = enemyObj.GetComponent<EnemySideViewChaser>();
        if (chaser != null && target != null)
        {
            chaser.SetTarget(target);
            chaser.RefreshEnemyCollisionIgnores();
        }

        EnemyRangedShooter shooter = enemyObj.GetComponent<EnemyRangedShooter>();
        if (shooter != null && target != null)
        {
            shooter.SetTarget(target);
        }
    }

    private void ResolveTarget()
    {
        if (target != null) return;
        DualBladeWeaponController weaponController = FindFirstObjectByType<DualBladeWeaponController>();
        if (weaponController != null)
        {
            target = weaponController.transform;
        }
    }

    private void OnDrawGizmos()
    {
        // 트리거 범위 표시 (BoxCollider2D 기준)
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // 주황색 반투명
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireCube(box.offset, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        DrawProfileGizmos();
    }

    private void DrawProfileGizmos()
    {
        if (formationProfile == null || formationProfile.spawns == null) return;

        foreach (var data in formationProfile.spawns)
        {
            Vector3 worldPos = transform.position + (Vector3)data.localOffset;
            
            Gizmos.color = new Color(1f, 0.8f, 0f, 1f); // 노란색(프로필 스폰)
            Gizmos.DrawWireSphere(worldPos, 0.5f);
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
            Gizmos.DrawLine(transform.position, worldPos);
            
            // X 표시
            Gizmos.DrawLine(worldPos - Vector3.right * 0.5f, worldPos + Vector3.right * 0.5f);
            Gizmos.DrawLine(worldPos - Vector3.up * 0.5f, worldPos + Vector3.up * 0.5f);
        }
    }
}
