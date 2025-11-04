using UnityEngine;

public class MMORPGCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Seu personagem
    public Vector3 targetOffset = new Vector3(0, 2f, 0);
    
    [Header("Camera Settings")]
    public float followSpeed = 5f;
    public float rotationSpeed = 2f;
    public float zoomSpeed = 10f;
    
    [Header("Zoom Settings")]
    public float minZoom = 3f;
    public float maxZoom = 15f;
    public float currentZoom = 8f;
    
    [Header("Camera Positions")]
    public float defaultHeight = 8f;
    public float defaultDistance = 6f;
    
    private Vector3 cameraOffset;
    private Vector2 rotation = Vector2.zero;
    private bool isRotating = false;
    private Vector3 lastMousePosition;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target não definido para a câmera!");
            return;
        }
        
        // Configuração inicial da câmera
        cameraOffset = new Vector3(0, defaultHeight, -defaultDistance);
        currentZoom = defaultDistance;
        transform.position = target.position + targetOffset + cameraOffset;
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        HandleZoom();
        HandleRightClickDrag();
        FollowTarget();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            
            // Atualiza o offset baseado no zoom
            cameraOffset.z = -currentZoom;
            cameraOffset.y = defaultHeight * (currentZoom / defaultDistance);
        }
    }

    void HandleRightClickDrag()
    {
        // Iniciar rotação
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
            Cursor.lockState = CursorLockMode.None;
        }
        
        // Parar rotação
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
            Cursor.lockState = CursorLockMode.None;
        }
        
        // Rotacionar câmera
        if (isRotating && Input.GetMouseButton(1))
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector2 mouseDelta = currentMousePosition - lastMousePosition;
            
            rotation.x += mouseDelta.x * rotationSpeed * Time.deltaTime;
            rotation.y -= mouseDelta.y * rotationSpeed * Time.deltaTime;
            rotation.y = Mathf.Clamp(rotation.y, -80f, 80f);
            
            lastMousePosition = currentMousePosition;
        }
    }

    void FollowTarget()
    {
        if (target == null) return;
        
        // Calcula a posição desejada da câmera
        Quaternion cameraRotation = Quaternion.Euler(rotation.y, rotation.x, 0);
        Vector3 desiredPosition = target.position + targetOffset + cameraRotation * cameraOffset;
        
        // Suaviza o movimento da câmera
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        
        // Faz a câmera olhar para o alvo
        Vector3 lookAtPosition = target.position + targetOffset;
        transform.LookAt(lookAtPosition);
    }

    // Método para resetar a câmera
    public void ResetCamera()
    {
        rotation = Vector2.zero;
        currentZoom = defaultDistance;
        cameraOffset = new Vector3(0, defaultHeight, -defaultDistance);
    }
}