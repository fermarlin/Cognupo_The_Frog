using UnityEngine;

// Este script mueve una plataforma por varios puntos
public class PlatformMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Vector3[] localPoints;   // Lista de puntos en local por los que pasara la plataforma
    [SerializeField] private float moveDuration = 2f; // Cuanto tarda en ir de un punto al siguiente
    [SerializeField] private float waitDuration = 1f; // Cuanto espera al llegar a cada punto
    [SerializeField] private bool loop = true;        // Si true, sigue moviendose indefinidamente
    [SerializeField] private bool closeLoop = false;  // Si true, al llegar al ultimo vuelve al primero. Si false, hace ida y vuelta

    [Header("Player Collision")]
    [SerializeField] private Collider platformCollider; // Collider de la plataforma que activaremos o desactivaremos

    private Vector3[] globalPoints; // Puntos convertidos a coordenadas globales

    private int currentPointIndex = 0; // Punto en el que esta ahora
    private int nextPointIndex = 0;    // Punto al que se dirige
    private int direction = 1;         // 1 = avanza, -1 = retrocede

    private float moveProgress = 0f;   // Progreso entre el punto actual y el siguiente
    private float waitTimer = 0f;      // Temporizador de espera

    private bool isWaiting = false;    // Para saber si esta esperando en un punto
    private bool hasFinished = false;  // Para saber si ya termino el recorrido cuando loop es false

    private Vector3 currentVelocity;   // Velocidad real de la plataforma

    public Vector3 CurrentVelocity => currentVelocity; // Devuelve la velocidad actual de la plataforma
    private Transform player;        // Referencia al player para saber si esta por encima o por debajo
    private void Start()
    {
        // Si no se ha asignado collider manualmente, intentamos coger el del propio objeto
        if (platformCollider == null)
        {
            platformCollider = GetComponent<Collider>();
        }

        // Si no hay puntos, no hacemos nada
        if (localPoints == null || localPoints.Length == 0)
        {
            Debug.LogWarning("PlatformMover: No hay puntos configurados.");
            hasFinished = true;
            return;
        }

        // Convertimos todos los puntos locales a globales
        globalPoints = new Vector3[localPoints.Length];
        for (int i = 0; i < localPoints.Length; i++)
        {
            globalPoints[i] = transform.TransformPoint(localPoints[i]);
        }

        // Colocamos la plataforma en el primer punto
        transform.position = globalPoints[0];

        // El siguiente punto sera el segundo si existe
        currentPointIndex = 0;
        nextPointIndex = localPoints.Length > 1 ? 1 : 0;

        moveProgress = 0f;
        waitTimer = 0f;
        isWaiting = false;
        hasFinished = localPoints.Length <= 1;

        currentVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        // Actualizamos si el collider debe estar activo o no
        UpdatePlatformCollider();

        // Si ya termino o no hay suficientes puntos, no se mueve
        if (hasFinished || globalPoints == null || globalPoints.Length <= 1)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        // Guardamos posicion antes de mover para calcular velocidad real
        Vector3 previousPosition = transform.position;

        // Si esta esperando, contamos tiempo
        if (isWaiting)
        {
            HandleWaiting();
        }
        else
        {
            HandleMovement();
        }

        // Calculamos velocidad real de la plataforma
        currentVelocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
    }

    private void UpdatePlatformCollider()
    {
        // Si falta alguna referencia, no hacemos nada
        if (player == null || platformCollider == null) return;

        // Si la plataforma esta por encima del player, desactivamos el collider
        // Si esta por debajo del player, lo activamos
        platformCollider.enabled = transform.position.y < player.position.y;
    }

    private void HandleWaiting()
    {
        waitTimer += Time.fixedDeltaTime;
        currentVelocity = Vector3.zero;

        // Cuando termina la espera, vuelve a moverse
        if (waitTimer >= waitDuration)
        {
            waitTimer = 0f;
            isWaiting = false;
        }
    }

    private void HandleMovement()
    {
        Vector3 startPos = globalPoints[currentPointIndex];
        Vector3 endPos = globalPoints[nextPointIndex];

        // Avanzamos el progreso entre un punto y el siguiente
        moveProgress += Time.fixedDeltaTime / moveDuration;
        moveProgress = Mathf.Clamp01(moveProgress);

        // Suavizamos el movimiento para que no sea tan brusco
        float smoothProgress = Mathf.SmoothStep(0f, 1f, moveProgress);

        // Movemos la plataforma entre ambos puntos
        transform.position = Vector3.Lerp(startPos, endPos, smoothProgress);

        // Si ha llegado al siguiente punto
        if (moveProgress >= 1f)
        {
            // Ahora ese siguiente punto pasa a ser el actual
            currentPointIndex = nextPointIndex;

            // Reiniciamos progreso
            moveProgress = 0f;

            // Calculamos cual sera el siguiente destino
            if (!CalculateNextPoint())
            {
                hasFinished = true;
                currentVelocity = Vector3.zero;
                return;
            }

            // Espera en el punto antes de seguir
            isWaiting = true;
            waitTimer = 0f;
        }
    }

    private bool CalculateNextPoint()
    {
        // Si hay menos de 2 puntos, no hay recorrido real
        if (globalPoints.Length <= 1)
            return false;

        // Modo circulo: 0 -> 1 -> 2 -> 3 -> 0 -> 1...
        if (closeLoop)
        {
            int candidate = currentPointIndex + 1;

            // Si se pasa del ultimo, vuelve al primero
            if (candidate >= globalPoints.Length)
            {
                if (!loop)
                    return false;

                candidate = 0;
            }

            nextPointIndex = candidate;
            return true;
        }

        // Modo ida y vuelta: 0 -> 1 -> 2 -> 3 -> 2 -> 1 -> 0...
        int nextCandidate = currentPointIndex + direction;

        // Si se sale por arriba, rebotamos hacia atras
        if (nextCandidate >= globalPoints.Length)
        {
            // Si no hace loop y ya llego al final, se para aqui
            if (!loop)
                return false;

            direction = -1;
            nextCandidate = currentPointIndex + direction;
        }
        // Si se sale por abajo, rebotamos hacia delante
        else if (nextCandidate < 0)
        {
            // Si no hace loop y ya volvio al inicio, se para aqui
            if (!loop)
                return false;

            direction = 1;
            nextCandidate = currentPointIndex + direction;
        }

        nextPointIndex = nextCandidate;
        return true;
    }

        private void OnCollisionEnter(Collision collision)
        {
            // Si choca con el player, guardamos su transform
            if (collision.transform.CompareTag("Player")&&player==null)
            {
                player = collision.transform;
            }
        }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Si no hay puntos, no dibujamos nada
        if (localPoints == null || localPoints.Length == 0)
            return;

        // Dibujamos los puntos y las lineas entre ellos
        Gizmos.color = Color.red;

        Vector3[] worldPoints = new Vector3[localPoints.Length];
        for (int i = 0; i < localPoints.Length; i++)
        {
            worldPoints[i] = transform.TransformPoint(localPoints[i]);
            Gizmos.DrawSphere(worldPoints[i], 0.2f);
        }

        // Dibuja las conexiones entre puntos
        for (int i = 0; i < worldPoints.Length - 1; i++)
        {
            Gizmos.DrawLine(worldPoints[i], worldPoints[i + 1]);
        }

        // Si closeLoop esta activo, dibuja tambien la linea del ultimo al primero
        if (closeLoop && worldPoints.Length > 1)
        {
            Gizmos.DrawLine(worldPoints[worldPoints.Length - 1], worldPoints[0]);
        }
    }
#endif
}