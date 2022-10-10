using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost_Sprite_Controller : MonoBehaviour
{

    public Sprite spriteIdleGround;
    public Sprite spriteIdleJumping;
    public Sprite spriteIdleFalling;
    public Sprite spriteIdleLanding;
    public Sprite spriteMovingWalking;
    public Sprite spriteMovingJumping;
    public Sprite spriteMovingFalling;
    public Sprite spriteMovingLanding;
    public float floatFactor = 1.0f;
    public float floatPeriod = 0.5f;

    PlayerController parentPlayerController;
    Transform parentTransform;
    SpriteRenderer sr;
    public float floatTime = 0.0f;
    

    // Start is called before the first frame update
    void Start()
    {
        parentPlayerController = this.GetComponentInParent<PlayerController>();
        parentTransform = this.GetComponentInParent<Transform>();
        sr = this.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // Get player statuses
        PlayerController.PlayerGroundStatus groundStatus = parentPlayerController.playerGroundStatus;
        PlayerController.PlayerDirectionStatus dirStatus = parentPlayerController.playerDirectionStatus;
        bool moving = (dirStatus != PlayerController.PlayerDirectionStatus.IDLE);

        // Update time
        floatTime = (floatTime + Time.deltaTime) % floatPeriod;

        // Change sprite based on status
        switch(groundStatus) {
            case PlayerController.PlayerGroundStatus.GROUNDED:
                // Switch to normal walking sprite
                sr.sprite = (moving)? spriteMovingWalking : spriteIdleGround;
                // Apply floating
                float shift = floatFactor * 0.1f * Mathf.Sin(2f * Mathf.PI * floatTime / floatPeriod);
                transform.localPosition = Vector3.up * shift;
                break;
            case PlayerController.PlayerGroundStatus.JUMPING:
                // Switch to jumping sprite
                sr.sprite = (moving)? spriteMovingJumping : spriteIdleJumping;
                transform.localPosition = Vector3.zero;
                break;
            case PlayerController.PlayerGroundStatus.FALLING:
                // Switch to falling sprite
                sr.sprite = (moving)? spriteMovingFalling : spriteIdleFalling;
                transform.localPosition = Vector3.zero;
                break;
            case PlayerController.PlayerGroundStatus.LANDING:
                // Switch to landing sprite
                sr.sprite = (moving)? spriteMovingLanding : spriteIdleLanding;
                transform.localPosition = Vector3.zero;
                break;
        }


    }
}
