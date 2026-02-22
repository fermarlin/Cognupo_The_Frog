using UnityEngine;

//Este script es para que haya una pequena sombra abajo del personaje y que sepamos donde vamos a caer
public class BlobShadow : MonoBehaviour
{
    // =========================
    // AJUSTES
    // =========================
    [Header("Ajustes de Raycast")]
    [SerializeField] private float heightOffset = 0.02f; // Pequena elevacion para evitar el Z-Fighting
    [SerializeField] private LayerMask groundLayer;      //Que es lo que consideramos suelo
    [SerializeField] private float maxRayDistance = 20f; // La distancia que tenemos con el suelo
    [SerializeField] private PlayerMovement playerMovement;
    private MeshRenderer meshRenderer;

    void Start()
    {
        if(playerMovement==null) playerMovement = GetComponentInParent<PlayerMovement>();
        
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void LateUpdate()
    {
        if (playerMovement == null || meshRenderer == null) return;

        // Si esta en el suelo, no quiero representar la sombra
        if (playerMovement.state == PlayerMovement.MovementState.running)
        {
            meshRenderer.enabled = false;
            return;
        }

        // Defino el origen del rayo desde el jugador un poco hacia arriba
        Vector3 rayOrigin = transform.parent.position + Vector3.up * 0.5f;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        //Lanzamos el Raycast hacia abajo
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundLayer))
        {
            // Encendemos la sombra si detecto el suelo estando en el aire
            meshRenderer.enabled = true;
            
            // Posicionamos la sombra en el punto de impacto + el offset
            transform.position = hit.point + (Vector3.up * heightOffset);

            // Esto lo hago para que la sombra se rote en funcion de la rampa que hubiera, le pongo el (90,0,0) 
            // para que que la sombra no nos salga vertical
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            // Si salta muy alto y el rayo no toca nada, la oculto
            meshRenderer.enabled = false;
        }
    }
}