using System.Collections;
using UnityEngine;
using System;
using UnityEngine.InputSystem;

    //Este script es para mover al personaje
public class PlayerMovement : MonoBehaviour
{
    // =========================
    // MOVIMIENTO
    // =========================
    [Header("Movement")]
    [SerializeField] private float runSpeed = 8f;          // Velocidad objetivo al correr
    [SerializeField] private float groundDrag = 5f;        // "Freno" en suelo para que no patine
    [SerializeField] private float airMultiplier = 0.5f;   // Control en el aire
    [Header("Slope")]
    [SerializeField] private float maxWalkableSlopeAngle = 70f; // Hasta este angulo no resbala
    [SerializeField] private float slopeStickForce = 5f;        // Fuerza para pegarlo al suelo sin que patine
    // =========================
    // VISUALES
    // =========================
    [Header("Visuals")]
    [SerializeField] private Transform playerMesh;         // El mesh que giramo
    [SerializeField] private float rotationSpeed = 10f;    // Suavidad de giro
    
    [Header("Ground Visual Rotation")]
    [SerializeField] private float groundAlignRayLength = 1.5f;    // Distancia del raycast hacia abajo para leer la normal
    [SerializeField] private float maxGroundTilt = 45f;            // Limite maximo de inclinacion para evitar cosas raras

    // =========================
    // SWING
    // =========================
    [Header("Swing")]
    [SerializeField] private VerletRope rope;                      // Referencia a la cuerda (ancla + maxDistance)
    [SerializeField] private float tangentialAccel = 14f;          // Aceleracion tangencial (empuje lateral para balancear)
    [SerializeField] private float radialCorrection = 35f;         // Correccion tipo muelle si "nos pasamos" del radio
    [SerializeField] private float gravityScaleWhileSwing = 1.25f; // Gravedad extra para que el swing se sienta mas pesado
    [SerializeField] private bool pendingSwingBoost = false;       // Al empezar a colgar, aplicar 1 impulso inicial
    private float lastSwingTime;
    // =========================
    // JUMP
    // =========================
    [Header("Jumping")]
    [SerializeField] private float jumpForce = 7f;         // Fuerza de salto
    [SerializeField] private float jumpCooldown = 0.2f;    // Para que no spamee el jugador
    [SerializeField] private float fallMultiplier = 2.5f;       // Gravedad extra al caer
    [SerializeField] private float lowJumpMultiplier = 2f;      // Gravedad extra si sueltas el boton de salto antes de tiempo
    [SerializeField] private float hangTimeThreshold = 1f;      // Velocidad Y donde empieza a considerarse el punto mas alto
    [SerializeField] private float hangTimeGravityMult = 0.5f;  // Cuanto reducimos la gravedad en el punto mas alto
    [SerializeField] private float jumpInitialLightTime = 0.12f; // tiempo con subida mas ligera

    private float lastJumpTime;
    private bool readyToJump = true;
    private bool isJumpHeld = false; // Para saber si el jugador mantiene pulsado el boton

    // =========================
    // GROUND CHECK
    // =========================
    [Header("Ground Check")]
    [SerializeField] private LayerMask whatIsGround;       // Que capa es suelo
    [SerializeField] private Transform groundCheck;        // Punto desde el que hacemos CheckSphere
    [SerializeField] private float groundCheckRadius = 0.22f; // Radio del check
    private bool grounded;                                 // Si esta en el suelo o no
    public bool IsGrounded => grounded;
    // =========================
    // REFERENCES
    // =========================
    [Header("References")]
    [SerializeField] private GrapplingGun grapplingGun;           // Para cortar grapple al saltar cuando nos balanceamos
    [SerializeField] private TargetLockHandler targetLockHandler; // Para quitar lock al empezar a colgar

    // =========================
    // INPUT / STATE
    // =========================
    private bool wasSwingingLastFrame = false; 
    

    private float horizontalInput;             
    private float verticalInput;               
    private Vector3 moveDirection;             // Direccion final de movimiento relativa a la camara
    private Rigidbody rb;                      // RigidBody del player
    private Transform camTransform;            // Camara principal

    private PlayerInputs inputs;               // Input System
    private Vector2 moveInput;                 // Vector2 del input
    private bool jumpPressedThisFrame;         


    // =========================
    // MOVEMENT STATE (EVENT)
    // =========================
    public MovementState state;
    public enum MovementState { swinging, running, air }

