using UnityEngine;
using UnityEngine.Rendering; //untuk mengakses volume

public class CameraController : MonoBehaviour
{
    [Header("Dependencies")]
    public TurnManager turnManager;

    [Header("Orbit Settings")]
    public Vector3 pivotPoint = Vector3.zero;
	
    [Header("Distance Settings")]
    public float distance = 18f;
    private float targetDistance;
    public float zoomSmoothness = 10f;
	
	[Header("Shake Settings")]
    public float defaultShakeDuration = 0.2f;
    public float defaultShakeIntensity = 0.3f;
    private Vector3 shakeOffset;
	
    public float transitionSpeed = 4f;
    public float orbitSpeed = 5f;
	
	[Header("Zoom Settings")]
    public float zoomSpeed = 10f;
    public float minDistance = 5f;
    public float maxDistance = 30f;

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
	
	[Header("Post Processing")]
    public Volume impactVolume;

    private void Start()
    {
		targetDistance = distance;
        targetYaw = whiteYaw;
        targetPitch = defaultPitch;
		
		// Memastikan efek blur mati saat awal permainan
        if (impactVolume != null) impactVolume.weight = 0f;

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
        if (UIManager.Instance != null && UIManager.Instance.IsPaused) return;
        HandleOrbit();
		
		// Logika Zoom Smooth
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            targetDistance -= scrollInput * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
        distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * zoomSmoothness);

        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * transitionSpeed);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * transitionSpeed);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

		Vector3 position = pivotPoint - (rotation * Vector3.forward * distance);
        // Tambahkan shakeOffset ke posisi akhir
        transform.position = position + shakeOffset;
        transform.rotation = rotation;
    }
	
	//Trigger efek shake
	public void TriggerShake(float intensityMultiplier = 1f)
		{
			StopAllCoroutines();
			StartCoroutine(ShakeCoroutine(defaultShakeDuration, defaultShakeIntensity * intensityMultiplier));
		}

	private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude)
		{
			float elapsed = 0f;
			while (elapsed < duration)
			{
				float x = Random.Range(-1f, 1f) * magnitude;
				float y = Random.Range(-1f, 1f) * magnitude;

				shakeOffset = new Vector3(x, y, 0);

				// LOGIKA BLUR: Naikkan weight saat mulai, turunkan secara linear
				if (impactVolume != null)
				{
					impactVolume.weight = Mathf.Lerp(magnitude, 0, elapsed / duration);
				}

				elapsed += Time.deltaTime;
				yield return null;
			}
			
			shakeOffset = Vector3.zero;
			if (impactVolume != null) impactVolume.weight = 0f;
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