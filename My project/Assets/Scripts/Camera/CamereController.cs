using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Dependencies")]
    public TurnManager turnManager;

    [Header("Transition Settings")]
    public float transitionSpeed = 3.5f;

    [Header("White Camera Position")]
    public Vector3 whitePosition = new Vector3(-6, 8, -10);
    public Vector3 whiteRotation = new Vector3(45, 35, 0);

    [Header("Black Camera Position")]
    public Vector3 blackPosition = new Vector3(14, 8, 25); 
    public Vector3 blackRotation = new Vector3(45, 215, 0); 

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private void Start()
    {
        targetPosition = whitePosition;
        targetRotation = Quaternion.Euler(whiteRotation);

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
    
    // Change position and rotation of camera
    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * transitionSpeed);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
    }


    // Change depending on the turn
    private void UpdateCameraTarget(bool isWhiteTurn)
    {
        targetPosition = isWhiteTurn ? whitePosition : blackPosition;
        targetRotation = Quaternion.Euler(isWhiteTurn ? whiteRotation : blackRotation);
    }
}