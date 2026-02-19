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
    [SerializeField] private float runSpeed = 8f;          // Velocidad objetivo al correr (clamp en SpeedControl)
    [SerializeField] private float groundDrag = 5f;        // "Freno" en suelo para que no patine
    [SerializeField] private float airMultiplier = 0.5f;   // Control en el aire (menos fuerza, mas floaty)

    // =========================
    // VISUALES
    // =========================
    [Header("Visuals")]
    [SerializeField] private Transform playerMesh;         // El mesh que giramos (NO el rigidbody/transform raiz)
    [SerializeField] private float rotationSpeed = 10f;    // Suavidad de giro (Slerp)

    // =========================
    // SWING
    // =========================
    [Header("Swing")]
    [SerializeField] private VerletRope rope;                      // Referencia a la cuerda (ancla + maxDistance)
    [SerializeField] private float tangentialAccel = 14f;          // Aceleracion tangencial (empuje lateral para balancear)
    [SerializeField] private float radialCorrection = 35f;         // Correccion tipo muelle si "nos pasamos" del radio
    [SerializeField] private float gravityScaleWhileSwing = 1.25f; // Gravedad extra para que el swing se sienta mas pesado
    [SerializeField] private bool pendingSwingBoost = false;       // Flag: al empezar a colgar, aplicar 1 impulso inicial

    // =========================
    // JUMP
    // =========================
    [Header("Jumping")]
    [SerializeField] private float jumpForce = 7f;         // Fuerza de salto
    [SerializeField] private float jumpCooldown = 0.2f;    // Para que no spamee el jugador
    private bool readyToJump = true;                       

    // =========================
    // GROUND CHECK
    // =========================
    [Header("Ground Check")]
    [SerializeField] private LayerMask whatIsGround;       // Que capa es suelo
    [SerializeField] private Transform groundCheck;        // Punto desde el que hacemos CheckSphere
    [SerializeField] private float groundCheckRadius = 0.22f; // Radio del check
    private bool grounded;                                 // Si esta en el suelo o no

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


    private void OnEnable()
    {
        // Creamos el input wrapper una sola vez
        if (inputs == null) inputs = new PlayerInputs();

        // Suscribimos callbacks (movement / jump) y activamos action map
        inputs.PlayerActionMap.Movement.performed += OnMovement;
        inputs.PlayerActionMap.Movement.canceled  += OnMovement;
        inputs.PlayerActionMap.Jump.performed += OnJump;
        inputs.PlayerActionMap.Enable();
    }

    private void OnDisable()
    {
        if (inputs == null) return;

        // Quitamos callbacks y desactivamos action map
        inputs.PlayerActionMap.Movement.performed -= OnMovement;
        inputs.PlayerActionMap.Movement.canceled  -= OnMovement;
        inputs.PlayerActionMap.Jump.performed     -= OnJump;
        inputs.PlayerActionMap.Disable();
    }


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

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
    // SWING: BOOST INICIAL
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
            Quaternion targetRot = Quaternion.LookRotation(targetDirection, Vector3.up);
            playerMesh.rotation = Quaternion.Slerp(
                playerMesh.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
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

        // Normalizamos para que diagonal no corra mas
        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        // En aire aplicamos menos fuerza
        float force = grounded ? runSpeed : runSpeed * airMultiplier;
        rb.AddForce(moveDirection * force * 10f, ForceMode.Force);
    }

    // =========================
    // FISICA DE PeNDULO
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
        // Antes de saltar, reseteamos Y para que no acumule
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void JumpFromSwing()
    {
        // Al saltar cuando estamos enganchados cortamos la cuerda
        if (grapplingGun != null) grapplingGun.StopGrapple();

        // Salto con direccion hacia camara y un poco hacia arriba
        Vector3 jumpDir = camTransform.forward + Vector3.up * 0.5f;
        rb.AddForce(jumpDir.normalized * jumpForce * 1.5f, ForceMode.Impulse);
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
        // Latcheamos jump en performed para consumirlo en Update 1 frame
        if (context.performed)
            jumpPressedThisFrame = true;
    }
}
