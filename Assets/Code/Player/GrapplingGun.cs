using UnityEngine;
using UnityEngine.InputSystem;

// Este script se engarga de engancharse a los objetivos

public class GrapplingGun : MonoBehaviour
{
    
    // =========================
    // INPUT (New Input System)
    // =========================

    [Header("Input")]
    [SerializeField] private bool useNewInputSystem = true; // por si quieres desactivarlo rapido en editor

    private PlayerInputs inputs;

// =========================
    // REFERENCES
    // =========================

    [Header("References")]
     [SerializeField] private PlayerMovement playerMovement;           // Player (para sacar posicion y las distancias y eso)
     [SerializeField] private VerletRope rope;                         // Cuerda
     [SerializeField] private Transform cam;                           // Main Camera
     [SerializeField] private LayerMask whatIsGrappleable;             // Capas a las que Si se puede enganchar
     [SerializeField] private TargetLockHandler targetLockHandler;     // Sistema de fijado

    // =========================
    // CONFIG
    // =========================

    [Header("General Settings")]
    public float maxGrappleDistance = 100f;         // Distancia maxima para poder engancharse

    // =========================
    // MODO LIBRE: DETECCIoN POR CONO
    // =========================

    [Header("Cone Detection")]
    public float coneSearchRadius = 100f;           // Hasta donde buscamos colliders

    [Range(1f, 180f)] public float coneAngle = 60f; // Cono
    public float coneForwardOffset = 2.0f;          // El cono empieza un poco delante del player para no pillarse a si mismo
    public Vector3 originOffset;                    // Offset libre por si quieres ajustarlo algo

    // =========================
    // INDICADOR VISUAL
    // =========================

    [Header("Indicator")]
    public GameObject targetIndicatorPrefab;        // Prefab del marker encima del candidato
    public float indicatorHeight = 1.5f;            // Altura del indicador sobre el target

    // =========================
    // CUERDA
    // =========================

    [Header("Rope")]
    public float lockRopeDistance = 8f;             // A que distancia queremos ajustar la cuerda al enganchar
    public float ropeReelInSpeed = 25f;             // Velocidad de acercarnos a la cuerda

    // =========================
    // ESTADO RUNTIME
    // =========================

    private Transform currentAnchor;                // Transformque usamos como fixedPoint real de la cuerda
    private Transform currentGrappleTarget;         // A que objeto estamos enganchados ahora mismo (para ignorarlo en seleccion)
    private Transform currentFreeCandidate;         // Mejor candidato actual para agarrarse
    private GameObject indicatorInstance;           // Instancia del indicador (para activarlo/desactivarlo)

    // Buffer fijo para OverlapSphereNonAlloc, esto es porque si hacemos Collider[] hits = Physics.OverlapSphere 
    // el Garbaje Collector va a estar pegandonos tirones cada vez que limpia
    private Collider[] overlapBuffer = new Collider[64];


    //Activar el input system
    private void OnEnable()
    {
        if (!useNewInputSystem) return;

        if (inputs == null) inputs = new PlayerInputs();

        inputs.PlayerActionMap.Enable();

        inputs.PlayerActionMap.Tongue.performed += OnTonguePerformed;
        inputs.PlayerActionMap.Tongue.canceled += OnTongueCanceled;
    }

    private void OnDisable()
    {
        if (!useNewInputSystem) return;

        inputs.PlayerActionMap.Tongue.performed -= OnTonguePerformed;
        inputs.PlayerActionMap.Tongue.canceled -= OnTongueCanceled;
        if (inputs != null)
            inputs.PlayerActionMap.Disable();
    }


    private void Start()
    {
        // Creamos un anchor dummy para no depender directamente del transform del objetivo, tal vez esto 
        // tengamos que modificarlo en un futuro si queremos que los elementos se muevan y el personaje quede enganchado
        GameObject anchorObj = new GameObject("GrappleAnchor");
        currentAnchor = anchorObj.transform;

        // Arrancamos sin grapple
        rope.fixedPoint = null;

        // Instanciamos el indicador una vez y lo reutilizamos (Hay que crear la parte grafica)
        if (targetIndicatorPrefab != null)
        {
            indicatorInstance = Instantiate(targetIndicatorPrefab);
            indicatorInstance.SetActive(false);
        }
    }

    private void Update()
    {
        // Cada frame actualizamos candidato del modo libre y el indicador
        UpdateFreeModeCandidateAndIndicator();
    }

    // =========================
    // HELPERS
    // =========================

