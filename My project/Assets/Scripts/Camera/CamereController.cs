using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Dependencies")]
    public TurnManager turnManager;

    [Header("Orbit Settings")]
    public Vector3 pivotPoint = Vector3.zero;
    public float distance = 18f;
    public float transitionSpeed = 4f;
    public float orbitSpeed = 5f;

    [Header("Turn Angles (Horizontal/Yaw)")]
    public float whiteYaw = 35f;
    public float blackYaw = 215f;

    [Header("Pitch Limits (Vertical)")]
    public float defaultPitch = 45f;
    public float minPitch = 20f;
    public float maxPitch = 85f;

    private float targetYaw;
    private float targetPitch;

    private float currentYaw;
    private float currentPitch;

    private void Start()
    {
        targetYaw = whiteYaw;
        targetPitch = defaultPitch;

        currentYaw = targetYaw;
        currentPitch = targetPitch;

        if (turnManager != null)
        {
            turnManager.OnTurnChanged += UpdateCameraTarget;
        }
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnChanged -= UpdateCameraTarget;
        }
    }

    private void Update()
    {
        HandleOrbit();

        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * transitionSpeed);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * transitionSpeed);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        Vector3 position = pivotPoint - (rotation * Vector3.forward * distance);

        transform.position = position;
        transform.rotation = rotation;
    }

    // Handle camera movement
    private void HandleOrbit()
    {
        if (Input.GetMouseButton(1))
        {
            targetYaw += Input.GetAxis("Mouse X") * orbitSpeed;
            targetPitch -= Input.GetAxis("Mouse Y") * orbitSpeed;

            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }
    }

    // Return camera to the side of the right team
    private void UpdateCameraTarget(bool isWhiteTurn)
    {
        targetYaw = isWhiteTurn ? whiteYaw : blackYaw;
        targetPitch = defaultPitch;
    }
}