using UnityEngine;

// Este script se encarga de leer el estado del PlayerMovement para que se anime
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement; // Script que controla el movimiento
    [SerializeField] private Rigidbody playerRb;    // Rigidbody para sacar velocidad
    [SerializeField] private TargetLockHandler targetLockHandler; // Para saber si estamos haciendo ZTarget
    [SerializeField] private Transform headBone; // Hueso de la cabeza que giraremos hacia el target
    [SerializeField] private Health health;

    [Header("Animator Parameters")]
    [SerializeField] private string isSwingingParam = "isSwinging";
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string jumpTriggerParam = "Jump";
    [SerializeField] private string isGroundedParam = "isGrounded";
    [SerializeField] private string isAttackingParam = "isAttacking";
    [SerializeField] private string isZTargetingParam = "isZTargeting";
    [SerializeField] private string damagedTrigger = "Damaged";
    [SerializeField] private string deathTrigger = "Death";
    
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (health != null)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }
    }


    private void OnEnable()
    {
        // Me suscribo al evento de cambio de estado para reaccionar justo cuando cambie
        if (playerMovement != null){
            playerMovement.OnMovementStateChanged += OnMovementStateChanged;
            playerMovement.OnJumpTriggered += OnJumpTriggered;
            playerMovement.OnAttackTriggered += OnAttackTriggered;
        }

    }

    private void OnDisable()
    {
        if (playerMovement != null){
            playerMovement.OnMovementStateChanged -= OnMovementStateChanged;
            playerMovement.OnJumpTriggered -= OnJumpTriggered;
            playerMovement.OnAttackTriggered -= OnAttackTriggered;
            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeath;
    
        }
    }

    private void Start()
    {
        // Al empezar sincronizo una primera vez el animator
        RefreshAnimator();
    }

    private void Update()
    {
        // Aunque el estado principal venga del PlayerMovement,
        // actualizo cada frame speed para que la animacion sea fluida
        RefreshAnimator();
        
    }

    private void LateUpdate(){

        RotateHeadToTarget();

    }

    private void RotateHeadToTarget()
    {
        // Si no hay cabeza o no hay sistema de target, no hacemos nada
        if (headBone == null || targetLockHandler == null) return;

        // Solo queremos girar la cabeza mientras estamos en ZTarget
        if (!targetLockHandler.IsTargeting) return;

        Transform currentTarget = targetLockHandler.GetCurrentTarget();
        if (currentTarget == null) return;

        // Igual que en PlayerAttack:
        // usamos una rotacion global directa del hueso para que mire al objetivo
        Vector3 dir = headBone.position - currentTarget.position;

        // Evitamos errores si esta practicamente en el mismo punto
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        headBone.rotation = targetRotation;
    }


    private void OnMovementStateChanged(PlayerMovement.MovementState previous, PlayerMovement.MovementState current)
    {
        // Cuando cambie de estado actualizo los bools principales
        ApplyStateBools(current);
    }

    private void OnJumpTriggered()
    {
        if (anim == null) return;
        anim.SetTrigger(jumpTriggerParam);
    }
    
    private void OnAttackTriggered()
    {
        if (anim == null) return;

        anim.SetBool(isAttackingParam, true);
        CancelInvoke(nameof(ResetAttackBool));
        Invoke(nameof(ResetAttackBool), 0.2f);
    }

    private void ResetAttackBool()
    {
        if (anim == null) return;
        anim.SetBool(isAttackingParam, false);
    }

    private void RefreshAnimator()
    {
        if (playerMovement == null || anim == null) return;

        // Aplico bools del estado actual
        ApplyStateBools(playerMovement.state);

        bool isZTargeting = targetLockHandler != null && targetLockHandler.IsTargeting;
        anim.SetBool(isZTargetingParam, isZTargeting);

        //Si no tengo rigidbody no puedo sacar velocidades
        if (playerRb == null) return;

        // Velocidad horizontal real del personaje
        Vector3 flatVel = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
        float flatSpeed = flatVel.magnitude;

        // Speed horizontal para el blend tree
        anim.SetFloat(speedParam, flatSpeed);
        
        anim.SetBool(isGroundedParam, playerMovement.IsGrounded);
    }

    private void OnDamaged(float damageReceived)
    {
        if (anim == null)
            return;

        anim.SetTrigger(damagedTrigger);
    }

    private void OnDeath()
    {
        if (anim == null)
            return;

        anim.SetTrigger(deathTrigger);
    }

    private void ApplyStateBools(PlayerMovement.MovementState currentState)
    {
        // Reseteo y activo solo el bool que toque segun el estado del movimiento
        bool isRunning = currentState == PlayerMovement.MovementState.running;
        bool isAir = currentState == PlayerMovement.MovementState.air;
        bool isSwinging = currentState == PlayerMovement.MovementState.swinging;
        anim.SetBool(isSwingingParam, isSwinging);
    }
}