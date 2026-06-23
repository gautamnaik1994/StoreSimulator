using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
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

    [Header("Follow Settings")]
    [SerializeField] private float followSmoothTime = 0.18f;
    [SerializeField] private Vector2 followOffset = Vector2.zero;

    public Camera cam;
    private InputAction panDragAction;
    private InputAction panMoveAction;
    private InputAction zoomAction;
    private InputAction panScrollAction;
    private InputAction toggleFollowAction;

    [Header("Audio Setup")]
    [SerializeField] private AudioMixer audioMixer;

    private bool isMousePanning = false;

    private GameObject selectedAgent;

    private bool isFollowingAgent = false;
    private Vector3 followVelocity = Vector3.zero;

    private Transform cameraTransform;

    private void Awake()
    {
        // cam = GetComponent<Camera>();
        cameraTransform = cam.transform;


        if (inputActions == null) return;
        var cameraMap = inputActions.FindActionMap("Camera");
        if (cameraMap == null) return;

        panDragAction = cameraMap.FindAction("PanDrag");
        panMoveAction = cameraMap.FindAction("PanMove");
        zoomAction = cameraMap.FindAction("Zoom");
        panScrollAction = cameraMap.FindAction("PanScroll");
        toggleFollowAction = cameraMap.FindAction("ToggleFollow");

    }

    private void OnEnable()
    {
        if (inputActions == null) return;
        inputActions.Enable();

        if (panDragAction != null)
        {
            panDragAction.started += HandlePanDragStarted;
            panDragAction.canceled += HandlePanDragCanceled;
        }

        if (toggleFollowAction != null)
        {
            toggleFollowAction.performed += HandleToggleFollowPerformed;
        }
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Disable();

        if (panDragAction != null)
        {
            panDragAction.started -= HandlePanDragStarted;
            panDragAction.canceled -= HandlePanDragCanceled;
        }

        if (toggleFollowAction != null)
        {
            toggleFollowAction.performed -= HandleToggleFollowPerformed;
        }
    }

    private void LateUpdate()
    {
        if (isFollowingAgent)
        {
            HandleFollow();
        }
        else
        {
            HandlePan();
        }

        HandleZoom();
    }

    private void HandlePanDragStarted(InputAction.CallbackContext context)
    {
        isMousePanning = true;
    }

    private void HandlePanDragCanceled(InputAction.CallbackContext context)
    {
        isMousePanning = false;
    }

    private void HandleToggleFollowPerformed(InputAction.CallbackContext context)
    {
        ToggleFollowSelectedAgent();
    }

    private void HandleFollow()
    {
        if (selectedAgent == null)
        {
            isFollowingAgent = false;
            followVelocity = Vector3.zero;
            return;
        }

        Vector3 targetPosition = selectedAgent.transform.position;
        targetPosition.x += followOffset.x;
        targetPosition.y += followOffset.y;
        targetPosition.z = cameraTransform.position.z;

        cameraTransform.position = Vector3.SmoothDamp(
            cameraTransform.position,
            targetPosition,
            ref followVelocity,
            followSmoothTime);

        cameraTransform.position = ClampToPanBounds(cameraTransform.position);
    }

    private void HandlePan()
    {
        if (panMoveAction == null || panScrollAction == null) return;



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

        cameraTransform.position = ClampToPanBounds(cameraTransform.position + move);
    }

    private void HandleZoom()
    {
        if (zoomAction == null) return;

        // Block zoom completely if Command key is being held for panning
        if (Keyboard.current != null && Keyboard.current.leftMetaKey.isPressed) return;

        float scrollValue = zoomAction.ReadValue<float>();

        if (Mathf.Approximately(scrollValue, 0f)) return;

        float sensitivity = Mathf.Abs(scrollValue) < 1f ? trackpadZoomSensitivity : mouseZoomSensitivity;
        float newZoom = cam.orthographicSize - (scrollValue * sensitivity);
        cam.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);



        // 1. Get the current camera zoom ratio (0 = fully zoomed in, 1 = fully zoomed out)
        float currentZoomFactor = (cam.orthographicSize - minZoom) / (maxZoom - minZoom);
        currentZoomFactor = Mathf.Clamp01(currentZoomFactor);

        // 2. Calculate Decibel values 
        // Logarithmic scaling is mathematically required because audio volume in decibels (dB) isn't linear.
        // -80f is completely silent, 0f is full volume.

        // When zoomed OUT (factor -> 1), Music is full (0dB), Crowd is quiet (-20dB)
        // When zoomed IN (factor -> 0), Music ducks (-12dB), Crowd is full (0dB)
        float musicTargetDb = Mathf.Lerp(-25f, 0f, currentZoomFactor);
        float crowdTargetDb = Mathf.Lerp(0f, -40f, currentZoomFactor);

        // 3. Apply to the Mixer exposed parameters
        audioMixer.SetFloat("MusicVol", musicTargetDb);
        audioMixer.SetFloat("EnvVol", crowdTargetDb);

    }

    private Vector3 ClampToPanBounds(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minPanBounds.x, maxPanBounds.x);
        position.y = Mathf.Clamp(position.y, minPanBounds.y, maxPanBounds.y);
        return position;
    }

    public void SetSelectedAgent(GameObject agent)
    {
        selectedAgent = agent;
        if (!isFollowingAgent || selectedAgent == null)
        {
            return;
        }

        followVelocity = Vector3.zero;
    }

    private void ToggleFollowSelectedAgent()
    {
        if (selectedAgent == null)
        {
            isFollowingAgent = false;
            followVelocity = Vector3.zero;
            return;
        }

        isFollowingAgent = !isFollowingAgent;
        followVelocity = Vector3.zero;

        if (!isFollowingAgent)
        {
            return;
        }

        Vector3 targetPosition = selectedAgent.transform.position;
        targetPosition.x += followOffset.x;
        targetPosition.y += followOffset.y;
        targetPosition.z = cameraTransform.position.z;
        cameraTransform.position = ClampToPanBounds(targetPosition);
    }


}