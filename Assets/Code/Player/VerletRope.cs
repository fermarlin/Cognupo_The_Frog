using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Este script es para hacer cuerdas, en nuestro caso es para la lengua de nuestro protagonista.

public class VerletRope : MonoBehaviour
{
    // =========================
    // CONFIG
    // =========================
    
    [SerializeField] private int pointsNb = 20;   // Numero de puntos de la cuerda
    public float maxDistance = 5f;                // Longitud maxima permitida entre fixedPoint y connectedPoint
    public Transform fixedPoint;           // Punto fijo/ancla
    public Transform connectedPoint;         // Punto conectado (nuestra ranita)

    [Header("Visual")]
    [SerializeField] private float startWidth = 1f;      // Grosor al inicio
    [SerializeField] private float middleWidth = 0.5f;   // Grosor en medio
    [SerializeField] private float endWidth = 1f;        // Grosor al final
    [SerializeField] private Transform visualConnectedPoint;
    [SerializeField] private Vector3 visualFixedPointOffset = Vector3.zero; // Offset visual del punto fijo
    
    [Header("Swing Spawn")]
    [SerializeField] private GameObject swingFixedPointPrefab;
    [SerializeField] private bool parentSpawnToFixedPoint = true;
    [SerializeField] private Vector3 swingSpawnOffset = Vector3.zero;

    private GameObject currentSwingFixedPointInstance;
    // =========================
    // RECOGER LA CUERDA
    // =========================

    [Header("Reel In")]
    public float desiredMaxDistance = 5f; // A que longitud queremos llegar 
    public float reelInSpeed = 20f; // Velocidad para cambiar entre maxDistance y desiredMaxDistance

    // =========================
    // ATTACHED POINTS
    // =========================

    [System.Serializable]
    public class AttachedPoint
    {
        public int id = 0; // indice del punto en la cuerda (0..pointsNb-1)
        public Transform transform; // Transform al que se pega
        [HideInInspector] public Vector3 force = Vector3.zero;

        public AttachedPoint(int _id, Transform _transform)
        {
            id = _id;
            transform = _transform;
        }

        public bool IsValid(int maxPoints = 0)
        {
            // Esto es una comprobacion de que el id exista dentro del array y el transform no sea null
            return !(id < 0 || id >= maxPoints || transform == null);
        }
    }

    // =========================
    // CONFIG DE CUERDA
    // =========================

    [Header("Rope")]
    [SerializeField] private float attachedBodiesDamping = 0.5f; // Damping al aplicar la fuerza a los objetos enganchados
    public List<AttachedPoint> attachedPoints = new List<AttachedPoint>();

    [HideInInspector] public Vector3[] pos;  // posicion actual de cada punto
    [HideInInspector] public Vector3[] prevPos; // posicion previa
    [HideInInspector] public float[] mass;    // masa (0 = anclado)

    // =========================
    // CONSTRAINTS
    // =========================

    [Space]
    [SerializeField] private float constraintDistance = 0.1f; // Distancia ideal entre puntos
    [SerializeField] private int constraintDistanceIterations = 20;  // Este valor cuanto mas alto mas rigido

    // =========================
    // VISUAL
    // =========================

    private LineRenderer line;

    private void Awake()
    {
        // Pillamos el line renderer que pinta la cuerda
        line = GetComponent<LineRenderer>();

        if (line != null)
        {
            line.enabled = false;

            AnimationCurve widthCurve = new AnimationCurve(
                new Keyframe(0f, startWidth),
                new Keyframe(0.5f, middleWidth),
                new Keyframe(1f, endWidth)
            );

            line.widthCurve = widthCurve;
        }
    }

    private void Start()
    {
        // Creamos arrays y distribuimos puntos iniciales
        CreatePoints();
        UpdateRuntimeAttachments();
    }

