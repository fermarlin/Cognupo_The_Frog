using UnityEngine;

public class Prop_SpringScript : MonoBehaviour
{
    [SerializeField] private Animator propAnimator;
    [SerializeField] private MovementBooster movementBooster;
    [SerializeField] private float pushForce = 35f;

    private void Start()
    {
        if (movementBooster == null)
        {
            Debug.Log("NO HAY SCRIPT");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            propAnimator.SetTrigger("Active");
            movementBooster.Push(other.GetComponent<Rigidbody>(), transform.up, pushForce);
        }
    }
}
