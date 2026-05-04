using UnityEngine;

public class MovementBooster: MonoBehaviour
{
    public void Push(Rigidbody targetRb, Vector3 pushDirection, float pushForce)
    {
        targetRb.AddForce(pushDirection*pushForce, ForceMode.Impulse);
    }
}
