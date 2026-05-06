using UnityEngine;

// Este script se encarga de gestionar la lengua cuando se ataca
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform startPivot;          // Punto exacto desde el que sale y al que vuelve
    [SerializeField] private GameObject tonguePrefab;       // Prefab de la punta de la lengua que vamos a instanciar
    [SerializeField] private LineRenderer lineRenderer;     // LineRenderer que une el pivot con la punta de la lengua
    [SerializeField] private TargetLockHandler targetLockHandler; // Para saber si estamos haciendo ZTarget

    [Header("Launch Settings")]
    [SerializeField] private float forwardDistance = 13f;    // Cuanto avanza hacia delante desde el pivot
    [SerializeField] private float moveSpeed = 70f;         // Velocidad tanto de ida como de vuelta
    [SerializeField] private bool useLocalForward = true;   // Si true usa el forward del pivot, si false el de este objeto
    [SerializeField] private float forwardDistanceZtarget = 10;    // Cuanto avanza hacia delante desde el pivot cuando hace ztarget
    [SerializeField] private float moveSpeedZtarget = 50f;         // Velocidad tanto de ida como de vuelta cuando hace ztarget

    [Header("Visual")]
    [SerializeField] private int visualPoints = 20;         // Numero de puntos visuales de la linea, como en VerletRope
    [SerializeField] private float startWidth = 1f;         // Grosor al inicio
    [SerializeField] private float middleWidth = 0.5f;      // Grosor en medio
    [SerializeField] private float endWidth = 1f;           // Grosor al final
    [SerializeField] private float offsetTongueTip = 1f;    // Offset de la punta de la lengua
    [SerializeField] private Transform visualConnectedPoint; // Punto visual final opcional
    [SerializeField] private Vector3 visualFixedPointOffset = Vector3.zero; // Offset visual del inicio
    [SerializeField] private Transform headToRotate;      // Cabeza que mirara hacia la punta de la lengua
    
    [Header("Runtime")]
    [SerializeField] private bool launchOnStart = false;    // Por si quieres probarlo nada mas empezar

    private Transform currentTongueTip;                     // Punta real que vamos moviendo
    private GameObject tongueInstance;                      // Instancia visual del prefab
    private bool isLaunching = false;                       // Guarda si ahora mismo esta haciendo el recorrido
    private bool isReturning = false;                       // Guarda si ahora mismo esta volviendo
    private Vector3 targetPosition;                         // Punto maximo al que tiene que llegar
    private Vector3 originalPivotRot;


    private void Start()
    {
        // Si no hay pivot usamos este mismo transform
        if (startPivot == null)
            startPivot = transform;
        
        originalPivotRot = startPivot.localEulerAngles;

        // Creamos una punta dummy reutilizable
        GameObject tongueTipObj = new GameObject("TongueAttackTip");
        currentTongueTip = tongueTipObj.transform;
        currentTongueTip.position = startPivot.position;

        // Si hay prefab visual, lo instanciamos una sola vez y lo reutilizamos
        if (tonguePrefab != null)
        {
            tongueInstance = Instantiate(tonguePrefab, startPivot.position, Quaternion.identity);
            tongueInstance.SetActive(false);
        }

        // Dejamos la linea apagada al inicio y configuramos su visual
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = Mathf.Max(2, visualPoints);
            ApplyLineVisual();
        }

        if (launchOnStart)
            LaunchTongue();
    }

    private void Update()
    {
        if (currentTongueTip == null) return;
        // Mientras este activa la lengua, la linea se actualiza cada frame
        if (lineRenderer != null && lineRenderer.enabled)
        {
            UpdateLine();
        }

        // Si no hay lanzamiento activo no seguimos
        if (!isLaunching && !isReturning) return;

        // Ida
        if (isLaunching)
        {
            if(!targetLockHandler.IsTargeting){
                currentTongueTip.position = Vector3.MoveTowards(
                    currentTongueTip.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );
            }else{
                currentTongueTip.position = Vector3.MoveTowards(
                    currentTongueTip.position,
                    targetPosition,
                    moveSpeedZtarget * Time.deltaTime
                );
            }

            if (tongueInstance != null)
                tongueInstance.transform.position = currentTongueTip.position+Vector3.up*offsetTongueTip;

            // Cuando llega al final, empieza a volver
            if (Vector3.Distance(currentTongueTip.position, targetPosition) < 0.01f)
            {
                isLaunching = false;
                isReturning = true;
            }
        }
        // Vuelta
        else if (isReturning)
        {
            Vector3 returnTarget = startPivot.position;

            if(!targetLockHandler.IsTargeting){
                currentTongueTip.position = Vector3.MoveTowards(
                    currentTongueTip.position,
                    returnTarget,
                    moveSpeed * Time.deltaTime
                );
            }else{
                    currentTongueTip.position = Vector3.MoveTowards(
                    currentTongueTip.position,
                    returnTarget,
                    moveSpeedZtarget * Time.deltaTime
                );
            }

            if (tongueInstance != null)
                tongueInstance.transform.position = currentTongueTip.position+Vector3.up*offsetTongueTip;

            if (Vector3.Distance(currentTongueTip.position, returnTarget) < 0.01f)
            {
                currentTongueTip.position = returnTarget;

                if (tongueInstance != null)
                    tongueInstance.transform.position = returnTarget;

                isReturning = false;
                EndLaunch();
            }
        }
    }

    private void LateUpdate()
    {
        UpdateHeadLook();
    }

    public void LaunchTongue()
    {
        // Si ya esta activa la lengua no lanzamos otra
        if (isLaunching || isReturning) return;

        // Si falta algo importante salimos
        if (startPivot == null || currentTongueTip == null) return;

        // Recalculamos por si el pivot se ha movido
        currentTongueTip.position = startPivot.position;

        if (tongueInstance != null)
        {
            tongueInstance.transform.position = startPivot.position;
            tongueInstance.SetActive(true);
        }

        Vector3 forwardDir = useLocalForward ? startPivot.forward : transform.forward;
        if(!targetLockHandler.IsTargeting){
            targetPosition = startPivot.position + forwardDir.normalized * forwardDistance;
        }else  targetPosition = startPivot.position + forwardDir.normalized * forwardDistanceZtarget;


        // Activamos estados
        isLaunching = true;
        isReturning = false;

        // Encendemos la linea
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = Mathf.Max(2, visualPoints);
            ApplyLineVisual();
            UpdateLine();
        }
    }

    public void CancelAndReturn()
    {
        // Si no esta activa la lengua no hacemos nada
        if (!isLaunching && !isReturning) return;

        // Cortamos la ida y forzamos la vuelta
        isLaunching = false;
        isReturning = true;
    }

    private void EndLaunch()
    {
        isLaunching = false;
        isReturning = false;

        if (currentTongueTip != null)
            currentTongueTip.position = startPivot.position;

        if (tongueInstance != null)
        {
            tongueInstance.transform.position = startPivot.position;
            tongueInstance.SetActive(false);
        }

        // Apagamos la linea
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void UpdateLine()
    {
        if (lineRenderer == null || currentTongueTip == null || startPivot == null) return;

        Vector3[] renderPositions = GetVisualTonguePositions();
        lineRenderer.positionCount = renderPositions.Length;
        lineRenderer.SetPositions(renderPositions);
    }
    

    private void ApplyLineVisual()
    {
        if (lineRenderer == null) return;

        lineRenderer.widthMultiplier = 1f;

        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0f, startWidth),
            new Keyframe(0.5f, middleWidth),
            new Keyframe(1f, endWidth)
        );

        lineRenderer.widthCurve = widthCurve;
    }

    private void UpdateHeadLook()
    {
        // Si falta alguna referencia importante, salimos
        if (headToRotate == null || targetLockHandler == null || currentTongueTip == null) return;

        // =========================================================
        // 1. Si estamos haciendo ZTarget, la cabeza/pivote mira al enemigo
        // =========================================================
        if (targetLockHandler.IsTargeting)
        {
            Transform currentTarget = targetLockHandler.GetCurrentTarget();
            if (currentTarget == null) return;

            // La direccion correcta es:
            // desde el pivote hacia el objetivo
            Vector3 dirToTarget = currentTarget.position - startPivot.position;

            // Evitamos errores si estan practicamente en el mismo punto
            if (dirToTarget.sqrMagnitude < 0.0001f) return;

            // Rotacion global para que mire directamente al objetivo
            startPivot.rotation = Quaternion.LookRotation(dirToTarget.normalized, Vector3.up);
            return;
        }

        // Si no estamos en ZTarget, devolvemos el pivote a su rotacion original
        startPivot.localEulerAngles = originalPivotRot;

        // =========================================================
        // 2. Si la lengua no esta activa, no seguimos rotando la cabeza
        // =========================================================
        if (!isLaunching && !isReturning) return;

        // La cabeza mira hacia la punta actual de la lengua
        Vector3 dirToTongueTip = headToRotate.position - currentTongueTip.position;

        // Evitamos errores si estan practicamente en el mismo punto
        if (dirToTongueTip.sqrMagnitude < 0.0001f) return;

        // Rotacion global de la cabeza hacia la punta de la lengua
        headToRotate.rotation = Quaternion.LookRotation(dirToTongueTip.normalized, Vector3.up);
    }

    private Vector3[] GetVisualTonguePositions()
    {
        int count = Mathf.Max(2, visualPoints);
        Vector3[] renderPositions = new Vector3[count];

        Vector3 logicStart = startPivot.position;
        Vector3 logicEnd = currentTongueTip.position;

        // Si no hay punto visual extra, dibujamos directamente entre inicio y punta,
        // pero con varios puntos para que visualmente se parezca mas al otro script
        if (visualConnectedPoint == null)
        {
            Vector3 visualStartSimple = startPivot.position + startPivot.TransformDirection(visualFixedPointOffset);
            Vector3 visualEndSimple = currentTongueTip.position;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                renderPositions[i] = Vector3.Lerp(visualStartSimple, visualEndSimple, t);
            }

            return renderPositions;
        }

        // Esta parte copia la idea de VerletRope:
        // la logica va entre startPivot y currentTongueTip,
        // pero visualmente queremos dibujarla entre otro inicio y otro final
        Vector3 visualStart = startPivot.position + startPivot.TransformDirection(visualFixedPointOffset);
        Vector3 visualEnd = visualConnectedPoint.position;

        Vector3 logicDir = logicEnd - logicStart;
        Vector3 visualDir = visualEnd - visualStart;

        float logicLength = logicDir.magnitude;
        float visualLength = visualDir.magnitude;

        // Si la longitud logica es demasiado pequeña, devolvemos linea recta visual
        if (logicLength <= 0.0001f)
        {
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                renderPositions[i] = Vector3.Lerp(visualStart, visualEnd, t);
            }

            return renderPositions;
        }

        Quaternion rotation = Quaternion.FromToRotation(logicDir.normalized, visualDir.normalized);
        float scale = visualLength / logicLength;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);

            // Creamos una linea logica con varios puntos intermedios
            Vector3 logicalPoint = Vector3.Lerp(logicStart, logicEnd, t);

            // La pasamos a local respecto al inicio logico
            Vector3 localOffset = logicalPoint - logicStart;

            // Rotamos y escalamos igual que hace VerletRope
            Vector3 rotatedOffset = rotation * localOffset;
            Vector3 scaledOffset = rotatedOffset * scale;

            // Colocamos el punto en su sitio visual
            renderPositions[i] = visualStart + scaledOffset;
        }

        // Reforzamos extremos exactos
        renderPositions[0] = visualStart;
        renderPositions[count - 1] = visualEnd;

        return renderPositions;
    }
}