using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine; 
using System.Linq;
using UnityEngine.InputSystem;

// Este script se encarga de gestionar el fijado de objetivos (Target Lock)
// Es el que decide a que enemigo mirar y controla las camaras de Cinemachine.

public class TargetLockHandler : MonoBehaviour
{
    // =========================
    // REFERENCES
    // =========================
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Animator cameraAnimator;        // Para disparar los States del Animator de la camara
    [SerializeField] private CinemachineCamera freeLookCamera; 
    [SerializeField] private CinemachineCamera targetLockCamera;
    [SerializeField] private CinemachineTargetGroup targetGroup; // El grupo que contiene al player y al enemigo para que la camara los encuadre
    [SerializeField] private GameObject targetMarkerPrefab;
    private GameObject targetMarker;             // El indicador visual sobre el enemigo, de momento es un cubito pero habra que cambiarlo
    public bool IsTargeting => activeTarget && currentTarget != null;
    public Transform GetCurrentTarget() => currentTarget;
    
    // =========================
    // CONFIG
    // =========================
    [Header("Settings")]
    public float enemyDetectRange = 20f;       // Radio de la esfera de busqueda
    public LayerMask enemyLayer;               // Layer de los enemigos
    public LayerMask obstacleLayer;            // Objetos que tapan la vision
    public float targetSwitchCooldown = 0.5f;  // Para no saltar entre enemigos como locos si spameamos el cambiar de objetivo

    [Header("Smooth Target Group")]
    [SerializeField] private float targetProxySmoothTime = 0.15f; // Cuanto tarda el punto de camara en llegar al nuevo enemigo
    [SerializeField] private float lostTargetSwitchCooldown = 0.1f; // Pequeño margen para no hacer varios cambios si mueren varios enemigos a la vez

    [Header("Target Marker")]
    [SerializeField] private float markerExtraHeight = 0.3f;       // Altura extra por encima del collider
    [SerializeField] private float markerDefaultHeight = 2f;       // Altura si no tiene collider

    // =========================
    // ESTADO RUNTIME
    // =========================
    private bool activeTarget = false;
    private List<Transform> nearbyTargets = new List<Transform>();
    private Transform currentTarget;
    private float lastSwitchTime;
    private float lastLostTargetTime;

    // En vez de meter el enemigo directamente en el TargetGroup, metemos este punto invisible, asi, si el enemigo se destruye, Cinemachine no pierde el target de golpe y no pega tiron.
    private Transform targetProxy;
    private Vector3 targetProxyVelocity;

    public System.Action<bool> onTargetLock;   // Evento por si otros scripts quieren reaccionar a que hemos targeteado algo
    
    [SerializeField] private PlayerMovement playerMovement;

    private PlayerInputs inputs;

    private void Awake()
    {
        // Inicializamos el imput
        inputs = new PlayerInputs();

        CreateTargetProxyIfNeeded();
    }

    private void OnEnable()
    {
        inputs.Enable();

        // Suscribimos las funciones a los eventos del Input System
        inputs.PlayerActionMap.Focus.performed += OnFocusPerformed;
        inputs.PlayerActionMap.SwitchFocusLeft.performed += OnSwitchLeftPerformed;
        inputs.PlayerActionMap.SwitchFocusRight.performed += OnSwitchRightPerformed;

        // Nos enganchamos al evento de movimiento para saber si el player empieza a columpiarse
        if (playerMovement != null)
            playerMovement.OnMovementStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        // Limpieza de eventos para no dejar basura en memoria
        inputs.PlayerActionMap.Focus.performed -= OnFocusPerformed;
        inputs.PlayerActionMap.SwitchFocusLeft.performed -= OnSwitchLeftPerformed;
        inputs.PlayerActionMap.SwitchFocusRight.performed -= OnSwitchRightPerformed;
        inputs.Disable();

        if (playerMovement != null)
            playerMovement.OnMovementStateChanged -= HandleStateChanged;
    }

