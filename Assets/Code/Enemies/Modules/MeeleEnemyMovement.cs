using UnityEngine;

// Este script controla el movimiento de un enemigo melee.
public class MeeleEnemyMovement : MonoBehaviour
{
    [Header("Movement Mode")]

    [SerializeField] private bool chargeToPlayer; // Si esta activo, el enemigo embiste hacia el player
    [SerializeField] private bool walkToPlayer;   // Si esta activo, el enemigo anda hacia el player
    [SerializeField] private bool stayStill;      // Si esta activo, el enemigo se queda quieto

    [Header("Movement")]

    [SerializeField] private float walkSpeed = 3f;       // Velocidad cuando va andando
    [SerializeField] private float chargeSpeed = 7f;     // Velocidad cuando embiste
    [SerializeField] private float rotationSpeed = 8f;   // Velocidad a la que gira mirando al player

    [Header("Attack")]

    [SerializeField] private float attackRange = 1.5f;   // Rango pequeño en el que ataca
    [SerializeField] private float attackCooldown = 1f;  // Tiempo entre ataques

    [Header("References")]

    [SerializeField] private EnemyAnimator enemyAnimator;
    [SerializeField] private Rigidbody rb;

    private float attackTimer;

    private void Awake()
    {
        // Si no hemos puesto el EnemyAnimator por inspector, lo buscamos.
        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<EnemyAnimator>();
        }

        // Si no hemos puesto el Rigidbody por inspector, lo buscamos.
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        // Bajamos el timer del ataque.
        // Esto evita que el enemigo llame a la animacion de ataque cada frame.
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    public void HandleTarget(Transform target)
    {
        HandleTarget(target, 1f, true);
    }

    public void HandleTarget(Transform target, float speedMultiplier, bool canAttack)
    {
        // Si no hay target, no hacemos nada.
        if (target == null)
            return;

        // Siempre miramos hacia el player.
        LookAtTarget(target);

        // Calculamos la distancia hasta el player.
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Si el player esta en rango de ataque, atacamos.
        if (distanceToTarget <= attackRange)
        {
            StopMovement();

            if (canAttack)
            {
                Attack();
            }

            return;
        }

        // Si esta marcado quedarse quieto, no se mueve.
        if (stayStill)
        {
            StopMovement();

            if (enemyAnimator != null)
            {
                enemyAnimator.SetIdle();
            }

            return;
        }

        // Si esta marcado embestir, va rapido hacia el player.
        if (chargeToPlayer)
        {
            MoveTowardsTarget(target, chargeSpeed, speedMultiplier);

            if (enemyAnimator != null)
            {
                enemyAnimator.SetFollowing();
            }

            return;
        }

        // Si esta marcado andar, va normal hacia el player.
        if (walkToPlayer)
        {
            MoveTowardsTarget(target, walkSpeed, speedMultiplier);

            if (enemyAnimator != null)
            {
                enemyAnimator.SetFollowing();
            }

            return;
        }

        // Si no hay ningun modo activado, se queda quieto.
        StopMovement();

        if (enemyAnimator != null)
        {
            enemyAnimator.SetIdle();
        }
    }

    private void Attack()
    {
        // Si todavia esta en cooldown, no puede volver a atacar.
        if (attackTimer > 0f)
            return;

        // Reiniciamos el cooldown del ataque.
        attackTimer = attackCooldown;

        // Llamamos a la animacion de ataque.
        // El daño lo puedes hacer desde un Animation Event en la animacion.
        if (enemyAnimator != null)
        {
            enemyAnimator.SetAttacking();
        }
    }

    private void LookAtTarget(Transform target)
    {
        // Calculamos la direccion hacia el player.
        Vector3 direction = target.position - transform.position;

        // Quitamos la Y para que el enemigo no mire hacia arriba o abajo.
        direction.y = 0f;

        // Si no hay direccion real, no giramos.
        if (direction.sqrMagnitude <= 0.001f)
            return;

        // Calculamos la rotacion mirando al player.
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Giramos suavemente hacia el player.
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void MoveTowardsTarget(Transform target, float speed, float speedMultiplier)
    {
        // Calculamos la direccion hacia el player.
        Vector3 direction = target.position - transform.position;

        // Quitamos la Y para movernos solo en horizontal.
        direction.y = 0f;

        // Si no hay direccion real, no nos movemos.
        if (direction.sqrMagnitude <= 0.001f)
            return;

        // Normalizamos para quedarnos solo con la direccion.
        direction.Normalize();

        // Calculamos la velocidad final.
        Vector3 finalVelocity = direction * speed * speedMultiplier;

        // Si tenemos Rigidbody, movemos usando velocidad.
        // Asi EnemyAnimator puede leer la velocidad y actualizar el parametro Speed.
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(
                finalVelocity.x,
                rb.linearVelocity.y,
                finalVelocity.z
            );
        }
        
    }

    private void StopMovement()
    {
        // Si hay Rigidbody, paramos solo el movimiento horizontal.
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(
                0f,
                rb.linearVelocity.y,
                0f
            );
        }
    }

}