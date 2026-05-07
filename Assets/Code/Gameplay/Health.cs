using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    // Este script gestiona el sistema de vida

    [Header("Health Parameters")]
    [SerializeField] private int maxHealth = 2; // Vida maxima

    [SerializeField] private float destroyDelay = 0f;

    [SerializeField] private bool destroyOnDeath = true; // Si queremos que se destruya al morir

    [Header("Death Prefab")]
    [SerializeField] private GameObject onDestroyPrefab; // Prefab que aparece justo antes de destruirse

    [SerializeField] private Vector3 onDestroyPrefabOffset = Vector3.up; // Offset para que aparezca un poco encima

    [Header("Knockback")]
    [SerializeField] private bool useKnockback = false; // Si queremos que reciba knockback al recibir daño

    [SerializeField] private float knockbackForce = 8f; // Fuerza horizontal del knockback

    [SerializeField] private float knockbackUpForce = 3f; // Fuerza hacia arriba del knockback

    public float currentHealth; // Vida actual

    private bool isDead = false;

    private Rigidbody rb;

    public event System.Action<float, float> OnHealthChanged; // Para la UI

    public event System.Action OnDeath; // Aviso cuando este objeto muere

    public event System.Action<float> OnDamaged; // Aviso cuando este objeto recibe daño

    private void Awake()
    {
        // Inicializa la vida al maximo al instanciar
        currentHealth = maxHealth;

        // Pillamos el Rigidbody por si queremos aplicar knockback
        rb = GetComponent<Rigidbody>();
    }

    public void ChangeHealth(float value)
    {
        // Llamamos a la version completa sin origen de daño
        ChangeHealth(value, null);
    }

    public void ChangeHealth(float value, Transform damageOrigin)
    {
        // Si ya esta muerto no hago nada
        if (isDead) return;

        // Guardamos si el cambio de vida es daño
        bool isDamage = value < 0;

        // Aplica el cambio a la vida
        currentHealth += value;

        if (currentHealth > maxHealth)
        {
            // Si la vida va a superar la maxima se establece la maxima
            currentHealth = maxHealth;
        }

        if (currentHealth < 0)
        {
            // Si baja de 0, la dejamos en 0
            currentHealth = 0;
        }

        // Si ha recibido daño, aviso a otros scripts
        if (isDamage)
        {
            OnDamaged?.Invoke(value);

            // Si queremos knockback y tenemos origen del daño, hacemos knockback
            if (useKnockback && damageOrigin != null)
            {
                MovementBooster.KnockbackFrom(rb, transform, damageOrigin, knockbackForce, knockbackUpForce);
            }
        }

        // Notifica cambio de vida a otros scripts
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Chequea si se ha muerto el personaje
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;

            // Aviso a otros sistemas de que ha muerto
            OnDeath?.Invoke();

            // Si queremos destruirlo, esperamos el delay y justo antes instanciamos el prefab
            if (destroyOnDeath)
            {
                StartCoroutine(DestroyAfterDelay());
            }
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        // Esperamos el tiempo que hayas puesto en el inspector
        yield return new WaitForSeconds(destroyDelay);

        // Justo antes de destruir el enemigo, instanciamos el prefab
        if (onDestroyPrefab != null)
        {
            Instantiate(onDestroyPrefab, transform.position + onDestroyPrefabOffset, transform.rotation);
        }

        //Destruimos este objeto
        Destroy(gameObject);
    }
}