using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("카메라가 추적할 타겟 (Player Transform)")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [Tooltip("타겟과의 카메라 위치 오프셋 (보통 Z축 오프셋을 위해 사용)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("추적 반응 속도 (값이 작을수록 빠르고 정확하게 추적하며, 클수록 부드럽게 추적)")]
    [SerializeField] private float smoothTime = 0.15f;

    // smoothTime 연산에 필요한 내부 변수
    private Vector3 currentVelocity = Vector3.zero;

    private void Start()
    {
        // 씬 시작 시 타겟이 지정되어 있다면 카메라의 위치를 즉시 초기화하여 부자연스러운 움직임을 방지합니다.
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치 계산 (타겟 위치 + 오프셋)
        Vector3 targetPosition = target.position + offset;

        // SmoothDamp를 이용하여 부드럽게 카메라 이동 처리
        // 플레이어가 대시를 통해 갑자기 빨라져도 속도 비례 추적으로 매우 자연스럽게 따라갑니다.
        Vector3 newPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            smoothTime
        );

        // 카메라의 새로운 위치 대입 (SmoothDamp 연산에 의해 Z축도 오프셋대로 정상 유지됨)
        transform.position = newPosition;
    }
}