    private void LateUpdate()
    {
        UpdateRuntimeAttachments();
        UpdateSwingFixedPointInstance();
        if (line != null)
            line.enabled = (fixedPoint != null && connectedPoint != null);

        if (pointsNb > 1 && fixedPoint != null)
        {
            if (maxDistance > desiredMaxDistance)
            {
                maxDistance = Mathf.MoveTowards(maxDistance, desiredMaxDistance, reelInSpeed * Time.deltaTime);
            }
            // Esto intenta asegurarse de que connectedPoint no se quede mas lejos de maxDistance.
            EnforceMaxDistanceOnConnected(); 
            ApplyForces();// aplica la fuerza que sea necesaria a los gameobjects cercanos
            ApplyAttach(); // fija los puntos anclados y calcula fuerzas
            ApplyVerlet();  // esta funcion se encarga de la inercia y la gravedad
            ApplyConstraints(); // constraints de distancia

            int last = pointsNb - 1;
            Vector3 dir = pos[last] - fixedPoint.position;
            float dist = dir.magnitude;

            if (dist > maxDistance)
            {
                Vector3 limitedPos = fixedPoint.position + dir.normalized * maxDistance;
                pos[last] = limitedPos;
                prevPos[last] = limitedPos;
            }
        }
        //Esto es para dibujar la linea
        if (line != null && pos != null && pos.Length == pointsNb && fixedPoint != null)
        {
            Vector3[] renderPositions = GetVisualRopePositions();
            line.SetPositions(renderPositions);
        }
        
    }

    private bool IsSwinging()
    {
        return fixedPoint != null && connectedPoint != null;
    }

    private void UpdateSwingFixedPointInstance()
    {
        if (!IsSwinging() || swingFixedPointPrefab == null)
        {
            if (currentSwingFixedPointInstance != null)
            {
                Destroy(currentSwingFixedPointInstance);
                currentSwingFixedPointInstance = null;
            }
            return;
        }

        if (currentSwingFixedPointInstance == null)
        {
            Vector3 spawnPos = fixedPoint.position + fixedPoint.TransformDirection(swingSpawnOffset);

            if (parentSpawnToFixedPoint)
            {
                currentSwingFixedPointInstance = Instantiate(
                    swingFixedPointPrefab,
                    spawnPos,
                    fixedPoint.rotation,
                    fixedPoint
                );
            }
            else
            {
                currentSwingFixedPointInstance = Instantiate(
                    swingFixedPointPrefab,
                    spawnPos,
                    fixedPoint.rotation
                );
            }
        }

        if (currentSwingFixedPointInstance != null)
        {
            currentSwingFixedPointInstance.transform.position =
                fixedPoint.position + fixedPoint.TransformDirection(swingSpawnOffset);

            if (!parentSpawnToFixedPoint)
                currentSwingFixedPointInstance.transform.rotation = fixedPoint.rotation;
        }
    }

    private void UpdateRuntimeAttachments()
    {
        // El primer punto de la cuerda siempre va al ancla
        if (fixedPoint != null)
        {
            AttachPoint(0, fixedPoint);
        }

        // El ultimo punto siempre va al jugador
        if (connectedPoint != null)
        {
            AttachPoint(pointsNb - 1, connectedPoint);
        }
    }

    // Creamos arrays y distribuimos puntos inicialesç
    private void CreatePoints()
    {
        pos = new Vector3[pointsNb];
        prevPos = new Vector3[pointsNb];
        mass = new float[pointsNb];

        // Tiramos la cuerda hacia abajo en direccion de gravedad (asi sale colgando al inicio)
        Vector3 targetPos = transform.position + Physics.gravity.normalized * (constraintDistance * pointsNb);
     
        // Si hay attachedPoints, y el ultimo es valido, hacemos que el target inicial sea su transform
        if (attachedPoints.Count > 1)
        {
            AttachedPoint lastPoint = attachedPoints[attachedPoints.Count - 1];
            if (lastPoint.IsValid(pointsNb)) targetPos = lastPoint.transform.position;
        }

        // Distribuimos los puntos en linea recta entre transform.position y targetPos
        for (int i = 0; i < pointsNb; i++)
        {
            pos[i] = Vector3.Lerp(transform.position, targetPos, (float)i / (pointsNb - 1));
            prevPos[i] = pos[i];
            mass[i] = 1.0f;
        }

        if (line) line.positionCount = pointsNb;
    }