    // Este evento lo podemos usar para animaciones o lo que nos haga falta mas adelante
    public event Action<MovementState, MovementState> OnMovementStateChanged;
    public event Action OnJumpTriggered;

    private void OnEnable()
    {
        // Creamos el input wrapper una sola vez
        if (inputs == null) inputs = new PlayerInputs();

        // Suscribimos callbacks y activamos action map
        inputs.PlayerActionMap.Movement.performed += OnMovement;
        inputs.PlayerActionMap.Movement.canceled  += OnMovement;
        inputs.PlayerActionMap.Jump.performed += OnJump;
        inputs.PlayerActionMap.Jump.canceled += OnJump;
        inputs.PlayerActionMap.Enable();
    }

    private void OnDisable()
    {
        if (inputs == null) return;

        // Quitamos callbacks y desactivamos action map
        inputs.PlayerActionMap.Movement.performed -= OnMovement;
        inputs.PlayerActionMap.Movement.canceled  -= OnMovement;
        inputs.PlayerActionMap.Jump.performed     -= OnJump;
        inputs.PlayerActionMap.Jump.canceled      -= OnJump;
        inputs.PlayerActionMap.Disable();
    }


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        // Cacheo algunos elementos
        if (Camera.main != null) camTransform = Camera.main.transform;
        if (playerMesh == null) playerMesh = transform;
        if (groundCheck == null) groundCheck = transform;
    }

    private void Update()
    {
        // Si se recrea la camara volvemos a pillarla
        if (camTransform == null && Camera.main != null) camTransform = Camera.main.transform;
        if (camTransform == null) return; // Sin camara no podemos mover relativo a ella

        // Ground check 
        grounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            whatIsGround,
            QueryTriggerInteraction.Ignore
        );

        MyInput();         //Todo el tema del movimiento y tal
        SpeedControl();    // Limita velocidad horizontal
        StateHandler();    // Calcula running/air/swinging y lanza evento si cambia
        HandleRotation();  // Gira mesh segun se mueva el pj

        // Detectar si acabo de entrar en swing
        bool isSwingingNow = IsSwinging();

        if (isSwingingNow) 
        {
            lastSwingTime = Time.time; // Guardo en que momento estoy balanceandome
        }

        if (isSwingingNow && !wasSwingingLastFrame)
        {
            // Marcamos que necesitamos el impulso inicial al empezar a colgar, esto lo hago porque a veces se me quedaba como tieso el pj y era dificil ganar velocidad
            pendingSwingBoost = true; 

            // Si estabas lockeando target, al colgar nos interesa soltar el lock para que nos podamos mover bien
            if (targetLockHandler != null && targetLockHandler.IsTargeting)
                targetLockHandler.LockTarget(false);
        }

        wasSwingingLastFrame = isSwingingNow;
    }

    private void FixedUpdate()
    {
        if (camTransform == null) return;

        // Si estamos en swing, NO usamos el movimiento
        if (IsSwinging())
        {
            // Aplicamos el impulso una sola vez al empezar a colgar
            if (pendingSwingBoost)
            {
                ApplyInitialSwingBoost();
            }

            // Fisica de pendulo
            HandleSwingPendulum();
            return;
        }

        // Movimiento normal por fuerzas
        MovePlayerNormal();
        HandleJumpPhysics();
    }

    // =========================
    // INPUT
    // =========================
    private void MyInput()
    {
        // Movement viene del Input System
        horizontalInput = moveInput.x;
        verticalInput   = moveInput.y;

        // Salto
        if (jumpPressedThisFrame)
        {
            jumpPressedThisFrame = false;

            if (IsSwinging())
            {
                // Salto especial desde swing
                JumpFromSwing();
            }
            else if (readyToJump && grounded)
            {
                // Salto normal en suelo
                readyToJump = false;
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }
    }

    // =========================
    // BOOST INICIAL
    // =========================
    private void ApplyInitialSwingBoost()
    {
        pendingSwingBoost = false;

        // Direccion de impulso, si estoy moviendome pues hacia donde me mueva, si no hacia donde mira la camara
        Vector3 camForward = camTransform.forward;
        camForward.y = 0;
        Vector3 camRight = camTransform.right;
        camRight.y = 0;

        Vector3 inputDir = camForward.normalized * verticalInput + camRight.normalized * horizontalInput;
        Vector3 boostDir = inputDir.sqrMagnitude > 0.001f ? inputDir.normalized : camForward.normalized;

        // Limpiamos un poco velocidad horizontal residual para que la transicion sea limpia
        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x * 0.5f,
            rb.linearVelocity.y,
            rb.linearVelocity.z * 0.5f
        );

        // Impulso horizontal para que enganche bien al colgar
        rb.AddForce(boostDir * 12f, ForceMode.Impulse);

        // Mini empujon vertical para que la cuerda no se destense de golpe al enganchar
        rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
    }

    // =========================
    // STATE MACHINE
    // =========================
    private void StateHandler()
    {
        // Guardamos estado anterior para evento
        MovementState previous = state;

        // Logica de estados
        if (IsSwinging()) state = MovementState.swinging;
        else if (grounded) state = MovementState.running;
        else state = MovementState.air;

        // Evento solo si hay cambio real
        if (previous != state)
        {
            OnMovementStateChanged?.Invoke(previous, state);
        }
    }

    // =========================
    // ROTACION DEL MESH
    // =========================

    private void HandleRotation()
    {
        if (playerMesh == null) return;

        Vector3 targetDirection = Vector3.zero;

        // Rotacion especial mientras hace swing
        if (IsSwinging() && rope != null && rope.fixedPoint != null)
        {
            Vector3 targetPoint = rope.fixedPoint.position;
            Vector3 toTarget = targetPoint - playerMesh.position;

            // Direccion horizontal para decidir hacia donde mira en Y
            Vector3 flatDir = new Vector3(toTarget.x, 0f, toTarget.z);

            if (flatDir.sqrMagnitude > 0.0001f)
            {
                // Rotacion base: mirar hacia el punto en horizontal
                Quaternion yawRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);

                // Distancia horizontal al punto
                float horizontalDistance = flatDir.magnitude;

                // Diferencia vertical respecto al punto
                float verticalDistance = Mathf.Abs(toTarget.y);

                // Cuanto "levantamos" al personaje:
                // - Si horizontalDistance tiende a 0 => angulo tiende a 0
                // - Si verticalDistance tiende a 0 => angulo tiende a 90
                float pitchRatio = 0f;
                float total = horizontalDistance + verticalDistance;

                if (total > 0.0001f)
                    pitchRatio = horizontalDistance / total;

                float pitchAngle = Mathf.Lerp(0f, 90f, pitchRatio);

                // Inclinacion sobre su eje local X
                Quaternion pitchRot = Quaternion.Euler(pitchAngle, 0f, 0f);

                // Rotacion final
                Quaternion targetRot = yawRot * pitchRot;

                playerMesh.rotation = Quaternion.Slerp(
                    playerMesh.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            // Velocidad horizontal
            Vector3 vel = rb.linearVelocity;
            Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);

            if (flatVel.sqrMagnitude > 0.04f)
            {
                targetDirection = flatVel.normalized;
            }
            else
            {
                // Si casi estas parado, usamos el input relativo a camara para orientar el inicio
                Vector3 camForward = camTransform.forward;
                Vector3 camRight = camTransform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                Vector3 inputDir = camForward * verticalInput + camRight * horizontalInput;
                if (inputDir.sqrMagnitude > 0.001f)
                    targetDirection = inputDir.normalized;
            }

            // Rotacion lerpeada para que no sea instantaneo
            if (targetDirection.sqrMagnitude > 0.001f)
            {
                Quaternion yawRot = Quaternion.LookRotation(targetDirection.normalized, Vector3.up);

                //Proyectamos su forward sobre la superficie
                if (TryGetGroundNormal(out Vector3 groundNormal))
                {
                    Vector3 projectedForward = Vector3.ProjectOnPlane(yawRot * Vector3.forward, groundNormal).normalized;

                    // Si por alguna razon la proyeccion sale mal, usamos la normal del suelo
                    if (projectedForward.sqrMagnitude > 0.001f)
                    {
                        Quaternion groundRot = Quaternion.LookRotation(projectedForward, groundNormal);

                        // Limito la inclinacion maxima para que no haga cosas exageradas
                        groundRot = ClampRotationTilt(groundRot, groundNormal, maxGroundTilt);

                        playerMesh.rotation = Quaternion.Slerp(
                            playerMesh.rotation,
                            groundRot,
                            rotationSpeed * Time.deltaTime
                        );
                        return;
                    }
                }

                playerMesh.rotation = Quaternion.Slerp(
                    playerMesh.rotation,
                    yawRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }
    // =========================
    // LEER NORMAL DEL SUELO
    // =========================
    private bool TryGetGroundNormal(out Vector3 groundNormal)
    {
        groundNormal = Vector3.up;

        Vector3 rayOrigin = playerMesh.position + Vector3.up * 0.25f;

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            groundAlignRayLength,
            whatIsGround,
            QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal;
            return true;
        }

        return false;
    }

    // =========================
    // LIMITAR INCLINACION MAXIMA
    // =========================
    private Quaternion ClampRotationTilt(Quaternion targetRot, Vector3 groundNormal, float maxTilt)
    {
        Vector3 forward = targetRot * Vector3.forward;

        // Rehacemos el forward sobre un plano para evitar deformaciones raras
        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        if (projectedForward.sqrMagnitude < 0.001f)
            projectedForward = playerMesh.forward;

        Quaternion flatRot = Quaternion.LookRotation(projectedForward, Vector3.up);
        float tiltAngle = Vector3.Angle(Vector3.up, groundNormal);

        if (tiltAngle <= maxTilt)
            return targetRot;

        float t = maxTilt / tiltAngle;
        Vector3 limitedNormal = Vector3.Slerp(Vector3.up, groundNormal, t).normalized;

        Vector3 limitedForward = Vector3.ProjectOnPlane(flatRot * Vector3.forward, limitedNormal).normalized;
        if (limitedForward.sqrMagnitude < 0.001f)
            limitedForward = Vector3.ProjectOnPlane(playerMesh.forward, limitedNormal).normalized;

        return Quaternion.LookRotation(limitedForward, limitedNormal);
    }

        // =========================
        // LIMITAR EL RESBALAR
        // =========================

    private void HandleSlopeAntiSlide()
    {
        if (!grounded) return;
        if (!TryGetGroundNormal(out Vector3 groundNormal)) return;

        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);

        // Si la pendiente es suave, quitamos la componente de velocidad que empuja cuesta abajo
        if (slopeAngle <= maxWalkableSlopeAngle)
        {
            Vector3 velocity = rb.linearVelocity;

            // Separamos velocidad vertical de horizontal
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

            // Direccion cuesta abajo sobre el plano
            Vector3 downhillDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;

            if (downhillDir.sqrMagnitude > 0.001f)
            {
                // Cuanta velocidad llevamos hacia abajo de la pendiente
                float downhillSpeed = Vector3.Dot(horizontalVelocity, downhillDir);

                // Si se esta moviendo cuesta abajo por el deslizamiento, se la quitamos
                if (downhillSpeed > 0f)
                {
                    horizontalVelocity -= downhillDir * downhillSpeed;
                }
            }

            rb.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);

            // Un pequeno empuje hacia el suelo para que no flote o tiemble
            rb.AddForce(-groundNormal * slopeStickForce, ForceMode.Acceleration);
        }
    }


    // =========================
    // MOVIMIENTO NORMAL (SUELO/AIRE)
    // =========================

    private void MovePlayerNormal()
    {
        //Como nuestro presonaje se mueve en funcion de donde mira la camara pues lo pillamos asi
        Vector3 camForward = camTransform.forward;
        Vector3 camRight = camTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        moveDirection = camForward * verticalInput + camRight * horizontalInput;
        if(moveDirection==Vector3.zero){
            HandleSlopeAntiSlide();
        }
        // Normalizamos para que diagonal no corra mas
        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        // En aire aplicamos menos fuerza
        float force = grounded ? runSpeed : runSpeed * airMultiplier;
        rb.AddForce(moveDirection * force * 10f, ForceMode.Force);
    }

    // =========================
    // FISICA DE PENDULO
    // =========================
    private void HandleSwingPendulum()
    {
        if (rope == null || rope.fixedPoint == null) return;

        // Quito el damping para conservar inercia
        rb.linearDamping = 0f;

        Vector3 anchor = rope.fixedPoint.position;
        Vector3 currentPos = transform.position;
        Vector3 toAnchor = anchor - currentPos;
        float distToAnchor = toAnchor.magnitude;
        Vector3 dirToAnchor = toAnchor.normalized;

        // Le aplico mas gravedad mientras esta colgando, esto es algo que he puesto para que sea mas gustoso
        rb.AddForce(Physics.gravity * gravityScaleWhileSwing, ForceMode.Acceleration);

        // Empuje solo en la tangente que si no la rana se pone a andar hacia delante en vez de balancearse
        Vector3 camFwd = camTransform.forward; camFwd.y = 0;
        Vector3 camRight = camTransform.right; camRight.y = 0;

        //Si no hay input, esto queda en Vector3.zero y no aplicamos fuerza
        Vector3 inputDir = (camFwd.normalized * verticalInput + camRight.normalized * horizontalInput).normalized;

        if (inputDir.sqrMagnitude > 0.01f)
        {
            // Proyectamos el input en un plano perpendicular a la cuerda
            Vector3 swingDir = Vector3.ProjectOnPlane(inputDir, dirToAnchor).normalized;

            // Si la velocidad es muy baja, damos empujon inicial para arrancar el balanceo
            if (rb.linearVelocity.magnitude < 1f)
                rb.AddForce(swingDir * tangentialAccel * 0.5f, ForceMode.VelocityChange);
            else
                rb.AddForce(swingDir * tangentialAccel, ForceMode.Acceleration);
        }

        if (distToAnchor > rope.maxDistance)
        {
            // Cuanto nos estamos alejando del punto de anclaje
            float speedAway = Vector3.Dot(rb.linearVelocity, -dirToAnchor);

            if (speedAway > 0)
            {
                // Eliminamos la velocidad que te aleja
                Vector3 tensionCorrection = dirToAnchor * speedAway;
                rb.linearVelocity += tensionCorrection;
            }

            // Si por error numerico nos fuimos mucho acerco de nuevo al jugador
            float drift = distToAnchor - rope.maxDistance;
            if (drift > 0.1f)
            {
                Vector3 restoreForce = dirToAnchor * (drift * radialCorrection);
                rb.AddForce(restoreForce, ForceMode.Acceleration);
            }
        }
    }

    // =========================
    // SPEED
    // =========================
    private void SpeedControl()
    {
        // En swing no limito la velocidad porque si no se queda tieso
        if (IsSwinging()) return;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Clampeo la velocidad horizontal
        if (flatVel.magnitude > runSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * runSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }

        //Drag solo en suelo 
        rb.linearDamping = grounded ? groundDrag : 0f;
    }

    // =========================
    // JUMP
    // =========================
    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        lastJumpTime = Time.time;

        OnJumpTriggered?.Invoke();
    }

    private void JumpFromSwing()
    {
        // Al saltar cuando estamos enganchados cortamos la cuerda
        if (grapplingGun != null) grapplingGun.StopGrapple();

        // Salto con direccion hacia camara y un poco hacia arriba
        Vector3 jumpDir = camTransform.forward + Vector3.up * 0.5f;
        rb.AddForce(jumpDir.normalized * jumpForce * 1.5f, ForceMode.Impulse);
    }

    private void HandleJumpPhysics()
    {
        if (grounded || IsSwinging()) return;

        float timeSinceJump = Time.time - lastJumpTime;

        // Caida
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        // Durante un instante inicial no endurecemos el salto
        else if (rb.linearVelocity.y > 0)
        {
            if (timeSinceJump < jumpInitialLightTime)
            {
                return;
            }

            if (!isJumpHeld && (Time.time - lastSwingTime > 0.3f))
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
            }
        }
        // Tiempo de suspension
        else if (Mathf.Abs(rb.linearVelocity.y) < hangTimeThreshold && isJumpHeld)
        {
            rb.AddForce(
                Vector3.up * (Mathf.Abs(Physics.gravity.y) * (1 - hangTimeGravityMult)),
                ForceMode.Acceleration
            );
        }
    }

    private void ResetJump() => readyToJump = true; 

    // =========================
    // SWING CHECK
    // =========================
    private bool IsSwinging()
    {
        return grapplingGun != null && grapplingGun.IsGrappling();
    }

    // =========================
    // CALLBACKS DE INPUT
    // =========================
    public void OnMovement(InputAction.CallbackContext context)
    {
        // Se actualiza continuamente (performed) y vuelve a (0,0) al soltar (canceled)
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpPressedThisFrame = true;
            isJumpHeld = true;
        }
        else if (context.canceled)
        {
            isJumpHeld = false;
        }
    }
}