    private void LateUpdate()
    {
        // Si estamos fijando target, actualizamos la posicion del marker cada frame.
        // Lo hacemos en LateUpdate para que se coloque despues de que el enemigo se haya movido.
        if (activeTarget && currentTarget != null && targetMarker != null)
        {
            UpdateTargetMarkerPosition();
        }

        // Actualizamos el proxy de la camara tambien en LateUpdate.
        // Esto evita que el TargetGroup cambie de golpe cuando el enemigo se mueve o se cambia de target.
        UpdateTargetProxyPosition();
    }


    void FixedUpdate()
    {
        // Si teniamos lock pero el target ha sido destruido, buscamos otro target antes de soltar la camara.
        // Esto evita el tiron cuando un elemento del TargetGroup desaparece.
        if (activeTarget && currentTarget == null)
        {
            HandleLostCurrentTarget();
            return;
        }

        // Si el enemigo se va demasiado lejos, soltamos el lock automaticamente
        if (activeTarget && currentTarget != null)
        {
            float distance = Vector3.Distance(playerTransform.position, currentTarget.position);

            // Le damos un margen de 5 metros extra para que si el player esta en el limite
            // y le damos no se active y desactive automaticamente
            if (distance > enemyDetectRange + 5f)
            {
                LockTarget(false);
            }
        }
    }

    // =========================
    // LOGICA DE SELECCIoN
    // =========================

    void TrySwitchTarget(bool switchLeft)
    {
        // Si no ha pasado el cooldown, ignoramos el cambio
        if (Time.time < lastSwitchTime + targetSwitchCooldown) return;

        SwitchTargetLogic(switchLeft);
    }

    void SwitchTargetLogic(bool switchLeft)
    {
        // Volvemos a escanear que hay cerca para asegurarnos de que la lista esta al dia
        Transform dummy;
        CollectTargetsAndGetMostInFront(out dummy);

        if (nearbyTargets.Count == 0) return;

        int currentIndex = nearbyTargets.IndexOf(currentTarget);
        
        // Si por lo que sea el enemigo actual ya no es valido, empezamos desde el primero
        if (currentIndex == -1) currentIndex = 0;

        // Navegamos por la lista de forma circular
        int newIndex;

        if (switchLeft) 
        {
            // Si pulsamos el boton de la izquierda, retrocedemos en la lista
            newIndex = currentIndex - 1; 
        }
        else 
        {
            // Si no sera el de la derecha, avanzamos al siguiente enemigo
            newIndex = currentIndex + 1; 
        }

        if (newIndex < 0) newIndex = nearbyTargets.Count - 1;
        if (newIndex >= nearbyTargets.Count) newIndex = 0;

        currentTarget = nearbyTargets[newIndex];
        
        // El TargetGroup mira al proxy, no directamente al enemigo.
        // Asi el cambio de enemigo se suaviza en UpdateTargetProxyPosition().
        SetTargetGroupTarget(targetProxy);
        
        // Movemos el marcador visual al nuevo enemigo
        if (targetMarker != null)
        {
            UpdateTargetMarkerPosition();
        }
        lastSwitchTime = Time.time;
    }

    public void LockTarget(bool state)
    {
        activeTarget = state;

        if (activeTarget)
        {
            Transform bestTarget;
            // Buscamos quien es el que este mas centrado en pantalla
            CollectTargetsAndGetMostInFront(out bestTarget);

            if (bestTarget != null)
            {
                currentTarget = bestTarget;

                CreateTargetProxyIfNeeded();

                // Al activar el lock colocamos el proxy directamente en el enemigo.
                targetProxy.position = GetCameraTargetPosition(currentTarget);
                targetProxyVelocity = Vector3.zero;

                SetTargetGroupTarget(targetProxy);

                targetLockCamera.Follow = playerTransform;
                targetLockCamera.LookAt = targetGroup.transform;

                if(targetMarker==null) 
                {
                    targetMarker = Instantiate(targetMarkerPrefab); 
                }
                targetMarker.SetActive(true);
                UpdateTargetMarkerPosition();

                SwitchCams(true);
                onTargetLock?.Invoke(true);
            }
            else
            {
                // Si no encontramos a nadie, pues no activamos el lock
                activeTarget = false;
            }
        }
        else
        {
            // Reset al desactivar
            nearbyTargets.Clear();
            currentTarget = null;
            if(targetMarker) targetMarker.gameObject.SetActive(false);
            SwitchCams(false);
            onTargetLock?.Invoke(false);
        }
    }

