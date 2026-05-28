using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerTopDownMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("기본 이동 속도")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash Settings")]
    [Tooltip("대시 시 속도")]
    [SerializeField] private float dashSpeed = 15f;
    [Tooltip("대시 지속 시간 (초)")]
    [SerializeField] private float dashDuration = 0.2f;
    [Tooltip("대시 쿨타임 (초)")]
    [SerializeField] private float dashCooldown = 1f;

    [Header("Visual Root Reference")]
    [Tooltip("마우스 방향을 바라볼 자식 오브젝트의 Transform (플레이어의 전체 transform 회전 방지)")]
    [SerializeField] private Transform visualRoot;

    // 컴포넌트 참조
    private Rigidbody2D rb2d;
    private Camera mainCamera;

    // 입력 및 상태 변수
    private Vector2 moveInput;
    private Vector2 lastLookDirection = Vector2.right;
    
    private bool isDashing;
    private float dashTimeRemaining;
    private float dashCooldownRemaining;
    private Vector2 dashDirection;

    private void Awake()
    {
        // Rigidbody2D 컴포넌트 캐싱
        rb2d = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Main Camera 캐싱 및 태그 확인 경고
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("PlayerTopDownMovement: Scene에 'MainCamera' 태그가 지정된 카메라가 없습니다. 마우스 바라보기 기능이 오작동할 수 있습니다.");
        }

        // visualRoot가 지정되지 않은 경우 경고
        if (visualRoot == null)
        {
            Debug.LogWarning("PlayerTopDownMovement: Visual Root가 지정되지 않았습니다. 인스펙터에서 플레이어의 렌더러가 포함된 자식 오브젝트를 지정해주세요.");
        }
    }

    private void Update()
    {
        // 1. 이동 입력 읽기 (기본 Input Manager)
        if (!isDashing)
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
            moveInput = moveInput.normalized;
        }

        // 2. 대시 쿨타임 갱신
        if (dashCooldownRemaining > 0f)
        {
            dashCooldownRemaining -= Time.deltaTime;
        }

        // 3. 대시 입력 처리 (Space)
        if (Input.GetKeyDown(KeyCode.Space) && !isDashing && dashCooldownRemaining <= 0f)
        {
            StartDash();
        }

        // 4. 마우스 방향을 바라보도록 회전 처리
        RotateVisualTowardsMouse();
    }

    private void FixedUpdate()
    {
        // 5. Rigidbody2D 기반 이동 처리
        if (isDashing)
        {
            PerformDashStep();
        }
        else
        {
            PerformNormalMovement();
        }
    }

    /// <summary>
    /// 대시 동작을 시작합니다.
    /// </summary>
    private void StartDash()
    {
        isDashing = true;
        dashTimeRemaining = dashDuration;
        dashCooldownRemaining = dashCooldown;

        // 이동 입력이 있다면 입력 방향으로, 없다면 마지막으로 바라보던(마우스) 방향으로 대시합니다.
        if (moveInput.sqrMagnitude > 0.001f)
        {
            dashDirection = moveInput.normalized;
        }
        else
        {
            dashDirection = lastLookDirection;
        }
    }

    /// <summary>
    /// 대시 상태에서의 물리 이동을 업데이트합니다.
    /// </summary>
    private void PerformDashStep()
    {
        dashTimeRemaining -= Time.fixedDeltaTime;

        if (dashTimeRemaining <= 0f)
        {
            isDashing = false;
            // 대시가 끝난 뒤 미끄러짐을 줄이기 위해 속도 초기화
            rb2d.linearVelocity = moveInput * moveSpeed;
        }
        else
        {
            // 대시 중에는 지정된 방향과 대시 속도로 Rigidbody2D 속도 갱신
            // transform/rigidbody의 위치가 매 프레임 정상적으로 갱신되어 BoomerangProjectile이 완벽히 실시간 추적하게 합니다.
            rb2d.linearVelocity = dashDirection * dashSpeed;
        }
    }

    /// <summary>
    /// 일반 상태에서의 물리 이동을 처리합니다.
    /// </summary>
    private void PerformNormalMovement()
    {
        rb2d.linearVelocity = moveInput * moveSpeed;
    }

    /// <summary>
    /// visualRoot가 마우스 커서의 세계 좌표 방향을 바라보도록 회전시킵니다.
    /// </summary>
    private void RotateVisualTowardsMouse()
    {
        if (mainCamera == null || visualRoot == null) return;

        // 마우스의 스크린 좌표를 월드 좌표로 변환
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        // Z축을 0으로 맞추어 2D 평면 벡터 생성
        Vector2 direction = new Vector2(mouseWorldPosition.x - transform.position.x, mouseWorldPosition.y - transform.position.y);

        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();
            lastLookDirection = direction;

            // 마우스 방향에 대응하는 회전 각도 계산 (Z축 회전)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // 플레이어 전체 Transform 대신 자식인 visualRoot만 회전
            visualRoot.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
