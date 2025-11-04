using UnityEngine;
using Unity.Cinemachine;

public class SimpleFreeLookZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 12f;
    public float minFOV = 30f;
    public float maxFOV = 60f;
    
    private CinemachineCamera virtualCamera;

    void Start()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
    }

    void Update()
    {
        HandleZoomInput();
    }

    void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.01f && virtualCamera != null)
        {
            // Controla o Field of View para zoom
            virtualCamera.Lens.FieldOfView -= scroll * zoomSpeed;
            virtualCamera.Lens.FieldOfView = Mathf.Clamp(virtualCamera.Lens.FieldOfView, minFOV, maxFOV);
        }
    }
}