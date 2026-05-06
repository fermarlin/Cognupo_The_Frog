using UnityEngine;

// Este script detecta al Player usando una esfera.
// Sirve para cualquier enemigo que necesite saber si el player esta cerca.
public class PlayerDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";       // Tag del player
    [SerializeField] private float detectionRadius = 8f;        // Radio para detectar
    [SerializeField] private float losePlayerDistance = 12f;    // Distancia para perderlo

    public Transform UpdateDetection(Transform currentTarget)
    {
        // Si ya tengo target, compruebo si se ha alejado demasiado.
        if (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            if (distanceToTarget > losePlayerDistance)
            {
                return null;
            }

            return currentTarget;
        }

        // Si no tengo target, busco con una esfera.
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);

        foreach (Collider col in colliders)
        {
            if (col.CompareTag(playerTag))
            {
                return col.transform;
            }
        }

        return null;
    }

}