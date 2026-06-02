using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerObj;
    private Rigidbody rb;
    private PlayerMovementAdvanced pm;
    public PlayerCam cam;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideForce;

    float slideSpeed;

    private float slideTimer;

    public float slideYScale;
    private float startYScale;
    private float startYPos;

    [Header("Input")]
    public KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovementAdvanced>();

        startYScale = playerObj.localScale.y;
        startYPos = playerObj.localPosition.y;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0))
        if (Input.GetKeyDown(slideKey))
            StartSlide();

        if (Input.GetKeyUp(slideKey) && pm.sliding)
            StopSlide();
    }

    private void FixedUpdate()
    {
        if (pm.sliding)
            SlidingMovement();
    }

    private void StartSlide()
    {
        pm.sliding = true;

        rb.drag = 0;

        // Reset Up Speed
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        // Offset Character Hitbox
        playerObj.localPosition = new Vector3(playerObj.localPosition.x, startYPos + (1-slideYScale), playerObj.localPosition.z);
        playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        
        if(!pm.grounded)
        {
            rb.AddForce(Vector3.down * slideForce, ForceMode.Impulse);
        }

        slideTimer = maxSlideTime;
        slideSpeed = slideForce;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;


        // sliding normal
        if(!pm.OnSlope() && rb.velocity.y <= 0f)
        {
            rb.AddForce(inputDirection.normalized * slideSpeed, ForceMode.Force);

            slideTimer -= Time.deltaTime;
            slideSpeed -= Time.deltaTime * 150;

            Debug.Log("normal");

            if(slideSpeed <= 0)
            {
                slideSpeed = 0;
            }
        }

        // sliding down a slope
        else if(pm.OnSlope() && rb.velocity.y < -0.1f)
        {
            rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * slideSpeed, ForceMode.Force);
            rb.AddForce(Vector3.down * slideForce, ForceMode.Force);

            slideSpeed -= Time.deltaTime * 300;
            
            Debug.Log("down");

            if(slideSpeed <= 0)
            {
                slideSpeed = 0;
            }
        }

        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float FovSpeed = horizontalVelocity.magnitude * 6;
        // Fov Speed
        cam.DoFov(Mathf.Clamp(FovSpeed, 80, 90));

        if (slideTimer <= 0)
            StopSlide();
    }

    private void StopSlide()
    {
        pm.sliding = false;

        // Offset Character Hitbox
        playerObj.localPosition = new Vector3(playerObj.localPosition.x, startYPos, playerObj.localPosition.z);
        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);
        
        rb.AddForce(Vector3.up.normalized * slideSpeed, ForceMode.Force);

        // reset Fov
        cam.DoFov(80f);
    }
}
