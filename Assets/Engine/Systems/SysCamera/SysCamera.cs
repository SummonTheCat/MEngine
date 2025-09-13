using UnityEngine;

[ExecuteAlways]
public class SysCamera : MonoBehaviour
{
    public static SysCamera Instance { get; private set; }

    public Camera TargetCamera;

    public EntState PlayerEntity;

    public enum CameraMode { Viewer, FollowPlayer }
    public enum MovementMode { Smooth, PixelPerfect }

    [Header("Camera Mode")]
    public CameraMode Mode = CameraMode.Viewer;
    public MovementMode Movement = MovementMode.Smooth;

    [Header("Zoom Settings")]
    public float ZoomSpeed = 2f;
    public float MinZoom = 2f;
    public float MaxZoom = 20f;

    [Header("Pixel Perfect Settings")]
    public int PixelsPerUnit = 16;
    public int BasePPUZoom = 1;
    public int MinPPUZoom = 1;
    public int MaxPPUZoom = 10;
    public bool PixelPerfectEntities = true;

    [Header("Pan Settings")]
    public float DragSensitivity = 0.01f;
    public float MoveSmoothness = 10f;

    private Vector3 dragOrigin;
    private Vector3 targetPosition;
    private float targetZoom;

    private int screenWidth;
    private int screenHeight;

    private void InitSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Init()
    {
        InitSingleton();

        if (TargetCamera == null)
            TargetCamera = Camera.main;

        TargetCamera.orthographic = true;
        targetPosition = TargetCamera.transform.position;
        targetZoom = TargetCamera.orthographicSize;

        screenWidth = Screen.width;
        screenHeight = Screen.height;
    }

    public void Tick()
    {
        if (TargetCamera == null) return;

        if (PlayerEntity == null || PlayerEntity != SysLevelEntities.Instance.stateManager.GetEntityWithName("Player"))
        {
            PlayerEntity = SysLevelEntities.Instance.stateManager.GetEntityWithName("Player");
        }

        if (Mode == CameraMode.Viewer)
        {
            HandleInput();
        }
        else if (Mode == CameraMode.FollowPlayer && PlayerEntity != null)
        {
            targetPosition = new Vector3(PlayerEntity.EntPosition.x, PlayerEntity.EntPosition.y, TargetCamera.transform.position.z);
        }

        UpdateCamera();

        if (screenWidth != Screen.width || screenHeight != Screen.height)
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            ForceProjectionUpdate();

            if (PlayerEntity != null && Mode == CameraMode.FollowPlayer)
            {
                targetPosition = new Vector3(PlayerEntity.EntPosition.x, PlayerEntity.EntPosition.y, TargetCamera.transform.position.z);
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = TargetCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 current = TargetCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta = dragOrigin - current;
            targetPosition += delta;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            if (Movement == MovementMode.Smooth)
            {
                float zoomDelta = -scroll * ZoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom + zoomDelta, MinZoom, MaxZoom);
            }
            else if (Movement == MovementMode.PixelPerfect)
            {
                BasePPUZoom += (int)Mathf.Sign(scroll);
                BasePPUZoom = Mathf.Clamp(BasePPUZoom, MinPPUZoom, MaxPPUZoom);
            }
        }
    }

    private void UpdateCamera()
    {
        if (Movement == MovementMode.Smooth)
        {
            TargetCamera.transform.position = Vector3.Lerp(TargetCamera.transform.position, targetPosition, Time.deltaTime * MoveSmoothness);
            TargetCamera.orthographicSize = Mathf.Lerp(TargetCamera.orthographicSize, targetZoom, Time.deltaTime * MoveSmoothness);
        }
        else if (Movement == MovementMode.PixelPerfect)
        {
            float unitsPerScreenPixel = 1f / (PixelsPerUnit * BasePPUZoom);
            float camSize = Screen.height * unitsPerScreenPixel * 0.5f;
            TargetCamera.orthographicSize = camSize;

            float unitSize = 1f / PixelsPerUnit;
            Vector3 snapped = new Vector3(
                Mathf.Round(targetPosition.x / unitSize) * unitSize,
                Mathf.Round(targetPosition.y / unitSize) * unitSize,
                targetPosition.z
            );

            TargetCamera.transform.position = snapped;
        }
    }

    private void ForceProjectionUpdate()
    {
        if (Movement == MovementMode.PixelPerfect)
        {
            float unitsPerScreenPixel = 1f / (PixelsPerUnit * BasePPUZoom);
            float camSize = Screen.height * unitsPerScreenPixel * 0.5f;
            TargetCamera.orthographicSize = camSize;
        }
    }
}
