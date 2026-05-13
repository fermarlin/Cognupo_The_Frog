using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthBar : MonoBehaviour
{
    // Este script representa la vida del jugador en la UI

    [SerializeField] private Health playerHealth; // Referencia al Health del jugador
    [SerializeField] private Image playerLifeFill;        // Imagen de la barra
    [SerializeField] private float lerpSpeed = 5f;        // Velocidad hacia el nuevo valor

    private float targetFill;                             // Objetivo de fill, de 0 a 1
    private Coroutine lerpRoutine;                        //Corrutina activa para interpolar

    void Start()
    {
        // Suscribo la funcion UpdateHealthBar a cada vez que cambie la vida del jugador
        if (playerHealth == null) return;
        if (playerLifeFill == null) return;
        playerHealth.OnHealthChanged += UpdateHealthBar;

        // Empieza con toda la vida
        targetFill = 1f;
        playerLifeFill.fillAmount = targetFill;
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        //Calcula cuanto tiene que poner o quitar y activa la interpolacion (si habia una que la cancele antes de activarlo)
        targetFill = currentHealth / maxHealth;
        if (lerpRoutine != null) StopCoroutine(lerpRoutine);
        lerpRoutine = StartCoroutine(LerpFillAmount());
    }

    private IEnumerator LerpFillAmount()
    {
        // Interpola desde el valor actual hasta el objetivo que hemos seteado antes
        float start = playerLifeFill.fillAmount;
        float timelerp = 0f;

        while (timelerp < 1f)
        {
            timelerp += Time.deltaTime * lerpSpeed;
            playerLifeFill.fillAmount = Mathf.Lerp(start, targetFill, timelerp);
            yield return null;
        }

        //Asegura el valor final por si el lerp no ha dejado el valor exacto
        playerLifeFill.fillAmount = targetFill;
    }
}
