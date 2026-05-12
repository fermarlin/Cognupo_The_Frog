using UnityEngine;

public class Prop_SpringScript : MonoBehaviour
{
    [SerializeField] private Animator propAnimator;
    [SerializeField] private float pushForce = 35f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            propAnimator.SetTrigger("Active");

            Rigidbody playerRb = other.GetComponent<Rigidbody>();

            MovementBooster.BounceUp(playerRb, pushForce);
        }
    }
}