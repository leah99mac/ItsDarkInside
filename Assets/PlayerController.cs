using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System;
using System.IO;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]

public class PlayerController : NetworkBehaviour
{

    public enum PlayerDirectionStatus { IDLE, RIGHT, LEFT };
    public enum PlayerGroundStatus { GROUNDED, JUMPING, FALLING, LANDING };

    // Move player in 2D space
    public float maxSpeed = 3.4f;
    public float jumpHeight = 80.0f;
    public float gravityScale = 0.6f;
    public float maxLandingTime = 1.0f;
    public Camera mainCamera;

    // Player status output
    public NetworkVariable<PlayerDirectionStatus> playerDirectionStatus = new NetworkVariable<PlayerDirectionStatus>(PlayerDirectionStatus.IDLE, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<PlayerGroundStatus> playerGroundStatus = new NetworkVariable<PlayerGroundStatus>(PlayerGroundStatus.GROUNDED, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Light Ball
    public GameObject lightBall;
    public float lightBallCooldown = 1.0f;
    public float lightBallSpeed = 3.0f;

    // Debug network data
    public bool publishNetworkData = true;
    public string networkFileLocation = "network_data";

    float moveDirection = 0;
    float landingCounter = 0;
    bool isGrounded = false;
    float timeUntilLightBall = 0f;
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

        if (mainCamera && IsLocalPlayer) {
            mainCamera.gameObject.tag = "MainCamera";
        }


        if (publishNetworkData && IsOwner && !IsServer)
        {
            networkFileLocation += "_CLIENT.csv";
            File.Delete(networkFileLocation);
        }
        else if (publishNetworkData && IsHost && !IsLocalPlayer)
        {
            networkFileLocation += "_SERVER.csv";
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Only move player if we own it
        if (IsOwner) {

            byte input = ConstructInputByte();

            // No input, no message needed?
            MovePlayerServerRpc(input);

            System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            double cur_time = (System.DateTime.UtcNow - epochStart).TotalMilliseconds;
            double rtt = NetworkManager.Singleton.LocalTime.Time - NetworkManager.Singleton.ServerTime.Time;
            // Line format: <timestamp>,<rtt>,<local x>,<local y>
            string networkData = "" + cur_time + "," + rtt + "," + transform.position.x + "," + transform.position.y;

            if (!File.Exists(networkFileLocation))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(networkFileLocation))
                {
                    sw.WriteLine(networkData);
                }
            }
            else
            {
                // This text is always added, making the file longer over time
                // if it is not deleted.
                using (StreamWriter sw = File.AppendText(networkFileLocation))
                {
                    sw.WriteLine(networkData);
                }
            }
        }

<<<<<<< HEAD


        // Camera follow TODO FIX THIS
        if (IsOwner && mainCamera)
        {
            mainCamera.transform.position = new Vector3(t.position.x, t.position.y, cameraPos.z);
        }

=======
>>>>>>> origin/main
    }

    private byte ConstructInputByte() {
        // Construct the input byte to be sent to the server (or used by the server)
        // This byte is of the format (_ is unused):
        // BIT      0   1   2   3   4       5   6   7
        // KEY      W   A   S   D   Space   _   _   _
        // When we make the switch to android, we'll have to change this
        // Call this function in Update() only!

        byte input = 0;
        if (Input.GetKeyDown(KeyCode.W))        input |= 0b1;
        if (Input.GetKey(KeyCode.A))            input |= 0b10;
        if (Input.GetKeyDown(KeyCode.S))        input |= 0b100;
        if (Input.GetKey(KeyCode.D))            input |= 0b1000;
        if (Input.GetKeyDown(KeyCode.Space))    input |= 0b10000;

        return input;
    }

    // This function will be requested by the client, but executed on the server
    // This way, we send our input from the client to the server which can do all physics calculations
    // Only call this on players you own!
    [ServerRpc]
    public void MovePlayerServerRpc(byte input)
    {
        // Extract input data
        moveDirection = 0f;
        if ((input & 0b10   )!= 0) moveDirection += -1f; // A pressed, move left
        if ((input & 0b1000 )!= 0) moveDirection +=  1f; // D pressed, move right
        bool tryJump = ((input & 0b1 )!= 0);
        bool tryShootLightBall = ((input & 0b10000 )!= 0);

        // Horizontal movement
        // If landing, no movement
        if (playerGroundStatus.Value == PlayerGroundStatus.LANDING) {
            moveDirection = 0f;
        }

        // Jumping
        bool startJump = false;
        if (tryJump && isGrounded && playerGroundStatus.Value != PlayerGroundStatus.LANDING)
        {
            r2d.velocity = new Vector2(r2d.velocity.x, jumpHeight);
            landingCounter = maxLandingTime;
            startJump = true;
        }

        // Player state transitions
        switch (playerGroundStatus.Value)
        {
            case PlayerGroundStatus.GROUNDED:
                // Grounded -> Jumping
                if (startJump)
                    playerGroundStatus.Value = PlayerGroundStatus.JUMPING;
                // Grounded -> Falling
                else if (!isGrounded && r2d.velocity.y < -0.05)
                    playerGroundStatus.Value = PlayerGroundStatus.FALLING;
                break;
            case PlayerGroundStatus.JUMPING:
                // Jumping -> Falling
                if (!isGrounded && r2d.velocity.y < -0.05)
                    playerGroundStatus.Value = PlayerGroundStatus.FALLING;
                break;
            case PlayerGroundStatus.FALLING:
                // Falling -> Landing
                if (isGrounded)
                    playerGroundStatus.Value = PlayerGroundStatus.LANDING;
                break;
            case PlayerGroundStatus.LANDING:
                // Landing -> Grounded
                if (landingCounter <= 0)
                    playerGroundStatus.Value = PlayerGroundStatus.GROUNDED;
                landingCounter -= Time.deltaTime;
                break;
        }

        // Set player direction
        playerDirectionStatus.Value = (playerGroundStatus.Value == PlayerGroundStatus.LANDING) ? playerDirectionStatus.Value : // If player is landing, maintain current direction
                                (moveDirection < 0) ? PlayerDirectionStatus.LEFT : // Not landing, direction depends on moving
                                (moveDirection > 0) ? PlayerDirectionStatus.RIGHT :
                                PlayerDirectionStatus.IDLE; // Not moving, idle

        // Shoot light ball
        if (tryShootLightBall && timeUntilLightBall <= 0f) {
            // Get velocity
            Vector2 vel;
            Vector3 spawn;
            switch(playerDirectionStatus.Value) {
                case PlayerDirectionStatus.LEFT:
                    vel = Vector2.left;
                    spawn = new Vector3(-0.6f, 0, -0.01f);
                break;
                case PlayerDirectionStatus.RIGHT:
                    vel = Vector2.right;
                    spawn = new Vector3(0.6f, 0, -0.01f);
                break;
                case PlayerDirectionStatus.IDLE:
                default:
                    vel = Vector2.up;
                    spawn = new Vector3(0, 0.6f, -0.01f);
                break;
            }
            vel *= lightBallSpeed;
            spawn += transform.position;
            // Create ball
            GameObject ball = Instantiate(lightBall, spawn, Quaternion.identity);
            ball.GetComponent<NetworkObject>().Spawn();
            ball.GetComponent<Rigidbody2D>().velocity = vel;

            // Reset cooldown
            timeUntilLightBall = lightBallCooldown;
        } else if (timeUntilLightBall > 0f) {
            timeUntilLightBall -= Time.deltaTime;
        }

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
                if (colliders[i] != mainCollider && !colliders[i].isTrigger)
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