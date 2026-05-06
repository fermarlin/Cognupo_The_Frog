using UnityEngine;

// Este script mantiene un objeto a cierta altura respecto al suelo.
public class FlyingHeightController : MonoBehaviour
{
    [Header("Ground Height")]
    [SerializeField] private LayerMask groundLayer;             // Capa que consideramos suelo
    [SerializeField] private float desiredHeight = 3f;          // Altura deseada respecto al suelo
    [SerializeField] private float heightLerpSpeed = 10f;       // Suavizado vertical
    [SerializeField] private float groundRayDistance = 30f;     // Distancia extra hacia abajo
    [SerializeField] private float groundRayStartHeight = 10f;  // Desde cuanto mas arriba empieza el raycast

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    public void KeepHeightFromGround()
    {
        // Lanzamos el raycast desde arriba hacia abajo. Asi, aunque el enemigo atraviese un poco el suelo, podemos volver a encontrar el suelo desde arriba.
        Vector3 rayStartPosition = transform.position + Vector3.up * groundRayStartHeight;

        Ray ray = new Ray(rayStartPosition, Vector3.down);

        float totalRayDistance = groundRayStartHeight + groundRayDistance;

        if (Physics.Raycast(ray, out RaycastHit hit, totalRayDistance, groundLayer))
        {
            ApplyHeightFromGround(hit.point);
        }
    }

    private void ApplyHeightFromGround(Vector3 groundPoint)
    {
        // Calculamos la altura objetivo.
        float targetY = groundPoint.y + desiredHeight;

        //Hacemos un lerp para que el personaje no se tepee
        float newY = Mathf.Lerp(
            transform.position.y,
            targetY,
            heightLerpSpeed * Time.deltaTime
        );

        transform.position = new Vector3(
            transform.position.x,
            newY,
            transform.position.z
        );

        // Si el Rigidbody va hacia abajo, cortamos la velocidad vertical.
        if (rb != null && rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );
        }
    }


}