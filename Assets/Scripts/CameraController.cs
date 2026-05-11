using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public CinemachineCamera cineCam;

    public Transform player;
    public Transform cameraTarget;

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minFOV = 25f;
    public float maxFOV = 60f;
    public float zoomSmooth = 10f;

    [Header("Pan")]
    public float panSpeed = 0.03f;

    [Header("Follow")]
    public float recenterSpeed = 5f;

    private CameraControls input;

    private bool followPlayer = true;
    private float targetFOV;

    void Awake()
    {
        input = new CameraControls();
        ResolveMissingReferences();
    }

    void OnEnable()
    {
        input.Enable();
    }

    void OnDisable()
    {
        input.Disable();
    }

    void Start()
    {
        ResolveMissingReferences();

        if (cineCam == null || player == null || cameraTarget == null)
        {
            enabled = false;
            return;
        }

        cineCam.Follow = cameraTarget;

        targetFOV = cineCam.Lens.FieldOfView;

        // старт по центру гравця
        cameraTarget.position = player.position;
    }

    void Update()
    {
        ResolveMissingReferences();

        if (cineCam == null || player == null || cameraTarget == null)
        {
            return;
        }

        HandleZoom();
        HandlePan();
        HandleFollow();
    }

    void ResolveMissingReferences()
    {
        if (cameraTarget == null)
        {
            cameraTarget = transform;
        }

        if (cineCam == null)
        {
            cineCam = FindFirstObjectByType<CinemachineCamera>();
        }

        if (player == null)
        {
            CharacterController playerController = FindFirstObjectByType<CharacterController>();
            player = playerController != null ? playerController.transform : null;
        }

        if (player == null)
        {
            PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
            player = playerMovement != null ? playerMovement.transform : null;
        }

        if (cineCam != null && cameraTarget != null && cineCam.Follow != cameraTarget)
        {
            cineCam.Follow = cameraTarget;
        }
    }

    // -----------------------------------
    // ZOOM
    // -----------------------------------
    void HandleZoom()
    {
        float scroll = input.Camera.Zoom.ReadValue<float>();

        if (scroll != 0)
        {
            targetFOV -= scroll * zoomSpeed;
            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        }

        cineCam.Lens.FieldOfView = Mathf.Lerp(
            cineCam.Lens.FieldOfView,
            targetFOV,
            Time.deltaTime * zoomSmooth
        );
    }

    // -----------------------------------
    // PAN WITH MIDDLE MOUSE
    // -----------------------------------
    void HandlePan()
    {
        if (input.Camera.Pan.IsPressed())
        {
            followPlayer = false;

            Vector2 delta = input.Camera.MouseDelta.ReadValue<Vector2>();

            Vector3 right = Quaternion.Euler(0, 45, 0) * Vector3.right;
            Vector3 forward = Quaternion.Euler(0, 45, 0) * Vector3.forward;
            Vector3 move = (-delta.x * right + -delta.y * forward) * panSpeed;
            cameraTarget.position += move;
        }
    }

    // -----------------------------------
    // RETURN TO PLAYER
    // -----------------------------------
    void HandleFollow()
    {
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();

        // якщо гравець рухається
        if (moveInput != Vector2.zero)
        {
            followPlayer = true;
        }

        // плавне повернення до гравця
        if (followPlayer)
        {
            cameraTarget.position = Vector3.Lerp(
                cameraTarget.position,
                player.position,
                Time.deltaTime * recenterSpeed
            );
        }
    }
}
