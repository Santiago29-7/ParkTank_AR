using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class ARTapToPlaceObject : MonoBehaviour
{
    [Header("Prefab del Tanque")]
    public GameObject tankPrefab;

    private GameObject spawnedTank;
    private ARRaycastManager raycastManager;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        // Detectar si se toca la pantalla o se hace clic
        if (GetTouchPosition(out Vector2 touchPosition))
        {
            if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;

                if (spawnedTank == null)
                {
                    if (tankPrefab != null)
                    {
                        spawnedTank = Instantiate(tankPrefab, hitPose.position, hitPose.rotation);
                    }
                    else
                    {
                        // Si por alguna razón el prefab falla, crea un cubo rojo de prueba
                        spawnedTank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        spawnedTank.transform.position = hitPose.position;
                        spawnedTank.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    }
                }
                else
                {
                    spawnedTank.transform.position = hitPose.position;
                }
            }
        }
    }

    private bool GetTouchPosition(out Vector2 touchPosition)
    {
        touchPosition = Vector2.zero;

        // Detector de toque para teléfonos móviles
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchPosition = touch.position;
                return true;
            }
        }

        // Detector para clics de ratón en el editor
        if (Input.GetMouseButtonDown(0))
        {
            touchPosition = Input.mousePosition;
            return true;
        }

        return false;
    }
}