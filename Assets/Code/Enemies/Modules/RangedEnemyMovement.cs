using UnityEngine;

// Este script controla el movimiento de un enemigo a distancia.
public class RangedEnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;             // Velocidad de movimiento
    [SerializeField] private float rotationSpeed = 8f;         // Velocidad de giro
    [SerializeField] private float safeDistance = 5f;          // Distancia ideal para disparar
    [SerializeField] private float safeDistanceMargin = 0.5f;  // Margen para no temblar

    [Header("References")]
    [SerializeField] private EnemyShooter shooter;

    private void Awake()
    {
        if (shooter == null)
        {
            shooter = GetComponent<EnemyShooter>();
        }
    }

    public void HandleTarget(Transform target)
    {
        HandleTarget(target, 1f, true);
    }

    public void HandleTarget(Transform target, float speedMultiplier, bool canShoot)
    {
        if (target == null)
            return;

        LookAtTarget(target);

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Si estoy demasiado cerca, me alejo.
        if (distanceToTarget < safeDistance - safeDistanceMargin)
        {
            MoveAwayFromTarget(target, speedMultiplier);
            return;
        }

        // Si estoy demasiado lejos, me acerco.
        if (distanceToTarget > safeDistance + safeDistanceMargin)
        {
            MoveTowardsTarget(target, speedMultiplier);
            return;
        }

        // Si estoy en distancia segura, disparo.
        if (canShoot && shooter != null)
        {
            shooter.Shoot();
        }
    }

    private void LookAtTarget(Transform target)
    {
        Vector3 direction = target.position - transform.position;

        // Quitamos Y para que solo gire horizontalmente.
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

    private void MoveTowardsTarget(Transform target, float speedMultiplier)
    {
        Vector3 direction = target.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        transform.position += direction * moveSpeed * speedMultiplier * Time.deltaTime;
    }

    private void MoveAwayFromTarget(Transform target, float speedMultiplier)
    {
        Vector3 direction = transform.position - target.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = -transform.forward;
        }

        direction.Normalize();

        transform.position += direction * moveSpeed * speedMultiplier * Time.deltaTime;
    }

}