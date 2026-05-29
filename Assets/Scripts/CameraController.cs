using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Input Action Asset Reference")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Pan Settings")]
    [SerializeField] private float mousePanSpeed = 0.5f;
    [SerializeField] private float trackpadPanSpeed = 0.8f;
    [SerializeField] private Vector2 minPanBounds = new Vector2(-100, -100);
    [SerializeField] private Vector2 maxPanBounds = new Vector2(100, 100);

    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private float mouseZoomSensitivity = 0.05f;
    [SerializeField] private float trackpadZoomSensitivity = 0.01f;

    private Camera cam;
    private InputAction panDragAction;
    private InputAction panMoveAction;
    private InputAction zoomAction;
    private InputAction panScrollAction; // New action tracking full 2D scroll stream

    private bool isMousePanning = false;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (inputActions == null) return;
        var cameraMap = inputActions.FindActionMap("Camera");
        if (cameraMap == null) return;

        panDragAction = cameraMap.FindAction("PanDrag");
        panMoveAction = cameraMap.FindAction("PanMove");
        zoomAction = cameraMap.FindAction("Zoom");
        panScrollAction = cameraMap.FindAction("PanScroll"); // Bind new action
    }

    private void OnEnable()
    {
        if (inputActions == null) return;
        inputActions.Enable();

        if (panDragAction != null)
        {
            panDragAction.started += ctx => isMousePanning = true;
            panDragAction.canceled += ctx => isMousePanning = false;
        }
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Disable();

        if (panDragAction != null)
        {
            panDragAction.started -= ctx => isMousePanning = true;
            panDragAction.canceled -= ctx => isMousePanning = false;
        }
    }

    private void LateUpdate()
    {
        if (panMoveAction == null || zoomAction == null || panScrollAction == null) return;

        HandlePan();
        HandleZoom();
    }

    private void HandlePan()
    {
        Vector3 move = Vector3.zero;

        // 1. Standard Mouse Drag Pan (Right Click)
        if (isMousePanning)
        {
            Vector2 mouseDelta = panMoveAction.ReadValue<Vector2>();
            move = new Vector3(-mouseDelta.x, -mouseDelta.y, 0) * mousePanSpeed * cam.orthographicSize * 0.01f;
        }
        // 2. Mac Trackpad Pan (Cmd + 2-Finger Swipe)
        else
        {
            bool isModifierPressed = Keyboard.current != null && Keyboard.current.leftMetaKey.isPressed;

            if (isModifierPressed)
            {
                // Read full 2D vector data from the trackpad gesture stream
                Vector2 trackpadDelta = panScrollAction.ReadValue<Vector2>();

                if (trackpadDelta != Vector2.zero)
                {
                    // Using separate X and Y values derived directly from the surface gesture 
                    move = new Vector3(-trackpadDelta.x, -trackpadDelta.y, 0) * trackpadPanSpeed * cam.orthographicSize * 0.005f;
                }
            }
        }

        if (move == Vector3.zero) return;

        // Apply and clamp position
        Vector3 targetPosition = transform.position + move;
        targetPosition.x = Mathf.Clamp(targetPosition.x, minPanBounds.x, maxPanBounds.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minPanBounds.y, maxPanBounds.y);
        transform.position = targetPosition;
    }

    private void HandleZoom()
    {
        // Block zoom completely if Command key is being held for panning
        if (Keyboard.current != null && Keyboard.current.leftMetaKey.isPressed) return;

        float scrollValue = zoomAction.ReadValue<float>();

        if (Mathf.Approximately(scrollValue, 0f)) return;

        float sensitivity = Mathf.Abs(scrollValue) < 1f ? trackpadZoomSensitivity : mouseZoomSensitivity;
        float newZoom = cam.orthographicSize - (scrollValue * sensitivity);
        cam.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);
    }
}