    private void HandleLostCurrentTarget()
    {
        // Si varios enemigos desaparecen casi a la vez, evitamos entrar muchas veces seguidas aqui.
        if (Time.time < lastLostTargetTime + lostTargetSwitchCooldown)
            return;

        lastLostTargetTime = Time.time;

        Transform bestTarget;
        CollectTargetsAndGetMostInFront(out bestTarget);

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            SetTargetGroupTarget(targetProxy);

            if (targetMarker != null)
                UpdateTargetMarkerPosition();

            return;
        }

        // Si no queda nadie cerca, entonces si soltamos el lock.
        LockTarget(false);
    }

    // =========================
    // DETECCION
    // =========================
    
    void CollectTargetsAndGetMostInFront(out Transform mostInFront)
    {
        nearbyTargets.Clear();
        mostInFront = null;
        float bestDot = -1f;

        //Pillamos todos los colliders en la capa enemiga
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, enemyDetectRange, enemyLayer);

        foreach (var hit in hits)
        {
            if (hit == null)
                continue;

            //Usamos el centro del volumen del enemigo, asi el Raycast no apunta a sus pies
            Vector3 targetCenter = hit.bounds.center;
            Vector3 rayOrigin = playerTransform.position + Vector3.up * 1.5f; // Altura de la vista del player

            //Comprobacion de angulo para saber si estamos mirando mas o menos hacia el
            Vector3 dirToEnemy = (targetCenter - Camera.main.transform.position).normalized;
            float dot = Vector3.Dot(Camera.main.transform.forward, dirToEnemy);

            // Si el dot es bajo, el enemigo esta muy en la periferia o detras, asi que lo ignoramos
            if (dot < 0.5f) 
            {
                Debug.DrawLine(rayOrigin, targetCenter, Color.yellow, 1f);
                continue;
            }

            // Comprobacion de Paredes, que no haya un muro entre nosotros
            float distToTarget = Vector3.Distance(rayOrigin, targetCenter);
            Vector3 rayDirection = (targetCenter - rayOrigin).normalized;

            RaycastHit rayHit;
            // Lanzamos el rayo considerando obstaculos y enemigos para ver que golpeamos primero
            if (Physics.Raycast(rayOrigin, rayDirection, out rayHit, distToTarget, obstacleLayer | enemyLayer))
            {
                // Si lo primero que toca el rayo es el enemigo
                if (rayHit.transform == hit.transform || rayHit.transform.IsChildOf(hit.transform))
                {
                    nearbyTargets.Add(hit.transform);

                    // El que tenga el dot mas alto es el que esta mas centrado en la camara
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        mostInFront = hit.transform;
                    }
                }
            }
        }
        
        // Finalmente ordenamos la lista por su posicion en el Viewport
        // Esto hace que al cambiar de target con Q/E se sienta verosimil con lo que ves en pantalla
        nearbyTargets = nearbyTargets.OrderBy(t => 
            Mathf.Abs(0.5f - Camera.main.WorldToViewportPoint(t.position).x)
        ).ToList();
    }

    // =========================
    // CAMARAS Y VISUALES
    // =========================

    void SwitchCams(bool locked)
    {
        if (locked)
        {
            // Cambiamos al estado del Animator que pone la prioridad de la camara Lock en alto
            cameraAnimator.Play("LockedCamera");
            return;
        }

        // Esto es un truquito para que al soltar el lock la camara libre no pegue un salto
        // Intentamos que herede la rotacion que tenia la camara de lock.
        var orbital = freeLookCamera.GetComponent<CinemachineOrbitalFollow>();

        if (orbital != null && Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            Quaternion camRot = Camera.main.transform.rotation;
            freeLookCamera.ForceCameraPosition(camPos, camRot);

            Vector3 dirToCamera = camPos - playerTransform.position;
            Vector3 flatDir = dirToCamera;
            flatDir.y = 0f;

            if (flatDir.sqrMagnitude > 0.0001f)
            {
                float angle = Vector3.SignedAngle(-playerTransform.forward, flatDir, Vector3.up);
                orbital.HorizontalAxis.Value = angle;
            }

            float pitch = camRot.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            orbital.VerticalAxis.Value = pitch;
            freeLookCamera.PreviousStateIsValid = false;
        }

        cameraAnimator.Play("FreeLookCamera");
    }

    private void CreateTargetProxyIfNeeded()
    {
        if (targetProxy != null)
            return;

        GameObject proxyObject = new GameObject("Target Lock Camera Proxy");
        targetProxy = proxyObject.transform;

        if (playerTransform != null)
            targetProxy.position = playerTransform.position + Vector3.up * 1.5f;
    }

    private void SetTargetGroupTarget(Transform target)
    {
        if (targetGroup == null || target == null)
            return;

        // Tu TargetGroup tiene el player en el indice 0 y el target en el indice 1.
        if (targetGroup.Targets.Count > 1)
        {
            targetGroup.Targets[1].Object = target;
        }
    }

    private void UpdateTargetProxyPosition()
    {
        if (!activeTarget || targetProxy == null || currentTarget == null)
            return;

        Vector3 desiredPosition = GetCameraTargetPosition(currentTarget);

        targetProxy.position = Vector3.SmoothDamp(
            targetProxy.position,
            desiredPosition,
            ref targetProxyVelocity,
            targetProxySmoothTime
        );
    }

    private Vector3 GetCameraTargetPosition(Transform target)
    {
        Bounds targetBounds;

        // Para la camara usamos el centro del collider si existe.
        // Asi no mira a los pies del enemigo.
        if (TryGetTargetBounds(target, out targetBounds))
        {
            return targetBounds.center;
        }

        return target.position + Vector3.up * 1.2f;
    }

    // =========================
    // TARGET VISUAL
    // =========================

    private void UpdateTargetMarkerPosition()
    {
        if (currentTarget == null || targetMarker == null)
            return;

        Vector3 markerPosition = GetMarkerPosition(currentTarget);

        // No lo hacemos hijo del enemigo.
        // Asi evitamos problemas si el enemigo tiene escala rara, rotacion o huesos animados.
        targetMarker.transform.SetParent(null);
        targetMarker.transform.position = markerPosition;
    }

    private Vector3 GetMarkerPosition(Transform target)
    {
        Bounds targetBounds;

        // Si el target tiene collider, usamos la parte mas alta del collider.
        if (TryGetTargetBounds(target, out targetBounds))
        {
            return new Vector3(
                targetBounds.center.x,
                targetBounds.max.y + markerExtraHeight,
                targetBounds.center.z
            );
        }

        // Si no tiene collider, usamos una altura por defecto.
        return target.position + Vector3.up * markerDefaultHeight;
    }

    private bool TryGetTargetBounds(Transform target, out Bounds finalBounds)
    {
        finalBounds = new Bounds();

        Collider[] colliders;


        Collider collider = target.GetComponent<Collider>();
        colliders = collider != null ? new Collider[] { collider } : new Collider[0];
        

        bool hasBounds = false;

        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;

            if (!hasBounds)
            {
                finalBounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                finalBounds.Encapsulate(col.bounds);
            }
        }

        return hasBounds;
    }
    

    private void HandleStateChanged(PlayerMovement.MovementState from, PlayerMovement.MovementState to)
    {
        // Si el player pasa a estado "swinging", quitamos el lock.
        if (to == PlayerMovement.MovementState.swinging)
        {
            LockTarget(false); 
        }
    }

    // =========================
    // CALLBACKS DE INPUT
    // =========================

    private void OnFocusPerformed(InputAction.CallbackContext context)
    {
        LockTarget(!activeTarget);
    }

    private void OnSwitchLeftPerformed(InputAction.CallbackContext context)
    {
        if (activeTarget) TrySwitchTarget(true); 
    }

    private void OnSwitchRightPerformed(InputAction.CallbackContext context)
    {
        if (activeTarget) TrySwitchTarget(false); 
    }
}
