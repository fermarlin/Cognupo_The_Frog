using UnityEngine;

// Este script es el cerebro concreto de McGregor.
// McGregor persigue al player y cuando va a atacar
// se queda completamente parado hasta que termina el ataque.
public class CH_McGregor_Behavior : MonoBehaviour
{
    [Header("Movement")]

    [SerializeField] private float maxSpeed = 8f;                    // Velocidad cuando va hacia el player
    [SerializeField] private float rotationSpeed = 10f;              // Velocidad a la que gira mirando al player

    [Header("Melee Attack")]

    [SerializeField] private int meleeDamage = 1;                    // Daño del ataque cuerpo a cuerpo
    [SerializeField] private float meleeAttackRange = 1.6f;          // Distancia a la que puede atacar
    [SerializeField] private float meleeAttackCooldown = 1f;         // Tiempo entre ataques
    [SerializeField] private float attackDuration = 0.8f;            // Tiempo que dura el ataque
    [SerializeField] private Transform attackPoint;                  // Punto desde donde hace el ataque
    [SerializeField] private float attackRadius = 1f;                // Radio del golpe
    [SerializeField] private string playerTag = "Player";

    [Header("Modules")]

    [SerializeField] private FlyingHeightController heightController;
    [SerializeField] private PatrolBetweenPoints patrol;
    [SerializeField] private KnockbackRecovery knockbackRecovery;
    [SerializeField] private EnemyAnimator enemyAnimator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Health health;
    [SerializeField] private PlayerDetector playerDetector;

    private Transform player;

    private float meleeAttackTimer;
    private float attackTimer;

    private bool isDead = false;
    private bool isAttacking = false;

    private void Awake()
    {
        // Si no estan puestos por Inspector, los buscamos en este GameObject.
        if (patrol == null)
        {
            patrol = GetComponent<PatrolBetweenPoints>();
        }

        if (playerDetector == null)
        {
            playerDetector = GetComponent<PlayerDetector>();
        }

        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (health != null)
        {
            health.OnDeath += OnDeath;
        }

        if (heightController == null)
        {
            heightController = GetComponent<FlyingHeightController>();
        }

        if (knockbackRecovery == null)
        {
            knockbackRecovery = GetComponent<KnockbackRecovery>();
        }

        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<EnemyAnimator>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        // Si no ponemos punto de ataque, usamos el propio transform.
        if (attackPoint == null)
        {
            attackPoint = transform;
        }
    }

    private void OnDestroy()
    {
        // Nos desuscribimos para evitar errores si se destruye el objeto.
        if (health != null)
        {
            health.OnDeath -= OnDeath;
        }
    }

    private void Update()
    {
        // Si esta muerto, no hace nada.
        if (isDead)
            return;

        // Bajamos cooldowns y tiempos internos.
        UpdateTimers();

        // Si esta recuperandose de knockback, no patrulla ni ataca.
        if (knockbackRecovery != null && knockbackRecovery.IsRecovering)
        {
            HandleKnockbackState();
            return;
        }

        // Mantenemos altura si usa este sistema.
        if (heightController != null)
        {
            heightController.KeepHeightFromGround();
        }

        // Si esta atacando, bloqueamos todo.
        if (isAttacking)
        {
            HandleAttackState();
            return;
        }

        // La deteccion la hace PlayerDetector.
        if (playerDetector != null)
        {
            player = playerDetector.UpdateDetection(player);
        }
        else
        {
            player = null;
        }

        // Si PlayerDetector ha encontrado player, McGregor combate.
        if (player != null)
        {
            HandleMcGregorCombat();
            return;
        }

        // Si PlayerDetector no encuentra player, patrulla.
        HandlePatrol();
    }

