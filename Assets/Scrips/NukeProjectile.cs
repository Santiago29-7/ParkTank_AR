using UnityEngine;
using Photon.Pun;
using System.Collections;
using UnityEngine.UI;

public class NukeProjectile : MonoBehaviourPun
{
    [Header("Configuración de Suelo y Expiración")]
    public float groundYLevel = 0f;
    public float maxLifetime = 8.0f;

    [Header("Efectos")]
    public GameObject massiveExplosionVFX;
    public AudioClip nukeExplosionSFX;

    private bool hasExploded = false;
    private GameObject shooterGameObject;
    private int shooterPhotonViewID = -1;

    public void SetShooter(GameObject shooter)
    {
        shooterGameObject = shooter;

        PhotonView pv = shooter.GetComponent<PhotonView>();
        if (pv != null)
        {
            shooterPhotonViewID = pv.ViewID;
        }

        // Tomamos el nivel Y del tanque que disparó como el nivel del suelo base
        groundYLevel = shooter.transform.position.y - 0.05f;
    }

    void Start()
    {
        // Respaldo por si el proyectil se va al infinito sin chocar
        if (!PhotonNetwork.IsConnected || (photonView != null && photonView.IsMine))
        {
            StartCoroutine(SafeDestroyRoutine(maxLifetime));
        }
    }

    void Update()
    {
        if (hasExploded) return;

        // Detección directa del plano inferior (el suelo)
        if (transform.position.y <= groundYLevel)
        {
            Vector3 fixedPos = transform.position;
            fixedPos.y = groundYLevel;
            transform.position = fixedPos;

            ExplodeNuke();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        ExplodeNuke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        ExplodeNuke();
    }

    private void ExplodeNuke()
    {
        if (hasExploded) return;
        hasExploded = true;

        // 1. Detener el proyectil y ocultarlo (SIN DESTRUIRLO AÚN para que corran los efectos)
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Frena la bala en seco
            rb.isKinematic = true;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        // 2. Instanciar explosión de partículas
        if (massiveExplosionVFX != null)
        {
            if (PhotonNetwork.IsConnected)
                PhotonNetwork.Instantiate(massiveExplosionVFX.name, transform.position, Quaternion.identity);
            else
                Instantiate(massiveExplosionVFX, transform.position, Quaternion.identity);
        }

        // 3. Disparar los cálculos de daño y pantallazo a través de la red
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("RPC_TriggerNuke", RpcTarget.All, shooterPhotonViewID);

            // 4. Programar la destrucción real después de 2.5 segundos (cuando acaben los efectos)
            if (photonView.IsMine)
            {
                StartCoroutine(SafeDestroyRoutine(2.5f));
            }
        }
        else
        {
            RPC_TriggerNuke(shooterPhotonViewID);
            Destroy(gameObject, 2.5f);
        }
    }

    [PunRPC]
    void RPC_TriggerNuke(int shooterViewID)
    {
        // Activar la pantalla blanca
        StartCoroutine(NukeScreenEffects());

        // Aplicar el daño
        ApplyNukeDamage(shooterViewID);
    }

    void ApplyNukeDamage(int shooterViewID)
    {
        TankHealth[] allTanks = FindObjectsByType<TankHealth>(FindObjectsSortMode.None);

        foreach (TankHealth tank in allTanks)
        {
            if (tank == null || tank.currentHealth <= 0) continue;

            bool isShooter = false;
            if (shooterViewID != -1 && tank.photonView != null && tank.photonView.ViewID == shooterViewID)
            {
                isShooter = true;
            }
            else if (!PhotonNetwork.IsConnected && shooterGameObject != null && tank.gameObject == shooterGameObject)
            {
                isShooter = true;
            }

            if (!isShooter)
            {
                // ¡CORRECCIÓN VITAL PARA RED!
                // Para no aplicar doble o triple daño, SOLO el dueño de este tanque (o el anfitrión para la IA) descuenta la salud.
                if (!PhotonNetwork.IsConnected || (tank.photonView != null && tank.photonView.IsMine))
                {
                    float damageAmount = tank.maxHealth * 0.5f;
                    tank.TakeDamage(damageAmount);
                }
            }
        }
    }

    IEnumerator NukeScreenEffects()
    {
        if (nukeExplosionSFX != null) AudioSource.PlayClipAtPoint(nukeExplosionSFX, transform.position);

        GameObject flashCanvasObj = new GameObject("NukeFlashCanvas");
        Canvas canvas = flashCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = flashCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        Image flashImg = flashCanvasObj.AddComponent<Image>();
        flashImg.color = Color.white;
        flashImg.rectTransform.anchorMin = Vector2.zero;
        flashImg.rectTransform.anchorMax = Vector2.one;
        flashImg.rectTransform.sizeDelta = Vector2.zero;

        Transform camTransform = Camera.main != null ? Camera.main.transform : null;
        Vector3 originalCamPos = camTransform != null ? camTransform.localPosition : Vector3.zero;

        float duration = 1.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            float alpha = Mathf.Lerp(1f, 0f, progress);
            if (flashImg != null) flashImg.color = new Color(1f, 1f, 1f, alpha);

            if (camTransform != null)
            {
                float shakeIntensity = Mathf.Lerp(0.6f, 0f, progress);
                camTransform.localPosition = originalCamPos + Random.insideUnitSphere * shakeIntensity;
            }

            yield return null;
        }

        if (camTransform != null) camTransform.localPosition = originalCamPos;
        if (flashCanvasObj != null) Destroy(flashCanvasObj);
    }

    IEnumerator SafeDestroyRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.IsConnected && photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}