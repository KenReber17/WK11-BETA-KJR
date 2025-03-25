using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    public CharacterController controller;
    public Transform cam;

    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    // Height offset above the terrain
    public float heightOffset = 0.5f;

    // Layer mask to raycast against the terrain
    public LayerMask terrainLayer;

    // Smoothing for height adjustment
    public float heightSmoothTime = 0.1f;
    private float heightVelocity;

    // Gravity and jumping variables
    public float gravity = 9.81f;
    public float jumpForce = 5f; // Public variable for jump strength
    private float verticalVelocity; // Tracks vertical movement (jumping/falling)

    void Start()
    {
        SnapToTerrainSurface();
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 moveDir = Vector3.zero;
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            moveDir = moveDir.normalized * speed * Time.deltaTime;
        }

        // Check for jump input
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            verticalVelocity = jumpForce;
        }

        // Apply movement and adjust height
        AdjustHeightToTerrain(moveDir);
    }

    // Snap player to terrain surface at start
    private void SnapToTerrainSurface()
    {
        if (GetTerrainHeight(transform.position, out float terrainHeight))
        {
            Vector3 newPosition = transform.position;
            newPosition.y = terrainHeight + heightOffset + controller.height / 2f; // Center of controller
            transform.position = newPosition;
            verticalVelocity = 0f; // Ensure grounded at start
        }
    }

    // Adjust player height to stay 0.5f above the terrain, including jumping
    private void AdjustHeightToTerrain(Vector3 horizontalMove)
    {
        if (GetTerrainHeight(transform.position, out float terrainHeight))
        {
            float targetHeight = terrainHeight + heightOffset + controller.height / 2f; // Center of controller
            Vector3 currentPosition = transform.position;
            float currentHeight = currentPosition.y;

            // Apply gravity to vertical velocity
            verticalVelocity -= gravity * Time.deltaTime;

            // Combine horizontal and vertical movement
            Vector3 totalMove = horizontalMove;
            totalMove.y = verticalVelocity * Time.deltaTime;

            // Move the controller
            controller.Move(totalMove);

            // Update current position after movement
            currentPosition = transform.position;

            // Only adjust height if grounded (close to terrain and not jumping)
            if (IsGrounded())
            {
                float smoothedHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, heightSmoothTime);
                Vector3 newPosition = currentPosition;
                newPosition.y = smoothedHeight;
                transform.position = newPosition;
                verticalVelocity = 0f; // Reset velocity when grounded
            }
            // Clamp to prevent sinking below terrain
            else if (currentPosition.y < targetHeight)
            {
                currentPosition.y = targetHeight;
                transform.position = currentPosition;
                verticalVelocity = 0f; // Reset velocity when hitting ground
            }
        }
        else
        {
            // If no terrain hit, apply gravity to fall naturally
            verticalVelocity -= gravity * Time.deltaTime;
            controller.Move(horizontalMove + Vector3.down * verticalVelocity * Time.deltaTime);
        }
    }

    // Check if player is grounded on the terrain
    private bool IsGrounded()
    {
        if (GetTerrainHeight(transform.position, out float terrainHeight))
        {
            float targetHeight = terrainHeight + heightOffset + controller.height / 2f;
            float currentHeight = transform.position.y;
            bool isCloseToGround = Mathf.Abs(currentHeight - targetHeight) < 0.2f;
            bool isNotJumping = verticalVelocity <= 0f;

            return isCloseToGround && isNotJumping;
        }
        return false;
    }

    // Raycast to get terrain height at position
    private bool GetTerrainHeight(Vector3 position, out float height)
    {
        Ray ray = new Ray(new Vector3(position.x, 1000f, position.z), Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 2000f, terrainLayer))
        {
            height = hit.point.y;
            return true;
        }

        height = 0f; // Default fallback if raycast fails
        return false;
    }
}