    private bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        // Check rapido por bitmask
        return (mask.value & (1 << obj.layer)) != 0;
    }

    // =========================
    // MODO LIBRE
    // =========================

    private void UpdateFreeModeCandidateAndIndicator()
    {
        // Si estamos en target lock, el modo libre queda apagado (no queremos dos sistemas peleandose)
        if (targetLockHandler != null && targetLockHandler.IsTargeting)
        {
            currentFreeCandidate = null;
            HideIndicator();
            return;
        }

        // Buscamos el mejor candidato dentro del cono
        currentFreeCandidate = FindBestConeCandidate();

        // Si hay candidato, ponemos el indicador arriba del target
        if (currentFreeCandidate != null && indicatorInstance != null)
        {
            indicatorInstance.SetActive(true);

            // Subimos el indicador un poco para que no quede clavado en el pivote 
            // (esto tambien habra que terminar de verlo cuando tengamos los enemigos)
            indicatorInstance.transform.position = currentFreeCandidate.position + Vector3.up * indicatorHeight;
        }
        else
        {
            HideIndicator();//Para ocultar el indicador
        }
    }

    // ---------------------------------------------------------
    // LOGICA PRINCIPAL
    // ---------------------------------------------------------
    private Transform FindBestConeCandidate()
    {
        // Obtenemos la direccion de la camara, pero le quito la rotacion de estar mirando al player
        // Esto es lo que hace que el cono este recto respecto al mundo:
        Vector3 flatForward = Vector3.forward;
        if (cam != null)
        {
            flatForward = cam.forward;
            flatForward.y = 0f;
            flatForward.Normalize();
        }
        else
        {
            // Fallback si no hay camara asignada
            flatForward = transform.forward;
            flatForward.y = 0f;
            flatForward.Normalize();
        }

        // Calcula el origen del cono
        // el origen sale del player, no de la camara.
        // Luego lo empujamos hacia delante un poco para que el cono salga delante del player.
        Vector3 origin = transform.position + (flatForward * coneForwardOffset) + originOffset;

        float halfAngle = coneAngle * 0.5f;

        // Buscar candidatos con la esfera, vuelvo a comentar el porque pongo el NonAlloc es para 
        // que no me devuelva un array de collider y el garbaje collector nos pete el juego
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            coneSearchRadius,
            overlapBuffer,
            whatIsGrappleable,
            QueryTriggerInteraction.Ignore
        );

        Transform best = null;
        float bestSqrDist = float.MaxValue;

        // Filtrar y eleccion del mas cercano
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null) continue;

            Transform candidate = col.transform;

            // Si ya estamos grappling, ignoramos punto actual
            // (evita que el candidato sea el mismo al que ya estas enganchado)
            if (IsGrappling() && currentGrappleTarget != null && candidate == currentGrappleTarget)
                continue;

            // Vector hacia el candidato desde el origen del cono
            Vector3 toTarget = candidate.position - origin;
            float sqrDist = toTarget.sqrMagnitude;
            if (sqrDist < 0.0001f) continue;

            // Distancia real
            float dist = Mathf.Sqrt(sqrDist);

            // Filtro por distancia maxima real de grapple
            if (dist > maxGrappleDistance) continue;

            // Direccion normalizada hacia el target
            Vector3 dir = toTarget / dist;

            // angulo respecto a la camara plana
            float angle = Vector3.Angle(flatForward, dir);
            if (angle > halfAngle) continue;

            // Elegimos el mas cercano
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                best = candidate;
            }
        }

        return best;
    }

    // =========================
    // INICIO GRAPPLE
    // =========================

    private void StartGrapple()
    {
        // Si estamos fijando target, mandamos ese como prioridad
        if (targetLockHandler != null && targetLockHandler.IsTargeting)
        {
            Transform target = targetLockHandler.GetCurrentTarget();

            // Que sea grapplable por layer
            if (target != null && IsInLayerMask(target.gameObject, whatIsGrappleable))
            {
                float distToTarget = Vector3.Distance(playerMovement.transform.position, target.position);

                // Si esta dentro de rango, enganchamos y salimos
                if (distToTarget <= maxGrappleDistance)
                {
                    AttachRope(target);
                    return;
                }
            }
        }

        // Si no hay target lock, usamos el candidato del cono
        if (currentFreeCandidate != null)
        {
            AttachRope(currentFreeCandidate);
        }
    }

    // =========================
    // ENGANCHE
    // =========================

    private void AttachRope(Transform target)
    {
        // Ponemos el anchor en la posicion del target (con un pelin de offset en Y)
        // (este offset evita que el punto quede enterrado)
        currentAnchor.position = target.position + Vector3.up * 1f;

        // Decimos a la cuerda que este es el punto fijo
        rope.fixedPoint = currentAnchor;

        // Guardamos target actual para poder ignorarlo en el cono
        currentGrappleTarget = target;

        // Ajustamos longitudes para que al enganchar
        // maxDistance arranca a la distancia real actual
        // desiredMaxDistance recoge hasta lockRopeDistance
        float currentDist = Vector3.Distance(playerMovement.transform.position, currentAnchor.position);

        rope.maxDistance = currentDist;
        rope.desiredMaxDistance = Mathf.Min(lockRopeDistance, currentDist);
        rope.reelInSpeed = ropeReelInSpeed;
    }

    // =========================
    // STOP GRAPPLE
    // =========================

    public void StopGrapple()
    {
        // Desactivamos punto fijo de la cuerda
        rope.fixedPoint = null;
        currentGrappleTarget = null;
    }

    public bool IsGrappling()
    {
        // Si hay fixedPoint, la cuerda esta activa
        return rope.fixedPoint != null;
    }

    // =========================
    // INDICATOR
    // =========================

    private void HideIndicator()
    {
        // Evitamos SetActive(false) cada frame si ya estaba apagado
        if (indicatorInstance != null && indicatorInstance.activeSelf)
            indicatorInstance.SetActive(false);
    }

    // =========================
    // CALLBACKS DE INPUT
    // =========================
    private void OnTonguePerformed(InputAction.CallbackContext ctx)
    {
        StartGrapple();
    }

    private void OnTongueCanceled(InputAction.CallbackContext ctx)
    {
        StopGrapple();
    }
}