using System.Collections;
using UnityEngine;

public class ImpactEffect3D : MonoBehaviour
{
    [Header("Configuración de Animación")]
    public float lifetime = 1.0f;       // Tiempo antes de destruirse
    public float expandDuration = 0.15f; // Rapidez de expansión
    public Vector3 defaultScale = Vector3.one;

    private Vector3 targetScale;

    void Awake()
    {
        // Guardar la escala asignada al prefab antes de ocultarlo
        targetScale = transform.localScale;
        if (targetScale == Vector3.zero)
        {
            targetScale = defaultScale;
        }
    }

    void Start()
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(AnimateImpact());
        Destroy(gameObject, lifetime);
    }

    private IEnumerator AnimateImpact()
    {
        float elapsed = 0f;

        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / expandDuration;
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, progress);
            yield return null;
        }

        transform.localScale = targetScale;
    }
}