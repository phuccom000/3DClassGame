using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Player : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;
    private Transform cam;
    private World world;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    public float gravity = -9.8f;
    public float flightSpeed = 8f; // Speed for creative flight
    public float flightSprintSpeed = 12f; // Sprinting speed for creative flight

    public float playerWidth = 0.15f;
    //public float boundsTolerance = 0.1f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    public Vector2 turn;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    public Transform highlightBlock;
    public Transform placeBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public Toolbar toolbar;


    private void Start()
    {
        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();

        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor in the center of the screen
        Cursor.visible = false;

    }

    private void FixedUpdate()
    {
        if (!world.inUI)
        {
            CalculateVelocity();
            if (!world.settings.isCreativeMode && jumpRequest)
                Jump();

            // Clamp the pitch to avoid over-rotation
            turn.y = Mathf.Clamp(turn.y, -90f, 90f);

            transform.localRotation = Quaternion.Euler(0, turn.x * world.settings.mouseSensitivity, 0);
            cam.localRotation = Quaternion.Euler(turn.y * world.settings.mouseSensitivity, 0, 0);
            transform.Translate(velocity, Space.World);
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            world.inUI = !world.inUI;
            Cursor.visible = !Cursor.visible;
        }

        if (Input.GetKeyDown(KeyCode.F1)) // Toggle creative mode
        {
            world.settings.isCreativeMode = !world.settings.isCreativeMode;
            if (world.settings.isCreativeMode)
            {
                verticalMomentum = 0; // Reset vertical momentum when entering creative mode
            }
        }

        if (!world.inUI)
        {
            GetPlayerInputs();
            placeCursorBlocks();
        }
    }


    void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }
    private void CalculateVelocity()
    {
        if (world.settings.isCreativeMode)
        {
            float currentFlightSpeed = isSprinting ? flightSprintSpeed : flightSpeed;

            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * currentFlightSpeed;

            if (Input.GetKey(KeyCode.Space)) // Ascend
            {
                velocity.y = currentFlightSpeed * Time.fixedDeltaTime;
            }
            else if (Input.GetKey(KeyCode.LeftShift)) // Descend
            {
                velocity.y = -currentFlightSpeed * Time.fixedDeltaTime;
            }
            else
            {
                velocity.y = 0; // No vertical movement when neither key is pressed
            }
        }
        else
        {
            if (verticalMomentum > gravity)
                verticalMomentum += Time.fixedDeltaTime * gravity;

            if (isSprinting)
                velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
            else
                velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;

            velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

            if (velocity.z > 0 && front || velocity.z < 0 && back)
                velocity.z = 0;
            if (velocity.x > 0 && right || velocity.x < 0 && left)
                velocity.x = 0;
            if (velocity.y < 0)
                velocity.y = checkDownSpeed(velocity.y);
            else if (velocity.y > 0)
                velocity.y = checkUpSpeed(velocity.y);
        }
    }
    private void placeCursorBlocks()
    {
        float step = checkIncrement;
        Vector3Int lastPos = new Vector3Int();
        while (step < reach)
        {
            Vector3 pos = cam.position + cam.forward * step;
            if (world.CheckForVoxel(pos))
            {
                highlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placeBlock.position = lastPos;

                highlightBlock.gameObject.SetActive(true);
                //placeBlock.gameObject.SetActive(true);

                return;
            }
            lastPos = new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            step += checkIncrement;
        }
        highlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);
    }
    private void GetPlayerInputs()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");
        turn.x += mouseHorizontal;
        turn.y -= mouseVertical;

        if (Input.GetButtonDown("Sprint"))
        {
            isSprinting = !isSprinting;
        }

        if (isSprinting && horizontal == 0 && vertical == 0)
        {
            isSprinting = false;
        }

        if (!world.settings.isCreativeMode && isGrounded && Input.GetButtonDown("Jump"))
        {
            jumpRequest = true;
        }

        if (highlightBlock.gameObject.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
                world.GetChunkFromVector3(highlightBlock.position).EditVoxel(Vector3Int.FloorToInt(highlightBlock.position), 0);

            if (Input.GetMouseButtonDown(1) && toolbar.slots[toolbar.slotIndex].HasItem)
            {
                world.GetChunkFromVector3(placeBlock.position).EditVoxel(Vector3Int.FloorToInt(placeBlock.position), toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                if (!world.settings.isCreativeMode)
                    toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
            }
        }
    }
    private float checkDownSpeed(float downSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))
            )
        {
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    private float checkUpSpeed(float upSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth))
            )
        {
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    public bool front
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth))
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool back
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth))
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool left
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z))
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool right
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z))
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
