using UnityEngine;

// Este script dispara un prefab desde un punto concreto.
// Sirve para cualquier enemigo que necesite disparar.
public class EnemyShooter : MonoBehaviour
{
    [Header("Shoot")]
    [SerializeField] private GameObject projectilePrefab;   // Prefab que se dispara
    [SerializeField] private Transform shootPoint;          // Punto desde donde sale
    [SerializeField] private float shootCooldown = 1.5f;    // Tiempo entre disparos

    [SerializeField] private EnemyAnimator enemyAnimator;
    private float lastShootTime;

    private void Awake()
    {
        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<EnemyAnimator>();
        }
    }

    public void Shoot()
    {
        if (projectilePrefab == null || shootPoint == null)
            return;

        if (Time.time < lastShootTime + shootCooldown)
            return;

        lastShootTime = Time.time;

        if (enemyAnimator != null)
        {
            enemyAnimator.SetAttacking();
        }
        
        Instantiate(
            projectilePrefab,
            shootPoint.position,
            shootPoint.rotation
        );
    }
}