using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]

public class PlayerController : MonoBehaviour
{

    public enum PlayerDirectionStatus {IDLE, RIGHT, LEFT};
    public enum PlayerGroundStatus {GROUNDED, JUMPING, FALLING, LANDING};

    // Move player in 2D space
    public float maxSpeed = 3.4f;
    public float jumpHeight = 6.5f;
    public float gravityScale = 1.5f;
    public float maxLandingTime = 1.0f;
    public Camera mainCamera;

    // Player status output
    public PlayerDirectionStatus playerDirectionStatus = PlayerDirectionStatus.IDLE;
    public PlayerGroundStatus playerGroundStatus = PlayerGroundStatus.GROUNDED;

    bool facingRight = true;
    float moveDirection = 0;
    float landingCounter = 0;
    bool isGrounded = false;
    Vector3 cameraPos;
    Rigidbody2D r2d;
    CapsuleCollider2D mainCollider;
    Transform t;

    // Use this for initialization
    void Start()
    {
        t = transform;
        r2d = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<CapsuleCollider2D>();
        r2d.freezeRotation = true;
        r2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        r2d.gravityScale = gravityScale;
        facingRight = t.localScale.x > 0;

        if (mainCamera)
        {
            cameraPos = mainCamera.transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Movement controls
        if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && playerGroundStatus != PlayerGroundStatus.LANDING)
        {
            moveDirection = Input.GetKey(KeyCode.A) ? -1 : 1;
        }
        else
        {
            if (isGrounded || r2d.velocity.magnitude < 0.01f)
            {
                moveDirection = 0;
            }
        }

        // Change facing direction
        if (moveDirection != 0)
        {
            if (moveDirection > 0 && !facingRight)
            {
                facingRight = true;
                t.localScale = new Vector3(Mathf.Abs(t.localScale.x), t.localScale.y, transform.localScale.z);
            }
            if (moveDirection < 0 && facingRight)
            {
                facingRight = false;
                t.localScale = new Vector3(-Mathf.Abs(t.localScale.x), t.localScale.y, t.localScale.z);
            }
        }

        // Jumping
        bool startJump = false;
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && playerGroundStatus != PlayerGroundStatus.LANDING)
        {
            r2d.velocity = new Vector2(r2d.velocity.x, jumpHeight);
            landingCounter = maxLandingTime;
            startJump = true;
        }

        // Camera follow
        if (mainCamera)
        {
            mainCamera.transform.position = new Vector3(t.position.x, cameraPos.y, cameraPos.z);
        }


        // Player state transitions
        switch(playerGroundStatus) {
            case PlayerGroundStatus.GROUNDED:
                // Grounded -> Jumping
                if (startJump)
                    playerGroundStatus = PlayerGroundStatus.JUMPING;
                // Grounded -> Falling
                else if (!isGrounded && r2d.velocity.y < -0.05)
                    playerGroundStatus = PlayerGroundStatus.FALLING;
            break;
            case PlayerGroundStatus.JUMPING:
                // Jumping -> Falling
                if (!isGrounded && r2d.velocity.y < -0.05)
                    playerGroundStatus = PlayerGroundStatus.FALLING;
            break;
            case PlayerGroundStatus.FALLING:
                // Falling -> Landing
                if (isGrounded)
                    playerGroundStatus = PlayerGroundStatus.LANDING;
            break;
            case PlayerGroundStatus.LANDING:
                // Landing -> Grounded
                if (landingCounter <= 0)
                    playerGroundStatus = PlayerGroundStatus.GROUNDED;
                landingCounter -= Time.deltaTime;
            break;
        }

        // Set player direction
        playerDirectionStatus = (playerGroundStatus == PlayerGroundStatus.LANDING)? playerDirectionStatus : // If player is landing, maintain current direction
                                (moveDirection < 0)? PlayerDirectionStatus.LEFT : // Not landing, direction depends on moving
                                (moveDirection > 0)? PlayerDirectionStatus.RIGHT : 
                                PlayerDirectionStatus.IDLE; // Not moving, idle
    }

    void FixedUpdate()
    {
        Bounds colliderBounds = mainCollider.bounds;
        float colliderRadius = mainCollider.size.x * 0.4f * Mathf.Abs(transform.localScale.x);
        Vector3 groundCheckPos = colliderBounds.min + new Vector3(colliderBounds.size.x * 0.5f, colliderRadius * 0.9f, 0);
        // Check if player is grounded
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckPos, colliderRadius);
        //Check if any of the overlapping colliders are not player collider, if so, set isGrounded to true
        isGrounded = false;
        if (colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != mainCollider)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        // Apply movement velocity
        r2d.velocity = new Vector2((moveDirection) * maxSpeed, r2d.velocity.y);

        // Simple debug
        Debug.DrawLine(groundCheckPos, groundCheckPos - new Vector3(0, colliderRadius, 0), isGrounded ? Color.green : Color.red);
        Debug.DrawLine(groundCheckPos, groundCheckPos - new Vector3(colliderRadius, 0, 0), isGrounded ? Color.green : Color.red);
    }
}