    private void UpdateTimers()
    {
        // Bajamos el cooldown del ataque melee.
        if (meleeAttackTimer > 0f)
        {
            meleeAttackTimer -= Time.deltaTime;
        }

        // Bajamos el tiempo real que dura el ataque.
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    private void HandleMcGregorCombat()
    {
        if (player == null)
            return;

        // Si ha detectado al player, no patrullamos
        DisablePatrol();

        // Siempre mira hacia el player.
        LookAtPlayer();

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Si esta a distancia cuerpo a cuerpo, se para y ataca.
        if (distanceToPlayer <= meleeAttackRange)
        {
            StopMovement();
            TryMeleeAttack();
            return;
        }

        // Si no esta a distancia cuerpo a cuerpo, va hacia el player.
        MoveToPlayer();
    }

    private void HandleAttackState()
    {
        // Mientras ataca, la patrulla debe estar apagada.
        DisablePatrol();

        // Mientras ataca, no se mueve.
        StopMovement();

        // Mientras ataca, mantenemos el estado de ataque.
        // Esto evita que se quede en animacion de andar/correr.
        if (enemyAnimator != null)
        {
            enemyAnimator.SetAttacking();
        }

        // Puede seguir mirando al player durante el ataque.
        if (player != null)
        {
            LookAtPlayer();
        }

        // Cuando termina el ataque, permitimos volver a moverse.
        if (attackTimer <= 0f)
        {
            isAttacking = false;
        }
    }

    private void HandlePatrol()
    {
        // Solo puede patrullar si NO esta atacando.
        if (isAttacking)
            return;

        // Si no ve al player, reactivamos la patrulla.
        EnablePatrol();

        // Paramos cualquier velocidad anterior.
        StopMovement();

        if (enemyAnimator != null)
        {
            enemyAnimator.SetIdle();
        }

        if (patrol != null)
        {
            patrol.Patrol();
        }
    }

    private void HandleKnockbackState()
    {
        bool canMoveAgain = knockbackRecovery.UpdateRecovery();

        if (!canMoveAgain)
            return;

        if (player != null)
        {
            LookAtPlayer();
        }
    }

    private void MoveToPlayer()
    {
        if (player == null)
            return;

        // Si esta atacando, no permitimos moverse.
        if (isAttacking)
            return;

        // Mientras persigue al player, la patrulla sigue apagada.
        DisablePatrol();

        // Calculamos direccion hacia el player.
        Vector3 direction = player.position - transform.position;

        // Quitamos Y para que no vaya hacia arriba o hacia abajo.
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        // Movemos a McGregor con Rigidbody.
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(
                direction.x * maxSpeed,
                rb.linearVelocity.y,
                direction.z * maxSpeed
            );
        }

        if (enemyAnimator != null)
        {
            enemyAnimator.SetFollowing();
        }
    }

    private void TryMeleeAttack()
    {
        // Si todavia esta en cooldown, NO ataca,
        // pero tampoco se mueve porque esta en rango de ataque.
        if (meleeAttackTimer > 0f)
        {
            StopMovement();
            DisablePatrol();
            return;
        }

        // Marcamos que esta atacando.
        isAttacking = true;

        // Durante este tiempo no se podra mover.
        attackTimer = attackDuration;

        // Reiniciamos cooldown.
        meleeAttackTimer = meleeAttackCooldown;

        // Apagamos patrulla antes de lanzar la animacion.
        DisablePatrol();

        // Paramos el movimiento justo al empezar el ataque.
        StopMovement();

        if (enemyAnimator != null)
        {
            enemyAnimator.SetAttacking();
        }

        DealMeleeDamage();
    }

    public void DealMeleeDamage()
    {
        if (attackPoint == null)
            return;

        Collider[] colliders = Physics.OverlapSphere(attackPoint.position, attackRadius);

        foreach (Collider col in colliders)
        {
            if (!col.CompareTag(playerTag))
                continue;

            DamagePlayer(meleeDamage);
            return;
        }
    }

    private void DamagePlayer(int damage)
    {
        if (player == null)
            return;

        Health playerHealth = player.GetComponent<Health>();

        if (playerHealth == null)
        {
            playerHealth = player.GetComponentInParent<Health>();
        }

        if (playerHealth == null)
            return;

        playerHealth.ChangeHealth(-damage, transform);
    }

    private void LookAtPlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void StopMovement()
    {
        if (rb == null)
            return;

        rb.linearVelocity = new Vector3(
            0f,
            rb.linearVelocity.y,
            0f
        );

        rb.angularVelocity = Vector3.zero;
    }

    private void DisablePatrol()
    {
        if (patrol == null)
            return;

        patrol.isPatrolling = false;

        // Apagamos el componente para que no mueva al enemigo por su cuenta.
        patrol.enabled = false;
    }

    private void EnablePatrol()
    {
        if (patrol == null)
            return;

        // Volvemos a activar la patrulla cuando ya no hay player.
        patrol.enabled = true;
    }

    private void OnDeath()
    {
        // Marcamos a McGregor como muerto.
        isDead = true;

        // Olvidamos al player para que no siga atacando.
        player = null;

        // Ya no esta atacando.
        isAttacking = false;

        // Apagamos patrulla.
        DisablePatrol();

        // Paramos movimiento.
        StopMovement();
    }
}