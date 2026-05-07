using UnityEngine;

// Este script mueve un objeto entre varios puntos de patrulla.
// Los puntos se configuran igual que en PlatformMover son puntos locales respecto al objeto en el inspector.
public class PatrolBetweenPoints : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Vector3[] localPoints;             // Lista de puntos locales por los que patrullara
    [SerializeField] private float moveSpeed = 4f;              // Velocidad de movimiento
    [SerializeField] private float rotationSpeed = 8f;          // Velocidad de giro
    [SerializeField] private float patrolPointDistance = 0.3f;  // Distancia minima para considerar que ha llegado al punto
    [SerializeField] private bool loop = true;                  // Si true, sigue patrullando indefinidamente
    [SerializeField] private bool closeLoop = false;            // Si true, al llegar al ultimo vuelve al primero. Si false, hace ida y vuelta

    private Vector3[] globalPoints; // Puntos locales convertidos a posiciones globales

    private int currentPointIndex = 0; // Punto en el que esta ahora
    private int nextPointIndex = 0;    // Punto al que se dirige
    private int direction = 1;         // 1 = avanza, -1 = retrocede

    private bool hasFinished = false;  // Para saber si ya termino el estado de patrulla
    //[HideInInspector]
    public bool isPatrolling = false;

    private void Start()
    {
        // Si no hay puntos, no hacemos nada
        if (localPoints == null || localPoints.Length == 0)
        {
            Debug.LogWarning("PatrolBetweenPoints: No hay puntos configurados.");
            hasFinished = true;
            return;
        }

        // Convertimos todos los puntos locales a globales.
        // Esto hace que funcione igual que PlatformMover.
        globalPoints = new Vector3[localPoints.Length];

        for (int i = 0; i < localPoints.Length; i++)
        {
            globalPoints[i] = transform.TransformPoint(localPoints[i]);
        }

        // Colocamos el objeto en el primer punto de patrulla.
        transform.position = globalPoints[0];

        // Empezamos desde el primer punto
        currentPointIndex = 0;

        // Si hay mas de un punto, el siguiente sera el segundo.
        // Si solo hay uno, no se movera.
        nextPointIndex = localPoints.Length > 1 ? 1 : 0;

        //Si solo hay un punto, no patrullo
        hasFinished = localPoints.Length <= 1;
    }

    public void Patrol()
    {
        Patrol(1f);
    }

    public void Patrol(float speedMultiplier)
    {
        // Si ya termino o no hay suficientes puntos, no se mueve
        if (hasFinished || globalPoints == null || globalPoints.Length <= 1){
            isPatrolling = false;
            return;
        }

        isPatrolling = true;
        // Punto al que se dirige ahora mismo
        Vector3 targetPoint = globalPoints[nextPointIndex];

        // Calculamos la direccion hacia el punto
        Vector3 directionToPoint = targetPoint - transform.position;

        // Quitamos la altura para que no intente inclinarse hacia arriba o abajo
        directionToPoint.y = 0f;

        // Si ya esta suficientemente cerca del punto, cambiamos al siguiente
        if (directionToPoint.magnitude <= patrolPointDistance)
        {
            // Ahora el punto al que ha llegado pasa a ser el punto actual
            currentPointIndex = nextPointIndex;

            //Calculamos cual sera el siguiente punto
            if (!CalculateNextPoint())
            {
                hasFinished = true;
                isPatrolling = false;
                return;
            }

            return;
        }

        // Normalizamos para movernos solo con direccion
        directionToPoint.Normalize();

        // Movemos el objeto hacia el punto
        transform.position += directionToPoint * moveSpeed * speedMultiplier * Time.deltaTime;

        // Giramos mirando hacia donde se mueve
        LookToDirection(directionToPoint);
    }

    private bool CalculateNextPoint()
    {
        // Si hay menos de 2 puntos, no hay recorrido
        if (globalPoints.Length <= 1)
            return false;

        if (closeLoop)
        {
            int candidate = currentPointIndex + 1;

            // Si se pasa del ultimo, vuelve al primero
            if (candidate >= globalPoints.Length)
            {
                // Si loop es false, termina al llegar al final
                if (!loop)
                    return false;

                candidate = 0;
            }

            nextPointIndex = candidate;
            return true;
        }

        // Modo ida y vuelta
        int nextCandidate = currentPointIndex + direction;

        // Si se sale por arriba, rebotamos hacia atras
        if (nextCandidate >= globalPoints.Length)
        {
            // Si loop es false, termina al llegar al ultimo punto
            if (!loop)
                return false;

            direction = -1;
            nextCandidate = currentPointIndex + direction;
        }
        // Si se sale por abajo, rebotamos hacia delante
        else if (nextCandidate < 0)
        {
            // Si loop es false, termina al volver al primer punto
            if (!loop)
                return false;

            direction = 1;
            nextCandidate = currentPointIndex + direction;
        }

        nextPointIndex = nextCandidate;
        return true;
    }

    private void LookToDirection(Vector3 direction)
    {
        // Si no hay direccion, no giramos
        if (direction.sqrMagnitude <= 0.001f)
            return;

        // Calculamos la rotacion mirando hacia la direccion de movimiento
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Giramos suavemente hacia esa rotacion
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Si no hay puntos, no dibujamos nada
        if (localPoints == null || localPoints.Length == 0)
            return;

        Gizmos.color = Color.blue;

        Vector3[] worldPoints = new Vector3[localPoints.Length];

        // Dibujamos cada punto en el mundo
        for (int i = 0; i < localPoints.Length; i++)
        {
            worldPoints[i] = transform.TransformPoint(localPoints[i]);
            Gizmos.DrawSphere(worldPoints[i], 0.2f);
        }

        // Dibujamos lineas entre los puntos
        for (int i = 0; i < worldPoints.Length - 1; i++)
        {
            Gizmos.DrawLine(worldPoints[i], worldPoints[i + 1]);
        }

        // Si closeLoop esta activo, dibujamos tambien la linea del ultimo al primero
        if (closeLoop && worldPoints.Length > 1)
        {
            Gizmos.DrawLine(worldPoints[worldPoints.Length - 1], worldPoints[0]);
        }
    }
#endif
}