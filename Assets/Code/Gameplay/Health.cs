using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    // Este script gestiona el sistema de vida

    [Header("Health Parameters")]
    [SerializeField]
    private int maxHealth = 2; // Vida maxima

    [SerializeField]
    private float destroyDelay = 0;   

    public float currentHealth;// Vida actual 
    private bool isDead = false;                    

    public event System.Action<float, float> OnHealthChanged; // Para la UI
    public event System.Action OnDeath; // Aviso cuando este objeto muere


    private void Awake(){
        // Inicializa la vida al maximo al instanciar
        currentHealth = maxHealth;
    }

    public void ChangeHealth(float value){
        // Si ya esta muerto no hago nada
        if (isDead) return;

        // Aplica el cambio a la vida
        currentHealth += value;
        
        if (currentHealth > maxHealth){
            // Si la vida va a superar la maxima se establece la maxima
            currentHealth = maxHealth;
        }

        // Notifica cambio de vida a otros scripts
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        

        // Chequea si se ha muerto el personaje
        if (currentHealth <= 0 && !isDead){
            isDead = true;
            currentHealth = 0;

            // Aviso a otros sistemas
            OnDeath?.Invoke();
            Destroy(gameObject, destroyDelay);
        }
    }

}