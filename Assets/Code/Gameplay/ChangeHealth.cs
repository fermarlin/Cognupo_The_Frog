using UnityEngine;

//Este script se encarga de modificar la vida del objetivo tanto si es para dar como para quitar vida
public class ChangeHealth : MonoBehaviour
{
    [SerializeField] private int healthDiff = 0;              // Cantidad de vida que suma o resta al objetivo
    [SerializeField] private string objectiveTag = "Player";  // Tag del objeto al que quiero afectar
    [SerializeField] private bool destroyOnTrigger = true;    // Si es true, este objeto se destruye al activarse
    [SerializeField] private AudioClip soundEffect;           // Sonido que se reproduce al activarse
    [Header("Spawn On Collect")]
    [SerializeField] private GameObject collectSpawnPrefab;
    private void OnTriggerEnter(Collider other)
    {
        // Si el objeto que entra no tiene el tag salgo
        if (!other.gameObject.CompareTag(objectiveTag))
            return;

        // Intento pillar el script de vida del objeto que ha entrado
        Health playerHealth = other.GetComponent<Health>();

        if (playerHealth == null)
            return;

        // Cambio la vida segun el valor configurado en healthDiff
        // Le paso tambien mi transform para que Health sepa desde donde viene el cambio
        playerHealth.ChangeHealth(healthDiff, transform);

        // Si tengo un sonido asignado, lo reproduzco en la posicion de este objeto
        // Esto lo hago asi para que si se destruye siga sonando el sonidito
        if (soundEffect != null)
        {
            AudioSource.PlayClipAtPoint(soundEffect, transform.position, 1);
        }

        // Si esta activado, destruyo este objeto despues de aplicarse
        if (destroyOnTrigger)
        {
            Destroy(gameObject);
        }

        if (collectSpawnPrefab!=null){
            Instantiate(
                collectSpawnPrefab,
                transform.position,
                Quaternion.identity
            );
        }

    }
}