using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementAdvanced : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float slideSpeed;
    public float wallrunSpeed;
    public float climbSpeed;

    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;

    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;

    //Spring
    [Tooltip("The target height/hover distance from the center of the player to the ground.")]
    public float rideHeight = 1.1f;
    [Tooltip("How strong the upward spring force is. Increase this if the player sinks too low.")]
    public float rideSpring = 350f;
    [Tooltip("How much the spring resists bouncing. Increase this if the player oscillates up and down.")]
    public float rideDamper = 35f;
    [Tooltip("Maximum distance the suspension ray will look for the ground.")]
    public float suspensionRayLength = 1.8f;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    

    public Transform orientation;
    public Transform playerObj;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        wallrunning,
        climbing,
        crouching,
        sliding,
        air
    }

    public bool sliding;
    public bool wallrunning;
    public bool climbing;

    [Header("Sound")]
    public AudioSource sound;
    public AudioClip landSound;

    [Header("Animation")]
    [SerializeField] Animator anim;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        playerObj.GetComponent<CapsuleCollider>().enabled = false;
        playerObj.GetComponent<CapsuleCollider>().enabled = true; 

        readyToJump = true;

        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        // ground check
        float scaleFactor = GetCurrentScaleFactor();
        float activeRideHeight = rideHeight * scaleFactor;
        float activeRayLength = suspensionRayLength * scaleFactor;

        bool hitGround = Physics.Raycast(transform.position, Vector3.down, out slopeHit, activeRayLength, whatIsGround);
        grounded = hitGround && slopeHit.distance <= (activeRideHeight + 0.15f);
        // grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

        // Check if Slide Up or Down
        Vector3 verticalVelocity = new Vector3(0, rb.velocity.y, 0);

        // handle drag
        if(grounded && sliding == false)
        {
            rb.drag = groundDrag;
        }
        else if(grounded && sliding == true && OnSlope() && !exitingSlope)
        {
            if(verticalVelocity.magnitude > 0.1f)
            {
                // slide up slope
                rb.drag += Time.deltaTime * 15;

                if(rb.drag >= 15)
                {
                    rb.drag = 15;
                }
            }
            else
            {
                // slide down slope
                rb.drag = 0;
            }
        }
        else if(grounded && sliding == true && (OnSlope() == false || exitingSlope))
        {
            // slide on ground
            rb.drag += Time.deltaTime * 10;

            if(rb.drag >= 15)
            {
                rb.drag = 15;
            }
        }
        else if(!grounded && sliding == true && (OnSlope() == false || exitingSlope))
        {
            // Sliding on Air
            rb.drag = 0;
            // rb.drag += Time.deltaTime * 20;

            // if(rb.drag >= 5)
            // {
            //     rb.drag = 5;
            // }
        }
        else
            rb.drag = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();

        RotatePlayerToMomentum();

        ApplySuspension();
    }

    public float GetCurrentScaleFactor()
    {
        if (sliding && playerObj != null)
        {
            return playerObj.localScale.y; // Uses the slide scaling from Sliding.cs
        }
        else if (state == MovementState.crouching)
        {
            return transform.localScale.y; // Uses crouch scale from local key handler
        }
        return 1f; // Standard standing height
    }

    private void ApplySuspension()
    {
        // Don't apply suspension forces if the player is actively jumping/exiting a slope
        if (exitingSlope) return;

        float scaleFactor = GetCurrentScaleFactor();
        float activeRideHeight = rideHeight * scaleFactor;
        float activeRayLength = suspensionRayLength * scaleFactor;

        // Perform a raycast straight down to measure compression
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, activeRayLength, whatIsGround))
        {
            // Calculate how far we have compressed our suspension spring
            float displacement = activeRideHeight - hit.distance;

            // Project the Rigidbody's velocity onto our upward axis to calculate damping
            float velocityAlongUp = Vector3.Dot(rb.velocity, Vector3.up);

            // Hooke's Law with damping: F = (displacement * K) - (velocity * D)
            float springForceY = (displacement * rideSpring) - (velocityAlongUp * rideDamper);

            // We only want to push UP (never pull the player downwards)
            if (springForceY > 0)
            {
                rb.AddForce(Vector3.up * springForceY, ForceMode.Force);
            }
        }
    }

    private void RotatePlayerToMomentum()
    {
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (horizontalVelocity.magnitude > 0.1f)
        {
            // Calculate the target rotation based on the velocity vector
            Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized);

            playerObj.rotation = Quaternion.Slerp(playerObj.rotation, targetRotation, Time.fixedDeltaTime * 30f);
        }
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if(Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // start crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // stop crouch
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void StateHandler()
    {
        // Mode - Climbing
        if (climbing)
        {
            state = MovementState.climbing;

            desiredMoveSpeed = climbSpeed;

            if(anim)
            {
                anim.SetBool("Walk", true);
            }
        }

        // Mode - Wallrunnning
        if (wallrunning)
        {
            state = MovementState.wallrunning;

            desiredMoveSpeed = wallrunSpeed;

            if(anim)
            {
                anim.SetBool("Walk", true);
            }
        }

        // Mode - Sliding
        else if (sliding)
        {
            state = MovementState.sliding;

            if (OnSlope() && rb.velocity.y < 0.1f)
                desiredMoveSpeed = slideSpeed;

            else
                desiredMoveSpeed = sprintSpeed;

            if(anim)
            {
                anim.SetBool("Walk", false);
            }
        }

        // Mode - Crouching
        else if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;

            if(anim)
            {
                anim.SetBool("Walk", false);
            }
        }

        // Mode - Sprinting
        else if(grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;

            if(anim)
            {
                anim.SetBool("Walk", true);
            }
        }

        // Mode - Walking
        else if (grounded)
        {
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;

            if(anim && ((horizontalInput != 0) || (verticalInput != 0)))
            {
                anim.SetBool("Walk", true);
            }
            else
            {
                anim.SetBool("Walk", false);
            }
        }

        // Mode - Air
        else
        {
            state = MovementState.air;

            if(anim)
            {
                anim.SetBool("Walk", false);
            }
        }

        // check if desiredMoveSpeed has changed drastically
        if(Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            moveSpeed = desiredMoveSpeed;
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        // smoothly lerp movementSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * speedIncreaseMultiplier;

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        // on ground
        else if(grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // in air
        else if(!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        // turn gravity off while on slope
        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        // limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }

        // limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        sound.clip = landSound;
        sound.Play();
    }
    private void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    public bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }
}