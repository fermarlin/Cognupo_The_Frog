using UnityEngine;

// Estados generales que puede tener un enemigo.
// Sirven para controlar el Animator de forma ordenada.
public enum EnemyAnimationState
{
    Idle,
    Following,
    Attacking,
    Damaged,
    Dead
}

// Este script controla las animaciones de un enemigo.
public class EnemyAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Health health;     // Health para escuchar daño y muerte
    [SerializeField] private Rigidbody rb;      // Rigidbody para calcular velocidad


    [Header("Timers")]
    [SerializeField] private float damagedStateTime = 0.35f;
    [SerializeField] private float attackingStateTime = 0.5f;

    [Header("Speed")]
    [SerializeField] private float maxSpeedForAnimation = 6f;


    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string followingParameter = "Following";
    [SerializeField] private string attackingParameter = "Attacking";
    [SerializeField] private string damagedParameter = "Damaged";
    [SerializeField] private string deadParameter = "Dead";

    private float damagedTimer;
    private float attackingTimer;
    private bool isDead;
    private PatrolBetweenPoints patrol;

    private EnemyAnimationState currentState = EnemyAnimationState.Idle;

    private void Awake()
    {
        // Si no lo hemos puesto por Inspector, intentamos buscarlo.
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (patrol == null)
        {
            patrol = GetComponent<PatrolBetweenPoints>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (health != null)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeath;
        }
    }

    private void Update()
    {
        // Si no hay Animator, no hacemos nada.
        if (animator == null)
            return;

        if (isDead)
        {
            SetState(EnemyAnimationState.Dead);
            UpdateAnimatorSpeed();
            return;
        }

        UpdateTimers();
        UpdateAnimatorSpeed();
        SendStateToAnimator();
    }

    private void UpdateTimers()
    {
        if (damagedTimer > 0f)
        {
            damagedTimer -= Time.deltaTime;
            SetState(EnemyAnimationState.Damaged);
            return;
        }

        if (attackingTimer > 0f)
        {
            attackingTimer -= Time.deltaTime;
            SetState(EnemyAnimationState.Attacking);
            return;
        }

        if (currentState == EnemyAnimationState.Damaged)
        {
            SetState(EnemyAnimationState.Idle);
        }

        if (currentState == EnemyAnimationState.Attacking)
        {
            SetState(EnemyAnimationState.Idle);
        }
    }

    public void SetFollowing()
    {
        if (isDead)
            return;

        if (damagedTimer > 0f || attackingTimer > 0f)
            return;

        SetState(EnemyAnimationState.Following);
    }

    public void SetIdle()
    {
        if (isDead)
            return;

        if (damagedTimer > 0f || attackingTimer > 0f)
            return;

        SetState(EnemyAnimationState.Idle);
    }

    public void SetAttacking()
    {
        if (isDead)
            return;

        if (currentState == EnemyAnimationState.Attacking)
            return;

        attackingTimer = attackingStateTime;
        
        SetState(EnemyAnimationState.Attacking);
    }

    private void OnDamaged(float damageReceived)
    {
        if (isDead)
            return;

        damagedTimer = damagedStateTime;
        SetState(EnemyAnimationState.Damaged);

    }

    private void OnDeath()
    {
        isDead = true;

        damagedTimer = 0f;
        attackingTimer = 0f;

        SetState(EnemyAnimationState.Dead);
    }

    private void SetState(EnemyAnimationState newState)
    {
        currentState = newState;
        SendStateToAnimator();
    }

    private void SendStateToAnimator()
    {
        if (animator == null)
            return;

        TrySetBool(followingParameter, currentState == EnemyAnimationState.Following);
        TrySetBool(attackingParameter, currentState == EnemyAnimationState.Attacking);
        TrySetBool(damagedParameter, currentState == EnemyAnimationState.Damaged);
        TrySetBool(deadParameter, currentState == EnemyAnimationState.Dead);
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null)
            return;

        float currentSpeed = 0f;

        if (rb != null)
        {
            Vector3 horizontalVelocity = rb.linearVelocity;
            horizontalVelocity.y = 0f;

            currentSpeed = horizontalVelocity.magnitude;
        }

        if(currentSpeed <= 0.1 && patrol!=null){
            if(patrol.isPatrolling){
                currentSpeed = maxSpeedForAnimation;
            }
        }

        float normalizedSpeed = 0f;

        if (maxSpeedForAnimation > 0f)
        {
            normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeedForAnimation);
        }

        TrySetFloat(speedParameter, normalizedSpeed);
    }

    private void TrySetBool(string parameterName, bool value)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(parameterName))
            return;

        animator.SetBool(parameterName, value);
    }

    private void TrySetFloat(string parameterName, float value)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(parameterName))
            return;

        animator.SetFloat(parameterName, value);
    }

    private void TrySetInteger(string parameterName, int value)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(parameterName))
            return;

        animator.SetInteger(parameterName, value);
    }

    private void TrySetTrigger(string parameterName)
    {
        if (animator == null)
            return;

        if (string.IsNullOrEmpty(parameterName))
            return;

        animator.SetTrigger(parameterName);
    }
}