    //Esta funcion como he comentado antes es para anclar los puntos de la cuerda
    public AttachedPoint AttachPoint(int id, Transform attach)
    {      
          // Creamos un punto anclado a un transform. Si ya existe id, lo actualizamos.
        AttachedPoint newPoint = new AttachedPoint(id, attach);

        for (int i = 0; i < attachedPoints.Count; i++)
        {
            var point = attachedPoints[i];

            if (point.id == id)
            {
                // Si ya existia actualizamos el transform y reseteamos la fuerza

                point.transform = attach;
                point.force = Vector3.zero;
                return point;
            }
            else if (point.id > id)
            {
                // Si no pues lo inserto
                attachedPoints.Insert(i, newPoint);
                return newPoint;
            }
        }

        attachedPoints.Add(newPoint);
        return newPoint;
    }


    /*  Calcula correcciones para que la distancia entre p1 y p2 sea "distance".
        Devuelve "difference" (magnitud relativa del error).
        constraint[0] y constraint[1] son los desplazamientos a aplicar a pos[p1] y pos[p2].
    */
    private float GetConstraint(int p1, int p2, float distance, Vector3[] constraint, bool useMass = true)
    {
        Vector3 delta = pos[p2] - pos[p1];
        float length = delta.magnitude;
        float difference;

        if (useMass)
        {
            // Si usamos masa el punto mas ligero es el que se corrige mas
            // Si la masa es 0 no se mueve (el punto que tenemos anclado)
            float inv1 = InverseMass(mass[p1]);
            float inv2 = InverseMass(mass[p2]);

            difference = (length - distance) / (length * (inv1 + inv2));
            constraint[0] = delta * difference * inv1;
            constraint[1] = -delta * difference * inv2;
        }
        else
        {
            // Sin masa repartimos 50/50

            difference = (length - distance) / length;
            constraint[0] = delta * difference * 0.5f;
            constraint[1] = -delta * difference * 0.5f;
        }

        return difference;
    }

    private void ApplyVerlet()
    {
        for (int i = 0; i < pointsNb; i++)
        {
            // suma a pos lo mismo que se movio en el frame anterior

            Vector3 temp = pos[i];
            pos[i] += pos[i] - prevPos[i];

            // Gravedad, aqui se multiplica por mass[i]
            float dt = Time.deltaTime;
            pos[i] += mass[i] * Physics.gravity * dt * dt;

            prevPos[i] = temp;
        }
    }

    // constraints de distancia
    private void ApplyConstraints()
    {
        Vector3[] constraint = new Vector3[2];
        // Cuantas mas iteraciones mas rigida la cuerda
        for (int iter = 0; iter < constraintDistanceIterations; iter++)
        {
            for (int i = 1; i < pointsNb; i++)
            {               
                 // Constraint de distancia entre i-1 e i
                GetConstraint(i - 1, i, constraintDistance, constraint);
                pos[i - 1] += constraint[0];
                pos[i] += constraint[1];
            }
        }
    }

    // fija los puntos anclados y calcula fuerzas
    private void ApplyAttach()
    {
        Vector3[] constraint = new Vector3[2];
        AttachedPoint prevPoint = null;

        foreach (var point in attachedPoints)
        {
            if (!point.IsValid(pointsNb)) continue;

            // Este punto esta pegado
            pos[point.id] = point.transform.position;
            prevPos[point.id] = point.transform.position;
            mass[point.id] = 0.0f;

            // Si tenemos un prevPoint, aplicamos un constraint entre ambos segun el span
            if (prevPoint != null)
            {
                int span = point.id - prevPoint.id;
                float diff = GetConstraint(prevPoint.id, point.id, constraintDistance * span, constraint);
                
                // Si estan mas lejos de lo debido hay tension
                // Guardamos fuerzas para luego aplicarlas a rigidbodies
                if (diff > 0f)
                {
                    prevPoint.force += constraint[0];
                    point.force = constraint[1];
                }
            }
            else
            {
                // Primer punto anclado por lo que no tiene fuerza previa
                point.force = Vector3.zero;
            }

            prevPoint = point;
        }
    }

