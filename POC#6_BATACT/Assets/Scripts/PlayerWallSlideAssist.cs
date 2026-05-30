using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerWallSlideAssist : MonoBehaviour
{
    [Header("Wall Slide Settings")]
    [SerializeField] private bool isActive = true;
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
    [SerializeField] private float wallSlideMaxFallSpeed = 2.5f;
    [SerializeField] private float wallSlideAcceleration = 20f;
    
    [Header("Wall Detection")]
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.1f, 0.8f);
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayers;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private bool debugIsWallSliding;
    [SerializeField] private int debugWallDirection;
    [SerializeField] private bool debugIsHoldingAttack;
    [SerializeField] private bool debugIsAirborne;
    [SerializeField] private float debugCurrentFallSpeed;

    private Rigidbody2D body;
    private PlayerTopDownMovement playerMovement;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerTopDownMovement>();
        
        if (groundLayers.value == 0)
        {
            groundLayers = 1 << LayerMask.NameToLayer("Ground");
        }
    }

    private void Update()
    {
        debugIsHoldingAttack = Input.GetKey(attackKey);
        debugCurrentFallSpeed = body.linearVelocity.y;
    }

    private void FixedUpdate()
    {
        if (!isActive || playerMovement == null)
        {
            debugIsWallSliding = false;
            return;
        }

        debugIsAirborne = !playerMovement.IsGrounded;

        // Condition 1: Must be in the air and holding attack
        if (!debugIsAirborne || !debugIsHoldingAttack)
        {
            debugIsWallSliding = false;
            return;
        }

        // Condition 2: Must be falling or barely moving up
        if (body.linearVelocity.y > 0.1f)
        {
            debugIsWallSliding = false;
            return;
        }

        // Condition 3: Must not be dashing (let player dash freely)
        if (playerMovement.IsDashing)
        {
            debugIsWallSliding = false;
            return;
        }

        // Detect Wall
        bool touchingWallLeft = CheckWall(-1);
        bool touchingWallRight = CheckWall(1);

        if (touchingWallLeft || touchingWallRight)
        {
            debugIsWallSliding = true;
            debugWallDirection = touchingWallRight ? 1 : -1;
            
            // Limit Fall Speed
            Vector2 vel = body.linearVelocity;
            float targetYSpeed = -wallSlideMaxFallSpeed;
            
            if (vel.y < targetYSpeed)
            {
                // Smoothly brake
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
        RaycastHit2D hit = Physics2D.BoxCast(origin, wallCheckSize, 0f, checkDir, wallCheckDistance, groundLayers);
        return hit.collider != null;
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
