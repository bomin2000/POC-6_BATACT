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

        // 안전장치: 기존 EnemySpawner가 씬에 켜져 있다면 중복 스폰 방지를 위해 강제로 끕니다.
        EnemySpawner oldSpawner = FindFirstObjectByType<EnemySpawner>();
        if (oldSpawner != null && oldSpawner.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[Encounter] '{encounterName}' - 씬에 기존 EnemySpawner가 켜져 있습니다. 중복 스폰 방지를 위해 해당 Spawner를 비활성화합니다.");
            oldSpawner.gameObject.SetActive(false);
        }
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
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is BoxCollider2D box)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // 주황색 반투명
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.offset, box.size);
                Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
                Gizmos.DrawWireCube(box.offset, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
                Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }

        DrawProfileGizmos(col != null ? col.bounds.center : transform.position);
    }

    private void DrawProfileGizmos(Vector3 centerPosition)
    {
        if (formationProfile == null || formationProfile.spawns == null || formationProfile.spawns.Length == 0)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(centerPosition, "No Formation Profile Assigned", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.red }, fontStyle = FontStyle.Bold, fontSize = 14 });
#endif
            return;
        }

        for (int i = 0; i < formationProfile.spawns.Length; i++)
        {
            var data = formationProfile.spawns[i];
            Vector3 worldPos = transform.position + (Vector3)data.localOffset;

            // Determine color based on enemy type/name
            Color gizmoColor = Color.white;
            string enemyName = "Empty";
            
            if (data.enemyPrefab != null)
            {
                enemyName = data.enemyPrefab.name;
                string lowerName = enemyName.ToLower();

                if (lowerName.Contains("fast")) gizmoColor = Color.yellow;
                else if (lowerName.Contains("ranged") || lowerName.Contains("shooter")) gizmoColor = Color.red;
                else if (lowerName.Contains("bomb") || lowerName.Contains("selfdestruct")) gizmoColor = Color.cyan;
                else gizmoColor = Color.green;
            }

            // Draw connecting line
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.4f);
            Gizmos.DrawLine(centerPosition, worldPos);

            // Draw spawn marker
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(worldPos, 0.5f);
            Gizmos.DrawLine(worldPos - Vector3.right * 0.5f, worldPos + Vector3.right * 0.5f);
            Gizmos.DrawLine(worldPos - Vector3.up * 0.5f, worldPos + Vector3.up * 0.5f);

#if UNITY_EDITOR
            // Draw Label
            GUIStyle style = new GUIStyle();
            style.normal.textColor = gizmoColor;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;

            string labelText = $"[{i}] {enemyName}\n(Delay: {data.delay}s)";
            UnityEditor.Handles.Label(worldPos + Vector3.up * 0.8f, labelText, style);
#endif
        }
    }
}