    // aplica la fuerza que sea necesaria a los gameobjects cercanos
    private void ApplyForces()
    {
        foreach (var point in attachedPoints)
        {
            if (!point.IsValid(pointsNb)) continue;

            // Si el transform tiene Rigidbody, aplicamos fuerza como cambio directo de velocidad
            var body = point.transform.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                 // Guardamos la masa real del body en el punto
                mass[point.id] = body.mass;

                // Empujamos su velocidad con la tension calculada
                body.linearVelocity += point.force * attachedBodiesDamping;
            }
        }
    }

    private static float InverseMass(float m)
    {
        /* El movimiento que recibe cada punto suele ser proporcional al inverso de su masa, por si te preguntas
         porque lo usamos devolvemos un valor muy pequeno para evitar dividir por 0*/
        return m == 0f ? 1e-8f : 1.0f / m;
    }

    //Esto intenta asegurarse de que connectedPoint no se quede mas lejos de maxDistance.
    private void EnforceMaxDistanceOnConnected()
    {       
         //Por si acaso para que no haya errores
        if (fixedPoint == null || connectedPoint == null) return;

        Vector3 delta = connectedPoint.position - fixedPoint.position;
        float dist = delta.magnitude;
        if (dist <= Mathf.Epsilon) return;

        // Si se pasa de maxDistance movemos el connectedPoint a la esfera de radio maxDistance y le quitamos velocidad que siga alejandolo 
        if (dist > maxDistance)
        {
            Vector3 dir = delta / dist;
            Vector3 clampedPos = fixedPoint.position + dir * maxDistance;

            Rigidbody rb = connectedPoint.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {  
                // Proyeccion directa a la esfera de radio maxDistance
                rb.position = clampedPos;

                // Quitar componente de velocidad que se aleja del ancla
                float awaySpeed = Vector3.Dot(rb.linearVelocity, dir);
                if (awaySpeed > 0f)
                    rb.linearVelocity -= dir * awaySpeed;
            }
            else
            {
            
                // Si no hay rigidbody, movemos el transform a pelo
                connectedPoint.position = clampedPos;
            }
        }
    }

    private Vector3[] GetVisualRopePositions()
    {
        Vector3[] renderPositions = new Vector3[pointsNb];

        // Si no hay punto visual, dibujo la cuerda normal
        if (visualConnectedPoint == null || fixedPoint == null || connectedPoint == null)
        {
            for (int i = 0; i < pointsNb; i++)
                renderPositions[i] = pos[i];

            return renderPositions;
        }

        Vector3 logicStart = fixedPoint.position;
        Vector3 logicEnd = connectedPoint.position;

        Vector3 visualStart = fixedPoint.position + fixedPoint.TransformDirection(visualFixedPointOffset);
        Vector3 visualEnd = visualConnectedPoint.position;

        Vector3 logicDir = logicEnd - logicStart;
        Vector3 visualDir = visualEnd - visualStart;

        float logicLength = logicDir.magnitude;
        float visualLength = visualDir.magnitude;

        // Si la longitud logica es demasiado pequeña, no intentamos remapear
        if (logicLength <= 0.0001f)
        {
            for (int i = 0; i < pointsNb; i++)
                renderPositions[i] = pos[i];

            return renderPositions;
        }

        Quaternion rotation = Quaternion.FromToRotation(logicDir.normalized, visualDir.normalized);
        float scale = visualLength / logicLength;

        for (int i = 0; i < pointsNb; i++)
        {
            // Posicion relativa respecto al inicio logico de la cuerda
            Vector3 localOffset = pos[i] - logicStart;

            // Rotamos toda la forma para alinearla con el nuevo extremo visual
            Vector3 rotatedOffset = rotation * localOffset;

            // Escalamos para que encaje en la nueva distancia visual
            Vector3 scaledOffset = rotatedOffset * scale;

            // La recolocamos desde el inicio visual
            renderPositions[i] = visualStart + scaledOffset;
        }

        // Reforzamos extremos exactos
        renderPositions[0] = visualStart;
        renderPositions[pointsNb - 1] = visualEnd;

        return renderPositions;
    }
}