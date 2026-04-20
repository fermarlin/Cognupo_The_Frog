using UnityEngine;

// Este script se encarga de leer el estado del PlayerMovement para que se anime
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement; // Script que controla el movimiento
    [SerializeField] private Rigidbody playerRb;    // Rigidbody para sacar velocidad

    [Header("Animator Parameters")]
    [SerializeField] private string isSwingingParam = "isSwinging";
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string jumpTriggerParam = "Jump";
    [SerializeField] private string isGroundedParam = "isGrounded";

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        // Me suscribo al evento de cambio de estado para reaccionar justo cuando cambie
        if (playerMovement != null){
            playerMovement.OnMovementStateChanged += OnMovementStateChanged;
            playerMovement.OnJumpTriggered += OnJumpTriggered;
        }

    }

    private void OnDisable()
    {
        if (playerMovement != null){
            playerMovement.OnMovementStateChanged -= OnMovementStateChanged;
            playerMovement.OnJumpTriggered -= OnJumpTriggered;

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

    private void RefreshAnimator()
    {
        if (playerMovement == null || anim == null) return;

        // Aplico bools del estado actual
        ApplyStateBools(playerMovement.state);

        //Si no tengo rigidbody no puedo sacar velocidades
        if (playerRb == null) return;

        // Velocidad horizontal real del personaje
        Vector3 flatVel = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
        float flatSpeed = flatVel.magnitude;

        // Speed horizontal para el blend tree
        anim.SetFloat(speedParam, flatSpeed);
        
        anim.SetBool(isGroundedParam, playerMovement.IsGrounded);
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