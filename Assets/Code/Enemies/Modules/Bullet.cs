using UnityEngine;

// Este script hace que la bala avance hacia delante
// y se destruya cuando choque con algo.
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 15f; // Velocidad de la bala
    [SerializeField] private float lifeTime = 5f; // Tiempo maximo antes de destruirse sola

    private void Start()
    {
        // Por seguridad, si no choca con nada, se destruye despues de unos segundos
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // La bala avanza hacia donde esta mirando
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}