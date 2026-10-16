using UnityEngine;

public class HealthBarBillboard : MonoBehaviour
{
    private Transform mainCam;

    void Start()
    {
        if (Camera.main != null)
            mainCam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (mainCam == null)
        {
            if (Camera.main != null) mainCam = Camera.main.transform;
            return;
        }

        // Mantiene el Canvas mirando a la cámara en tiempo real
        transform.LookAt(transform.position + mainCam.rotation * Vector3.forward, mainCam.rotation * Vector3.up);
    }
}