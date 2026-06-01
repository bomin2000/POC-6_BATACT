using UnityEngine;

[System.Serializable]
public struct FormationSpawnData
{
    [Tooltip("스폰할 적 프리팹")]
    public GameObject enemyPrefab;
    
    [Tooltip("트리거 중심 좌표(Transform.position) 기준의 상대적 오프셋 (예: x=2 이면 오른쪽 2m 위치에 스폰)")]
    public Vector2 localOffset;
    
    [Tooltip("트리거 발동 후 스폰까지의 지연 시간 (0이면 즉시)")]
    public float delay;
}

[CreateAssetMenu(fileName = "NewEncounterFormation", menuName = "ScriptableObjects/Encounter Formation Profile")]
public class EncounterFormationProfile : ScriptableObject
{
    [Header("Formation Settings")]
    [Tooltip("이 포메이션의 이름 또는 설명")]
    public string formationName = "Default Formation";

    [Tooltip("스폰될 적들의 목록과 상대 위치")]
    public FormationSpawnData[] spawns;
}
