using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    public Camera camera;
    public bool freezeXZAxis = true; // Ограничить поворот только по Y оси
    
    void Start()
    {
        if (camera == null)
            camera = Camera.main;
    }
    
    void Update()
    {
        if (camera == null)
            return;
            
        // Поворачиваем объект к камере
        Vector3 directionToCamera = camera.transform.position - transform.position;
        
        if (freezeXZAxis)
        {
            // Ограничиваем поворот только по Y оси (горизонтальный поворот)
            directionToCamera.y = 0;
        }
        
        if (directionToCamera != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            transform.rotation = targetRotation;
        }
    }
}
