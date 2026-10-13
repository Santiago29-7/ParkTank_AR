using System.Collections;
using UnityEngine;

public class MuzzleFlash3D : MonoBehaviour
{
    [Header("Parámetros de Animación")]
    public float flashDuration = 0.2f; // Aumentado a 0.2s para que sea claramente visible
    public Vector3 baseScale;

    private Coroutine flashCoroutine;

    void Awake()
    {
        // Guardar automáticamente la escala que le diste al modelo en la vista 3D
        baseScale = transform.localScale;
        if (baseScale == Vector3.zero)
        {
            baseScale = Vector3.one;
        }
    }

    public void TriggerFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        gameObject.SetActive(true);
        flashCoroutine = StartCoroutine(AnimateFlash());
    }

    private IEnumerator AnimateFlash()
    {
        // Rotación aleatoria en Z en cada disparo
        transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / flashDuration;

            // Crece y se encoge usando una curva senoidal
            float scaleFactor = Mathf.Sin(progress * Mathf.PI);
            transform.localScale = baseScale * scaleFactor;

            yield return null;
        }

        gameObject.SetActive(false);
    }
}