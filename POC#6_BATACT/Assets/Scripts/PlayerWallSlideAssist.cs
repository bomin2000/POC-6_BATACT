using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerWallSlideAssist : MonoBehaviour
{
    [Header("Wall Slide Settings")]
    [SerializeField] private bool isActive = true;
    [SerializeField] private float wallSlideMaxFallSpeed = 2.5f;
    [SerializeField] private float wallSlideAcceleration = 20f;
    
    [Header("Wall Jump Settings")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private float wallJumpSideImpulse = 12f;
    [SerializeField] private float wallJumpUpImpulse = 15f;
    
    [Header("Wall Detection")]
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.1f, 0.8f);
    [SerializeField] private float wallCheckDistance = 0.65f;
    [SerializeField] private LayerMask groundLayers;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private bool debugIsWallSliding;
    [SerializeField] private int debugWallDirection;
    [SerializeField] private bool debugIsAirborne;
    [SerializeField] private float debugCurrentFallSpeed;

    private Rigidbody2D body;
    private PlayerTopDownMovement playerMovement;
    private DualBladeWeaponController weaponController;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerTopDownMovement>();
        weaponController = GetComponentInChildren<DualBladeWeaponController>();
        
        if (groundLayers.value == 0)
        {
            groundLayers = LayerMask.GetMask("Default", "Terrain", "Ground");
            if (groundLayers.value == 0) groundLayers = ~0; // Everything
        }
    }

    private void Update()
    {
        debugCurrentFallSpeed = body.linearVelocity.y;

        // Wall Jump Check
        if (debugIsWallSliding && Input.GetKeyDown(jumpKey))
        {
            PerformWallJump();
        }
    }

    private void PerformWallJump()
    {
        if (playerMovement == null) return;
        
        // Jump away from the wall
        float jumpDirX = -debugWallDirection;
        Vector2 jumpImpulse = new Vector2(jumpDirX * wallJumpSideImpulse, wallJumpUpImpulse);
        
        playerMovement.ApplyExternalKnockback(jumpImpulse, 0.3f);
        debugIsWallSliding = false;
    }

    private void FixedUpdate()
    {
        if (!isActive || playerMovement == null || weaponController == null)
        {
            debugIsWallSliding = false;
            return;
        }

        if (weaponController.CurrentState != WeaponState.Scissors)
        {
            debugIsWallSliding = false;
            return;
        }

        debugIsAirborne = !playerMovement.IsGrounded;

        if (!debugIsAirborne)
        {
            debugIsWallSliding = false;
            return;
        }

        if (body.linearVelocity.y > 0.1f)
        {
            debugIsWallSliding = false;
            return;
        }

        if (playerMovement.IsDashing)
        {
            debugIsWallSliding = false;
            return;
        }

        bool touchingWallLeft = CheckWall(-1);
        bool touchingWallRight = CheckWall(1);

        int inputDir = 0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputDir = -1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputDir = 1;

        // You must press towards the wall to slide
        bool isPushingWallLeft = touchingWallLeft && inputDir == -1;
        bool isPushingWallRight = touchingWallRight && inputDir == 1;

        if (isPushingWallLeft || isPushingWallRight)
        {
            debugIsWallSliding = true;
            debugWallDirection = isPushingWallRight ? 1 : -1;
            
            Vector2 vel = body.linearVelocity;
            float targetYSpeed = -wallSlideMaxFallSpeed;
            
            if (vel.y < targetYSpeed)
            {
                vel.y = Mathf.MoveTowards(vel.y, targetYSpeed, wallSlideAcceleration * Time.fixedDeltaTime);
                body.linearVelocity = vel;
            }
        }
        else
        {
            debugIsWallSliding = false;
            debugWallDirection = 0;
        }
    }

    private bool CheckWall(int directionX)
    {
        Vector2 origin = transform.position;
        Vector2 checkDir = new Vector2(directionX, 0f);
        
        // Use a BoxCast to check for the wall
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, wallCheckSize, 0f, checkDir, wallCheckDistance, groundLayers);
        foreach (var hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger && !hit.collider.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = debugIsWallSliding ? Color.green : Color.yellow;
        
        Vector2 origin = transform.position;
        Vector2 leftPos = origin + Vector2.left * wallCheckDistance;
        Vector2 rightPos = origin + Vector2.right * wallCheckDistance;

        Gizmos.DrawWireCube(leftPos, wallCheckSize);
        Gizmos.DrawWireCube(rightPos, wallCheckSize);
    }
}
